using System.IdentityModel.Tokens.Jwt;
using core.jarvis.data.RLS;
using Dapper;
using Npgsql;
using Microsoft.IdentityModel.Tokens;

namespace core.jarvis.data
{
    /// <summary>
    /// Provides a secure, convention-based client for PostgreSQL access.
    /// Handles JWT-based RLS, authentication, and table access with snake_case mapping.
    /// On initialization, verifies connection and minimum authentication schema/policies.
    /// </summary>
    public class PgClient
    {
        private readonly NpgsqlConnection _conn;
        private string? _jwt;
        private Dictionary<string, string>? _jwtClaims;
        private readonly RLSPolicyRegistry _rlsPolicies;
        
        /// <summary>
        /// Gets the current user ID extracted from the JWT token.
        /// </summary>
        public Guid CurrentUserId { get; private set; }

        /// <summary>
        /// Initializes a new PgClient with the given Npgsql connection.
        /// Verifies connection and minimum authentication schema/policies.
        /// </summary>
        /// <param name="conn">The Npgsql database connection.</param>
        /// <param name="rlsPolicies">Optional RLS policy registry. If not provided, uses default policies.</param>
        public PgClient(NpgsqlConnection conn, RLSPolicyRegistry? rlsPolicies = null)
        {
            _conn = conn;
            _rlsPolicies = rlsPolicies ?? new RLSPolicyRegistry();
            
            // Register default policies if using default registry
            if (rlsPolicies == null)
            {
                DefaultRLSPolicies.RegisterDefaultPolicies(_rlsPolicies);
            }
            
            VerifyMinimums().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Verifies the connection and checks for required authentication tables and policies.
        /// Throws InvalidOperationException if requirements are not met.
        /// </summary>
        private async Task VerifyMinimums()
        {
            if (_conn.State != System.Data.ConnectionState.Open)
                await _conn.OpenAsync();

            // Check for account table (component table)
            var tableCheck = "SELECT to_regclass('public.account') IS NOT NULL";
            var hasAccountTable = (bool)(await new NpgsqlCommand(tableCheck, _conn).ExecuteScalarAsync() ?? false);
            if (!hasAccountTable)
                throw new InvalidOperationException("Required table 'account' does not exist.");

            // Check for password_hash column
            var colCheck = "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='account' AND column_name='password_hash')";
            var hasPasswordHash = (bool)(await new NpgsqlCommand(colCheck, _conn).ExecuteScalarAsync() ?? false);
            if (!hasPasswordHash)
                throw new InvalidOperationException("Required column 'password_hash' does not exist in 'account' table.");

            // Check for at least one RLS policy on account table
            var policyCheck = "SELECT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'account')";
            var hasPolicy = (bool)(await new NpgsqlCommand(policyCheck, _conn).ExecuteScalarAsync() ?? false);
            if (!hasPolicy)
                throw new InvalidOperationException("No RLS policy found on 'account' table. RLS must be enabled and at least one policy defined.");
        }

        /// <summary>
        /// Validates user credentials and issues a JWT if valid.
        /// Extend this method to implement RBAC/RLS logic as needed.
        /// </summary>
        /// <param name="email">User email.</param>
        /// <param name="password">Plaintext password.</param>
        /// <returns>JWT string if authentication succeeds, otherwise null.</returns>
        public async Task<string?> Authenticate(string email, string password)
        {
            // Ensure connection is open
            if (_conn.State != System.Data.ConnectionState.Open)
                await _conn.OpenAsync();
                
            // Query account component by email and check if active
            var sql = "SELECT owner_entity_id AS Id, password_hash AS PasswordHash, is_active AS IsActive FROM \"account\" WHERE email = @email";
            try
            {
                var user = await _conn.QueryFirstOrDefaultAsync<UserAuthRecord>(sql, new { email });

                if (user == null)
                {
                    Console.WriteLine($"No account found with email: {email}");
                    return null;
                }
                
                // Check if account is active
                if (!user.IsActive)
                {
                    Console.WriteLine($"Account {email} is not active");
                    return null;
                }
                
                // Check if password hash is null or empty before verification
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    return null;
                }

                // Verify password using BCrypt
                var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                
                if (!isValid)
                    return null;

                // Return a simple JWT-like token that indicates successful authentication
                // The actual JWT generation should be done by the consuming application
                // This is just to indicate that authentication was successful
                return $"auth.success.{user.Id}";
            }
            catch (Exception)
            {
                throw;
            }
        }

        private class UserAuthRecord
        {
            public Guid Id { get; set; }
            public string PasswordHash { get; set; } = "";
            public bool IsActive { get; set; } = true;
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
            if (_jwtClaims == null || _jwtClaims.Count == 0)
                return;

            await EnsureConnectionOpen();

            // Set each claim as a session variable
            foreach (var claim in _jwtClaims)
            {
                // PostgreSQL has a 63-character limit for identifiers
                // Some JWT claims (like Microsoft's) have very long names
                // We need to sanitize the claim key to be a valid PostgreSQL identifier
                var sanitizedKey = SanitizeClaimKey(claim.Key);
                var variableName = $"jwt.claims.{sanitizedKey}";
                
                // PostgreSQL custom variables need to be set with literal values
                // We escape single quotes to prevent SQL injection
                var escapedValue = claim.Value.Replace("'", "''");
                var sql = $"SET SESSION \"{variableName}\" = '{escapedValue}';";
                
                using var cmd = new NpgsqlCommand(sql, _conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Sanitizes a JWT claim key to be a valid PostgreSQL identifier.
        /// Handles common JWT claim types and shortens long names.
        /// </summary>
        private string SanitizeClaimKey(string claimKey)
        {
            // Handle common JWT claim types with long URIs
            var commonMappings = new Dictionary<string, string>
            {
                ["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] = "role",
                ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] = "sub",
                ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] = "name",
                ["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] = "email",
                ["http://schemas.microsoft.com/identity/claims/objectidentifier"] = "oid",
                ["http://schemas.microsoft.com/identity/claims/tenantid"] = "tid"
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
            
            // Truncate to 63 characters minus "jwt.claims." prefix (11 chars)
            if (sanitized.Length > 52)
                sanitized = sanitized.Substring(0, 52);
            
            return string.IsNullOrEmpty(sanitized) ? "claim" : sanitized;
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
            return new PgTable<T>(_conn, this, _rlsPolicies, _jwtClaims ?? new Dictionary<string, string>());
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

            await _conn.ExecuteAsync(sql, dynamicParams);
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

            await _conn.ExecuteAsync(sql, parameters);
        }
    }
}