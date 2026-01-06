using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using core.jarvis.data.RLS;
using Dapper;
using Npgsql;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace core.jarvis.data
{
    /// <summary>
    /// Provides a secure, convention-based client for PostgreSQL access.
    /// Handles JWT-based RLS, authentication, and table access with snake_case mapping.
    /// On initialization, verifies connection and minimum authentication schema/policies.
    /// </summary>
    public class PgClient : IDisposable
    {
        private readonly NpgsqlConnection _conn;
        private string? _jwt;
        private Dictionary<string, string>? _jwtClaims;
        private readonly RLSPolicyRegistry _rlsPolicies;
        private readonly ILogger? _logger;
        private bool _jwtClaimsSetForConnection = false;
        
        // Activity source for distributed tracing
        private static readonly ActivitySource ActivitySource = new ActivitySource("Jarvis.Database.PgClient");
        
        /// <summary>
        /// Gets the current user ID extracted from the JWT token.
        /// </summary>
        public Guid CurrentUserId { get; private set; }

        /// <summary>
        /// Initializes a new PgClient with the given Npgsql connection.
        /// Note: Call VerifyMinimumsAsync() after construction to verify connection.
        /// </summary>
        /// <param name="conn">The Npgsql database connection.</param>
        /// <param name="rlsPolicies">Optional RLS policy registry. If not provided, uses default policies.</param>
        /// <param name="logger">Optional logger for debugging and diagnostics.</param>
        public PgClient(NpgsqlConnection conn, RLSPolicyRegistry? rlsPolicies = null, ILogger? logger = null)
        {
            _conn = conn;
            _rlsPolicies = rlsPolicies ?? new RLSPolicyRegistry();
            _logger = logger;
            
            // Register default policies if using default registry
            if (rlsPolicies == null)
            {
                DefaultRLSPolicies.RegisterDefaultPolicies(_rlsPolicies);
            }
            
            // Subscribe to connection state changes to reset JWT claims flag
            _conn.StateChange += OnConnectionStateChange;
        }

        /// <summary>
        /// Creates and initializes a new PgClient with proper async verification.
        /// </summary>
        /// <param name="conn">The Npgsql database connection.</param>
        /// <param name="rlsPolicies">Optional RLS policy registry. If not provided, uses default policies.</param>
        /// <param name="logger">Optional logger for debugging and diagnostics.</param>
        /// <returns>A fully initialized PgClient instance.</returns>
        public static async Task<PgClient> CreateAsync(NpgsqlConnection conn, RLSPolicyRegistry? rlsPolicies = null, ILogger? logger = null)
        {
            var client = new PgClient(conn, rlsPolicies, logger);
            await client.VerifyMinimums();
            return client;
        }

        /// <summary>
        /// Verifies the connection is open and ready.
        /// </summary>
        private async Task VerifyMinimums()
        {
            try
            {
                if (_conn.State != System.Data.ConnectionState.Open)
                    await _conn.OpenAsync();
                
                // Connection verification is sufficient - table creation is handled by the component system
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to verify database connection");
                throw;
            }
        }


        /// <summary>
        /// Sets the JWT token to be used for all subsequent requests.
        /// Parses the JWT to extract claims for RLS.
        /// </summary>
        /// <param name="jwt">JWT string.</param>
        public void JWT(string jwt)
        {
            _jwt = jwt;
            _jwtClaims = ParseJWTClaims(jwt);
        }

        /// <summary>
        /// Parses and validates JWT claims for RLS purposes.
        /// SECURITY: Always validates JWT signature to prevent token forgery.
        /// </summary>
        private Dictionary<string, string> ParseJWTClaims(string jwt)
        {
            using var activity = ActivitySource.StartActivity("PgClient.ParseJWTClaims");
            
            var handler = new JwtSecurityTokenHandler();
            
            // Get JWT secret from environment or config (using double underscore for nested config)
            var jwtSecret = Environment.GetEnvironmentVariable("Jwt__SecretKey") ?? 
                           throw new InvalidOperationException("Jwt__SecretKey not configured");
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? "jarvis-api",
                ValidateAudience = true,
                ValidAudience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? "jarvis-clients",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            try
            {
                // Validate token and extract principal
                var principal = handler.ValidateToken(jwt, validationParameters, out SecurityToken validatedToken);
                
                var claims = new Dictionary<string, string>();
                foreach (var claim in principal.Claims)
                {
                    // Store the last value if there are duplicates
                    claims[claim.Type] = claim.Value;
                }
                
                // Also check for standard JWT claims and add them with expected names
                // This ensures RLS policies work with common claim names
                if (claims.TryGetValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", out var nameId))
                {
                    claims["sub"] = nameId;
                }
                if (claims.TryGetValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var roleValue))
                {
                    claims["role"] = roleValue;
                }
                
                // Extract and set CurrentUserId if available
                if (claims.TryGetValue("sub", out var userId) && Guid.TryParse(userId, out var parsedUserId))
                {
                    CurrentUserId = parsedUserId;
                }
                
                return claims;
            }
            catch (SecurityTokenValidationException ex)
            {
                throw new UnauthorizedAccessException($"Invalid JWT token: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException($"Token validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets JWT claims as PostgreSQL session variables for RLS.
        /// </summary>
        internal async Task JWTClaims()
        {
            using var activity = ActivitySource.StartActivity("PgClient.JWTClaims");
            
            if (_jwtClaims == null || _jwtClaims.Count == 0)
            {
                activity?.SetTag("has_claims", false);
                return;
            }
            
            // Check if JWT claims are already set for this connection
            if (_jwtClaimsSetForConnection)
            {
                activity?.SetTag("already_set", true);
                activity?.SetTag("skipped", true);
                _logger?.LogDebug("JWT claims already set for this connection, skipping");
                return;
            }

            activity?.SetTag("has_claims", true);
            activity?.SetTag("claim_count", _jwtClaims.Count);
            activity?.SetTag("already_set", false);
            
            using (var openActivity = ActivitySource.StartActivity("PgClient.EnsureConnectionOpen"))
            {
                await EnsureConnectionOpen();
            }

            // Set each claim as a session variable
            using (var setClaimsActivity = ActivitySource.StartActivity("PgClient.SetSessionClaims"))
            {
                setClaimsActivity?.SetTag("claim_count", _jwtClaims.Count);
                
                foreach (var claim in _jwtClaims)
            {
                // PostgreSQL has a 63-character limit for identifiers
                // Some JWT claims (like Microsoft's) have very long names
                // We need to sanitize the claim key to be a valid PostgreSQL identifier
                var sanitizedKey = SanitizeClaimKey(claim.Key);
                var variableName = $"app.{sanitizedKey}";
                
                // PostgreSQL custom variables need to be set with literal values
                // We escape single quotes to prevent SQL injection
                var escapedValue = claim.Value.Replace("'", "''");
                var sql = $"SET SESSION \"{variableName}\" = '{escapedValue}';";
                
                try
                {
                    using var cmd = new NpgsqlCommand(sql, _conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to set session variable {VariableName}", variableName);
                    throw;
                }
            }
            }
            
            // Mark claims as set for this connection
            _jwtClaimsSetForConnection = true;
            _logger?.LogDebug("JWT claims set successfully for connection");
        }

        /// <summary>
        /// Sanitizes a JWT claim key to be a valid PostgreSQL identifier.
        /// Handles common JWT claim types and shortens long names.
        /// </summary>
        private string SanitizeClaimKey(string claimKey)
        {
            // Handle common JWT claim types with long URIs
            // Also map standard claims to RLS-friendly names
            var commonMappings = new Dictionary<string, string>
            {
                ["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] = "role",
                ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] = "user_id",
                ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] = "name",
                ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] = "email",
                ["http://schemas.microsoft.com/identity/claims/objectidentifier"] = "oid",
                ["http://schemas.microsoft.com/identity/claims/tenantid"] = "tenant_id",
                // Map standard JWT "sub" claim to user_id for RLS owner checks
                ["sub"] = "user_id"
            };
            
            if (commonMappings.TryGetValue(claimKey, out var shortName))
                return shortName;
            
            // For other claims, sanitize the key
            // Remove URI prefixes
            if (claimKey.StartsWith("http://") || claimKey.StartsWith("https://"))
            {
                var lastSlash = claimKey.LastIndexOf('/');
                if (lastSlash >= 0 && lastSlash < claimKey.Length - 1)
                    claimKey = claimKey.Substring(lastSlash + 1);
            }
            
            // Replace invalid characters with underscores
            var sanitized = System.Text.RegularExpressions.Regex.Replace(claimKey, @"[^a-zA-Z0-9_]", "_");
            
            // Ensure it doesn't start with a number
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;
            
            // Truncate to 63 characters minus "app." prefix (4 chars)
            if (sanitized.Length > 59)
                sanitized = sanitized.Substring(0, 59);
            
            return string.IsNullOrEmpty(sanitized) ? "claim" : sanitized;
        }

        /// <summary>
        /// Handles connection state changes to reset JWT claims tracking.
        /// When a connection is closed or broken, we need to re-set JWT claims when it reopens.
        /// </summary>
        private void OnConnectionStateChange(object sender, System.Data.StateChangeEventArgs e)
        {
            // When connection closes or breaks, reset the flag so claims are re-set on next use
            if (e.CurrentState == System.Data.ConnectionState.Closed || 
                e.CurrentState == System.Data.ConnectionState.Broken)
            {
                _jwtClaimsSetForConnection = false;
                _logger?.LogDebug("Connection state changed to {State}, resetting JWT claims flag", e.CurrentState);
            }
        }
        
        /// <summary>
        /// Ensures the database connection is open.
        /// </summary>
        private async Task EnsureConnectionOpen()
        {
            if (_conn.State != System.Data.ConnectionState.Open)
                await _conn.OpenAsync();
        }

        /// <summary>
        /// Returns a strongly-typed table accessor for the given entity type.
        /// Passes the JWT claims for RLS if set.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <returns>PgTable instance for the entity type.</returns>
        public PgTable<T> From<T>() where T : class, new()
        {
            return new PgTable<T>(_conn, this, _rlsPolicies, _jwtClaims ?? new Dictionary<string, string>(), _logger);
        }

        /// <summary>
        /// Calls a PostgreSQL function (RPC) with named arguments.
        /// Optionally sets JWT for RLS.
        /// </summary>
        /// <param name="functionName">Function name.</param>
        /// <param name="args">Arguments object (properties mapped to parameters).</param>
        public async Task Rpc(string functionName, object args)
        {
            var paramList = args.GetType().GetProperties()
                .Select(p => $"@{p.Name}")
                .ToList();
            var sql = $"SELECT {functionName}({string.Join(", ", paramList)})";

            var dynamicParams = new Dapper.DynamicParameters();
            foreach (var prop in args.GetType().GetProperties())
            {
                dynamicParams.Add($"@{prop.Name}", prop.GetValue(args));
            }

            // Set JWT claims for RLS if available
            await JWTClaims();

            try
            {
                await _conn.ExecuteAsync(sql, dynamicParams);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to execute RPC {FunctionName}", functionName);
                throw;
            }
        }

        /// <summary>
        /// Executes SQL command directly with parameters.
        /// Sets JWT claims for RLS if available.
        /// </summary>
        /// <param name="sql">SQL command to execute.</param>
        /// <param name="parameters">Parameters for the SQL command.</param>
        public async Task ExecuteAsync(string sql, object? parameters = null)
        {
            await EnsureConnectionOpen();
            
            // Set JWT claims for RLS if available
            await JWTClaims();

            try
            {
                await _conn.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to execute SQL: {Sql}", sql);
                throw;
            }
        }

        /// <summary>
        /// Disposes the PgClient and its resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method for proper disposal pattern.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from state change events before disposing
                if (_conn != null)
                {
                    _conn.StateChange -= OnConnectionStateChange;
                    _conn.Dispose();
                }
            }
        }
    }
}