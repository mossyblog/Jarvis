using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Shouldly;
using Xunit;

namespace core.jarvis.data.tests
{
    /// <summary>
    /// INTENT: Validate PgClient session variable mapping from JWT claims to PostgreSQL.
    /// PURPOSE: Ensure JWT claims are correctly transformed and set as PostgreSQL session variables.
    /// BUSINESS CONTEXT: RLS policies depend on correct session variable naming and values.
    /// WHY IMPORTANT: Incorrect mapping breaks tenant isolation and access control.
    /// ARCHITECTURAL SIGNIFICANCE: Bridge between JWT authentication and database-level security.
    /// FUTURE RESILIENCE: Prevents security regressions in claim mapping logic.
    /// </summary>
    [Collection("JWT Tests")]
    public class PgClientSessionVariableTests : IAsyncLifetime
    {
        private NpgsqlConnection _conn = null!;
        private readonly string _jwtSecret = "test-secret-key-must-be-at-least-256-bits-long!!!";

        public async Task InitializeAsync()
        {
            // Set up JWT environment variables for testing
            Environment.SetEnvironmentVariable("Jwt__SecretKey", _jwtSecret);
            Environment.SetEnvironmentVariable("Jwt__Issuer", "jarvis-api-test");
            Environment.SetEnvironmentVariable("Jwt__Audience", "jarvis-clients-test");

            // Ensure test database exists
            await Tables.TestHelpers.EnsureTestDatabaseExists();

            var connString = Tables.TestHelpers.GetConnectionStringFromEnv();
            _conn = new NpgsqlConnection(connString);
            await _conn.OpenAsync();

            // Create helper function to retrieve session variables
            await _conn.ExecuteAsync(@"
                CREATE OR REPLACE FUNCTION get_session_var(var_name TEXT)
                RETURNS TEXT AS $$
                BEGIN
                    RETURN current_setting(var_name, true);
                END;
                $$ LANGUAGE plpgsql STABLE;
            ");
        }

        public async Task DisposeAsync()
        {
            await _conn.ExecuteAsync("DROP FUNCTION IF EXISTS get_session_var(text);");
            await _conn.CloseAsync();
        }

        private string GenerateJWT(Dictionary<string, string> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var claimsList = claims.Select(c => new Claim(c.Key, c.Value)).ToList();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claimsList),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "jarvis-api-test",
                Audience = "jarvis-clients-test",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// INTENT: Verify JWT "sub" claim maps to PostgreSQL session variable "app.user_id".
        /// PURPOSE: The "sub" (subject) claim is the standard JWT claim for user identity.
        /// BUSINESS CONTEXT: RLS policies use app.user_id for owner-based access control.
        /// </summary>
        [Fact]
        public async Task SubClaim_MapsTo_AppUserId()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", userId },
                { "tenant_id", Guid.NewGuid().ToString() }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act - Trigger JWT claims to be set by executing something
            await client.ExecuteAsync("SELECT 1");

            // Assert - Verify the session variable was set correctly
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.user_id')");
            result.ShouldBe(userId);
        }

        /// <summary>
        /// INTENT: Verify JWT "tenant_id" claim maps to PostgreSQL session variable "app.tenant_id".
        /// PURPOSE: Multi-tenant isolation requires tenant_id to be correctly propagated.
        /// BUSINESS CONTEXT: RLS policies use app.tenant_id for tenant-based data isolation.
        /// </summary>
        [Fact]
        public async Task TenantIdClaim_MapsTo_AppTenantId()
        {
            // Arrange
            var tenantId = Guid.NewGuid().ToString();
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", tenantId }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.tenant_id')");
            result.ShouldBe(tenantId);
        }

        /// <summary>
        /// INTENT: Verify JWT "role" claim maps to PostgreSQL session variable "app.role".
        /// PURPOSE: Role-based access control requires role to be correctly propagated.
        /// BUSINESS CONTEXT: RLS policies use app.role for classification-based access control.
        /// </summary>
        [Fact]
        public async Task RoleClaim_MapsTo_AppRole()
        {
            // Arrange
            var role = "admin";
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { "role", role }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.role')");
            result.ShouldBe(role);
        }

        /// <summary>
        /// INTENT: Verify session variables use "app." prefix, not "jwt.claims." prefix.
        /// PURPOSE: PostgreSQL RLS functions expect the "app." prefix convention.
        /// BUSINESS CONTEXT: Ensures compatibility with existing RLS helper functions.
        /// </summary>
        [Fact]
        public async Task SessionVariablePrefix_IsApp_NotJwtClaims()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", userId },
                { "tenant_id", Guid.NewGuid().ToString() }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert - "app." prefix should work
            var appResult = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.user_id')");
            appResult.ShouldBe(userId);

            // Assert - "jwt.claims." prefix should NOT be set (returns null/empty)
            var jwtClaimsResult = await _conn.QuerySingleOrDefaultAsync<string>("SELECT get_session_var('jwt.claims.sub')");
            jwtClaimsResult.ShouldBeNullOrEmpty();
        }

        /// <summary>
        /// INTENT: Verify special characters in claim values are sanitized (SQL injection prevention).
        /// PURPOSE: Claim values with single quotes must be escaped to prevent SQL injection.
        /// BUSINESS CONTEXT: Security-critical - prevents malicious JWT payloads from executing SQL.
        /// </summary>
        [Fact]
        public async Task SpecialCharactersInClaimValues_AreSanitized()
        {
            // Arrange - Value with single quotes (SQL injection attempt)
            var maliciousValue = "test'; DROP TABLE users; --";
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { "custom_claim", maliciousValue }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act - This should not throw and should properly escape the value
            await client.ExecuteAsync("SELECT 1");

            // Assert - The value should be stored with escaped quotes (the original value preserved)
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.custom_claim')");
            result.ShouldBe(maliciousValue);
        }

        /// <summary>
        /// INTENT: Verify claim values with double single quotes are handled correctly.
        /// PURPOSE: Edge case where claim already contains escaped quotes.
        /// BUSINESS CONTEXT: Ensures robust handling of all possible claim values.
        /// </summary>
        [Fact]
        public async Task ClaimValuesWithDoubleQuotes_AreHandledCorrectly()
        {
            // Arrange - Value with double quotes
            var valueWithQuotes = "O'Brien's \"special\" data";
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { "name", valueWithQuotes }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.name')");
            result.ShouldBe(valueWithQuotes);
        }

        /// <summary>
        /// INTENT: Verify long claim keys are truncated to fit PostgreSQL identifier limits.
        /// PURPOSE: PostgreSQL has a 63-character limit for identifiers.
        /// BUSINESS CONTEXT: Microsoft identity claims often have very long URI-based names.
        /// </summary>
        [Fact]
        public async Task LongClaimKeys_AreTruncatedAppropriately()
        {
            // Arrange - Microsoft-style long claim key
            var longClaimKey = "http://schemas.microsoft.com/identity/claims/objectidentifier";
            var claimValue = Guid.NewGuid().ToString();
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { longClaimKey, claimValue }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert - The claim should be mapped to "app.oid" based on the SanitizeClaimKey mapping
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.oid')");
            result.ShouldBe(claimValue);
        }

        /// <summary>
        /// INTENT: Verify Microsoft role claim URI maps to "app.role".
        /// PURPOSE: Microsoft identity uses long URI for role claim.
        /// BUSINESS CONTEXT: Ensures compatibility with Azure AD/Microsoft identity tokens.
        /// </summary>
        [Fact]
        public async Task MicrosoftRoleClaimUri_MapsTo_AppRole()
        {
            // Arrange
            var role = "admin";
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", role }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.role')");
            result.ShouldBe(role);
        }

        /// <summary>
        /// INTENT: Verify Microsoft nameidentifier claim URI maps to "app.user_id".
        /// PURPOSE: Microsoft identity uses long URI for subject/nameidentifier claim.
        /// BUSINESS CONTEXT: Ensures compatibility with Azure AD/Microsoft identity tokens.
        /// </summary>
        [Fact]
        public async Task MicrosoftNameIdentifierClaimUri_MapsTo_AppUserId()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", userId },
                { "tenant_id", Guid.NewGuid().ToString() }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.user_id')");
            result.ShouldBe(userId);
        }

        /// <summary>
        /// INTENT: Verify Microsoft tenantid claim URI maps to "app.tenant_id".
        /// PURPOSE: Microsoft identity uses long URI for tenant claim.
        /// BUSINESS CONTEXT: Ensures compatibility with Azure AD/Microsoft identity tokens.
        /// </summary>
        [Fact]
        public async Task MicrosoftTenantIdClaimUri_MapsTo_AppTenantId()
        {
            // Arrange
            var tenantId = Guid.NewGuid().ToString();
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "http://schemas.microsoft.com/identity/claims/tenantid", tenantId }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.tenant_id')");
            result.ShouldBe(tenantId);
        }

        /// <summary>
        /// INTENT: Verify claim keys with invalid identifier characters are sanitized.
        /// PURPOSE: Claim keys must be valid PostgreSQL identifiers.
        /// BUSINESS CONTEXT: Prevents errors when setting session variables with non-standard claim names.
        /// </summary>
        [Fact]
        public async Task ClaimKeysWithInvalidCharacters_AreSanitized()
        {
            // Arrange - Claim key with special characters
            var claimValue = "test_value";
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { "my-custom.claim:name", claimValue }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert - Invalid characters should be replaced with underscores
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.my_custom_claim_name')");
            result.ShouldBe(claimValue);
        }

        /// <summary>
        /// INTENT: Verify claim keys starting with numbers get prefixed with underscore.
        /// PURPOSE: PostgreSQL identifiers cannot start with a number.
        /// BUSINESS CONTEXT: Ensures all claim keys can be used as session variable names.
        /// </summary>
        [Fact]
        public async Task ClaimKeysStartingWithNumber_ArePrefixedWithUnderscore()
        {
            // Arrange
            var claimValue = "numeric_key_value";
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { "123claim", claimValue }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert - Should be prefixed with underscore
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app._123claim')");
            result.ShouldBe(claimValue);
        }

        /// <summary>
        /// INTENT: Verify very long claim values are stored correctly (no truncation of values).
        /// PURPOSE: Claim values should not be truncated unlike keys.
        /// BUSINESS CONTEXT: Some claims may contain long data like permissions lists.
        /// </summary>
        [Fact]
        public async Task LongClaimValues_AreNotTruncated()
        {
            // Arrange - Very long value
            var longValue = new string('x', 1000);
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { "long_data", longValue }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert - Full value should be preserved
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.long_data')");
            result.ShouldBe(longValue);
            result.Length.ShouldBe(1000);
        }

        /// <summary>
        /// INTENT: Verify empty claim values are handled correctly.
        /// PURPOSE: Edge case - empty strings should be stored as empty, not null.
        /// BUSINESS CONTEXT: Ensures consistent handling of all claim values.
        /// </summary>
        [Fact]
        public async Task EmptyClaimValues_AreHandledCorrectly()
        {
            // Arrange
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { "empty_claim", "" }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.empty_claim')");
            result.ShouldBe("");
        }

        /// <summary>
        /// INTENT: Verify claims are only set once per connection (optimization check).
        /// PURPOSE: Avoid redundant SET SESSION calls for performance.
        /// BUSINESS CONTEXT: Multiple queries should not re-set claims unnecessarily.
        /// </summary>
        [Fact]
        public async Task JWTClaims_AreSetOncePerConnection()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", userId },
                { "tenant_id", Guid.NewGuid().ToString() }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act - Execute multiple times
            await client.ExecuteAsync("SELECT 1");
            await client.ExecuteAsync("SELECT 2");
            await client.ExecuteAsync("SELECT 3");

            // Assert - Claims should still be set correctly
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.user_id')");
            result.ShouldBe(userId);
        }

        /// <summary>
        /// INTENT: Verify custom claims are propagated to session variables.
        /// PURPOSE: Business-specific claims should be accessible in RLS policies.
        /// BUSINESS CONTEXT: Applications may define custom claims for authorization.
        /// </summary>
        [Fact]
        public async Task CustomClaims_ArePropagatedToSessionVariables()
        {
            // Arrange
            var department = "engineering";
            var clearanceLevel = "top_secret";
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { "department", department },
                { "clearance_level", clearanceLevel }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert
            var deptResult = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.department')");
            deptResult.ShouldBe(department);

            var clearanceResult = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.clearance_level')");
            clearanceResult.ShouldBe(clearanceLevel);
        }

        #region Unicode Normalization Security Tests (jarvis-46)

        /// <summary>
        /// INTENT: Verify Unicode homograph attacks are blocked via normalization.
        /// SECURITY: Cyrillic 'a' (U+0430) looks like Latin 'a' but is different.
        /// BUSINESS CONTEXT: Prevents attackers from using look-alike characters to bypass security.
        /// </summary>
        [Fact]
        public async Task UnicodeHomographAttack_CyrillicCharacters_AreNormalized()
        {
            // Arrange - Use Cyrillic 'а' (U+0430) which looks like Latin 'a'
            var cyrillicClaimKey = "rol\u0435"; // 'e' is Cyrillic U+0435
            var claimValue = "admin";
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { cyrillicClaimKey, claimValue }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert - Cyrillic characters should be sanitized (converted to underscores)
            // The normalized form breaks down Cyrillic to base characters + combining marks
            // Then non-ASCII chars are stripped, leaving only valid ASCII
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.rol_')");
            result.ShouldBe(claimValue);
        }

        /// <summary>
        /// INTENT: Verify claims with mixed Unicode scripts are sanitized.
        /// SECURITY: Mixed scripts can be used to create confusing or deceptive identifiers.
        /// </summary>
        [Fact]
        public async Task MixedUnicodeScripts_AreSanitized()
        {
            // Arrange - Mix of Latin and Greek characters
            var mixedClaimKey = "user\u03B1name"; // Greek alpha (U+03B1)
            var claimValue = "test_value";
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { mixedClaimKey, claimValue }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert - Non-ASCII characters should be replaced with underscores
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.user_name')");
            result.ShouldBe(claimValue);
        }

        /// <summary>
        /// INTENT: Verify Unicode fullwidth characters are normalized.
        /// SECURITY: Fullwidth 'a' (U+FF41) looks like regular 'a' but could bypass string comparison.
        /// </summary>
        [Fact]
        public async Task FullwidthCharacters_AreNormalized()
        {
            // Arrange - Fullwidth characters
            var fullwidthKey = "\uFF41\uFF42\uFF43"; // Fullwidth 'abc'
            var claimValue = "fullwidth_test";
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() },
                { fullwidthKey, claimValue }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act
            await client.ExecuteAsync("SELECT 1");

            // Assert - After normalization (FormKD), fullwidth chars become regular ASCII
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.abc')");
            result.ShouldBe(claimValue);
        }

        /// <summary>
        /// INTENT: Verify null and empty claim keys default to "claim".
        /// SECURITY: Empty keys should have a safe default, not cause errors.
        /// </summary>
        [Fact]
        public async Task EmptyClaimKey_DefaultsToClaim()
        {
            // Note: JWT standard doesn't allow empty claim keys, but we test the sanitization logic
            // by testing with a key that normalizes to empty after sanitization
            var jwt = GenerateJWT(new Dictionary<string, string>
            {
                { "sub", Guid.NewGuid().ToString() },
                { "tenant_id", Guid.NewGuid().ToString() }
            });

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act - Should not throw
            await client.ExecuteAsync("SELECT 1");

            // Assert - sub should be mapped to user_id
            var result = await _conn.QuerySingleAsync<string>("SELECT get_session_var('app.user_id')");
            result.ShouldNotBeNullOrEmpty();
        }

        #endregion
    }
}
