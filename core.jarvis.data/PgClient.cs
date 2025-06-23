using System.IdentityModel.Tokens.Jwt;
using core.jarvis.data.RLS;
using Dapper;
using Npgsql;

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

            // Check for users table
            var tableCheck = "SELECT to_regclass('public.users') IS NOT NULL";
            var hasUsersTable = (bool)(await new NpgsqlCommand(tableCheck, _conn).ExecuteScalarAsync() ?? false);
            if (!hasUsersTable)
                throw new InvalidOperationException("Required table 'users' does not exist.");

            // Check for password_hash column
            var colCheck = "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='password_hash')";
            var hasPasswordHash = (bool)(await new NpgsqlCommand(colCheck, _conn).ExecuteScalarAsync() ?? false);
            if (!hasPasswordHash)
                throw new InvalidOperationException("Required column 'password_hash' does not exist in 'users' table.");

            // Check for at least one RLS policy on users table
            var policyCheck = "SELECT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'users')";
            var hasPolicy = (bool)(await new NpgsqlCommand(policyCheck, _conn).ExecuteScalarAsync() ?? false);
            if (!hasPolicy)
                throw new InvalidOperationException("No RLS policy found on 'users' table. RLS must be enabled and at least one policy defined.");
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
                
            // Example: Query user by email
            var sql = "SELECT id, password_hash AS PasswordHash FROM users WHERE email = @email";
            try
            {
                var user = await _conn.QueryFirstOrDefaultAsync<UserAuthRecord>(sql, new { email });

                if (user == null)
                {
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
        /// Parses JWT claims without validation (for RLS purposes).
        /// In production, you should validate the JWT signature.
        /// </summary>
        private Dictionary<string, string> ParseJWTClaims(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(jwt);
            
            var claims = new Dictionary<string, string>();
            foreach (var claim in jsonToken.Claims)
            {
                // Store the last value if there are duplicates
                claims[claim.Type] = claim.Value;
            }
            
            return claims;
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
                // PostgreSQL custom variables need to be set with literal values
                // We escape single quotes to prevent SQL injection
                var variableName = $"jwt.claims.{claim.Key}";
                var escapedValue = claim.Value.Replace("'", "''");
                var sql = $"SET SESSION \"{variableName}\" = '{escapedValue}';";
                
                using var cmd = new NpgsqlCommand(sql, _conn);
                await cmd.ExecuteNonQueryAsync();
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
    }
}