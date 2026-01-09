using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Shouldly;
using Xunit;

namespace core.jarvis.data.tests.Tables
{
    /// <summary>
    /// INTENT: Verify that JWT claim values cannot be used to inject SQL via session variables.
    /// PURPOSE: Security validation test suite for session variable SQL injection prevention.
    /// BUSINESS CONTEXT: Ensures JWT claims are safely parameterized when set as PostgreSQL session variables.
    /// WHY IMPORTANT: Session variable injection could allow privilege escalation or data exfiltration.
    /// ARCHITECTURAL SIGNIFICANCE: Validates parameterized queries in session variable handling.
    /// FUTURE RESILIENCE: Protects against regressions in JWT claim handling security.
    /// </summary>
    [Collection("JWT Tests")]
    public class JwtSessionVariableInjectionTests : IAsyncLifetime
    {
        private NpgsqlConnection _conn = null!;
        private PgClient _client = null!;
        private readonly string _secretKey = "ThisIsATestSecretKeyForJWTSessionVariableInjectionTestingPurposesOnly!123456";

        public async Task InitializeAsync()
        {
            // Set up JWT environment variables for testing
            Environment.SetEnvironmentVariable("Jwt__SecretKey", _secretKey);
            Environment.SetEnvironmentVariable("Jwt__Issuer", "jarvis-api-test");
            Environment.SetEnvironmentVariable("Jwt__Audience", "jarvis-clients-test");

            var connString = TestHelpers.GetConnectionStringFromEnv();
            _conn = new NpgsqlConnection(connString);
            await _conn.OpenAsync();

            // Create a test table that will be targeted by injection attempts
            var createTablesSql = @"
                DROP TABLE IF EXISTS session_injection_test CASCADE;
                CREATE TABLE session_injection_test (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    data TEXT NOT NULL,
                    secret_flag BOOLEAN DEFAULT false
                );

                -- Insert test data
                INSERT INTO session_injection_test (data, secret_flag) VALUES
                    ('Normal data', false),
                    ('Secret data', true);
            ";

            using (var cmd = new NpgsqlCommand(createTablesSql, _conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await PgClientFactory.EnsureMinimumSchema(_conn);
            _client = new PgClient(_conn);
        }

        public async Task DisposeAsync()
        {
            var dropSql = "DROP TABLE IF EXISTS session_injection_test CASCADE;";
            using var cmd = new NpgsqlCommand(dropSql, _conn);
            await cmd.ExecuteNonQueryAsync();
            await _conn.CloseAsync();
        }

        /// <summary>
        /// INTENT: Test that SQL injection via JWT claim values is prevented.
        /// PURPOSE: Verify parameterized set_config() blocks SQL injection in session variables.
        /// BUSINESS CONTEXT: Attackers may craft JWT claims with SQL injection payloads.
        /// WHY IMPORTANT: Session variables are set from JWT claims and must be secure.
        /// ARCHITECTURAL SIGNIFICANCE: Validates the core security fix for SEC-01.
        /// FUTURE RESILIENCE: Ensures parameterization remains effective.
        /// </summary>
        [Fact]
        public async Task SetJwtClaims_WithSqlInjectionPayloads_AreParameterizedSafely()
        {
            // Arrange - Various SQL injection attempts via JWT claim values
            // Note: Null byte (\0) is intentionally excluded because PostgreSQL
            // rejects invalid UTF-8 bytes at the encoding layer before any SQL execution,
            // which is a security feature that prevents null byte injection attacks.
            var injectionPayloads = new[]
            {
                // Classic SQL injection
                "'; DROP TABLE session_injection_test; --",
                // SQL injection with UPDATE
                "'; UPDATE session_injection_test SET secret_flag = true; --",
                // SQL injection with boolean manipulation
                "' OR '1'='1",
                // SQL injection with UNION
                "' UNION SELECT * FROM pg_user; --",
                // SQL injection with nested quotes
                "test''; DELETE FROM session_injection_test; --",
                // SQL injection attempting to break out of set_config
                "value'); DROP TABLE session_injection_test; SELECT set_config('app.x', '",
                // SQL injection with PostgreSQL dollar quoting
                "$tag$; DROP TABLE session_injection_test; $tag$",
                // SQL injection with semicolon
                "value; DROP TABLE session_injection_test",
                // SQL injection with newline
                "value\n; DROP TABLE session_injection_test; --"
            };

            foreach (var payload in injectionPayloads)
            {
                // Create a fresh connection and client for each payload to ensure clean state
                await using var testConn = new NpgsqlConnection(TestHelpers.GetConnectionStringFromEnv());
                await testConn.OpenAsync();
                var testClient = new PgClient(testConn);

                // Act - Set JWT with injection payload in user_id claim
                var token = CreateTestJWT(new Dictionary<string, string>
                {
                    { "user_id", payload },
                    { "tenant_id", "00000000-0000-0000-0000-000000000001" },
                    { "role", "user" }
                });

                // This should NOT throw - the payload should be safely parameterized
                await Should.NotThrowAsync(async () =>
                {
                    testClient.JWT(token);
                    // Force claims to be set by accessing a table
                    await testClient.From<SessionInjectionTest>().Get();
                });

                // Assert - Verify table still exists and has original data
                var verifyTableSql = "SELECT COUNT(*) FROM session_injection_test";
                using var verifyCmd = new NpgsqlCommand(verifyTableSql, testConn);
                var count = (long)(await verifyCmd.ExecuteScalarAsync() ?? 0);
                count.ShouldBe(2, $"Table data was modified by injection payload: {payload}");

                // Verify the session variable was set with the literal payload value
                var checkVarSql = "SELECT current_setting('app.user_id', true)";
                using var checkCmd = new NpgsqlCommand(checkVarSql, testConn);
                var sessionValue = (string?)(await checkCmd.ExecuteScalarAsync());
                sessionValue.ShouldBe(payload, "Session variable should contain the literal payload, not execute it");
            }
        }

        /// <summary>
        /// INTENT: Test that SQL injection via JWT claim keys (custom claims) is prevented.
        /// PURPOSE: Verify claim key sanitization prevents injection.
        /// BUSINESS CONTEXT: Custom claim names might contain malicious content.
        /// WHY IMPORTANT: Claim keys become part of session variable names.
        /// ARCHITECTURAL SIGNIFICANCE: Validates claim key sanitization.
        /// FUTURE RESILIENCE: Ensures key sanitization remains effective.
        /// </summary>
        [Fact]
        public async Task SetJwtClaims_WithInjectionInClaimKeys_AreSanitized()
        {
            // Arrange - Injection attempts via custom claim keys
            // Note: Claim keys go through SanitizeClaimKey which strips non-alphanumeric chars
            var customClaims = new Dictionary<string, string>
            {
                { "custom'; DROP TABLE session_injection_test; --", "value1" },
                { "my_claim\" OR 1=1 --", "value2" },
                { "http://malicious.com/claim'; DELETE *; --", "value3" },
                { "user_id", "safe-user-id" },
                { "tenant_id", "00000000-0000-0000-0000-000000000001" },
                { "role", "user" }
            };

            var token = CreateTestJWT(customClaims);

            // Act
            await Should.NotThrowAsync(async () =>
            {
                _client.JWT(token);
                await _client.From<SessionInjectionTest>().Get();
            });

            // Assert - Table should still exist with original data
            var verifyTableSql = "SELECT COUNT(*) FROM session_injection_test";
            using var verifyCmd = new NpgsqlCommand(verifyTableSql, _conn);
            var count = (long)(await verifyCmd.ExecuteScalarAsync() ?? 0);
            count.ShouldBe(2, "Table data was modified by injection in claim key");
        }

        /// <summary>
        /// INTENT: Test that multi-statement injection is blocked.
        /// PURPOSE: Verify that statement separators in claim values don't execute.
        /// BUSINESS CONTEXT: Attackers may try to append statements using semicolons.
        /// WHY IMPORTANT: Multi-statement execution could be catastrophic.
        /// ARCHITECTURAL SIGNIFICANCE: Validates parameterization blocks statement chaining.
        /// FUTURE RESILIENCE: Ensures no bypass via statement chaining.
        /// </summary>
        [Fact]
        public async Task SetJwtClaims_WithMultiStatementInjection_ExecutesOnlySetConfig()
        {
            // Arrange - Multi-statement injection attempts
            var payload = "value'; DROP TABLE session_injection_test; SELECT set_config('app.x', 'y', false";

            var token = CreateTestJWT(new Dictionary<string, string>
            {
                { "user_id", payload },
                { "tenant_id", "00000000-0000-0000-0000-000000000001" },
                { "role", "admin" }
            });

            // Act
            _client.JWT(token);
            await _client.From<SessionInjectionTest>().Get();

            // Assert - Table should still exist
            var verifyTableSql = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'session_injection_test')";
            using var verifyCmd = new NpgsqlCommand(verifyTableSql, _conn);
            var exists = (bool)(await verifyCmd.ExecuteScalarAsync() ?? false);
            exists.ShouldBeTrue("Table was dropped by multi-statement injection");

            // Verify data integrity
            var countSql = "SELECT COUNT(*) FROM session_injection_test";
            using var countCmd = new NpgsqlCommand(countSql, _conn);
            var count = (long)(await countCmd.ExecuteScalarAsync() ?? 0);
            count.ShouldBe(2, "Table data was modified by multi-statement injection");
        }

        /// <summary>
        /// INTENT: Test that Unicode and special character injection is handled safely.
        /// PURPOSE: Verify encoding-based attacks don't bypass parameterization.
        /// BUSINESS CONTEXT: Attackers may use Unicode tricks to bypass filters.
        /// WHY IMPORTANT: Encoding attacks are a common bypass technique.
        /// ARCHITECTURAL SIGNIFICANCE: Validates robust character handling.
        /// FUTURE RESILIENCE: Ensures no bypass via encoding tricks.
        /// </summary>
        [Fact]
        public async Task SetJwtClaims_WithUnicodeInjection_AreSafelyParameterized()
        {
            // Arrange - Unicode and special character injection attempts
            var unicodePayloads = new[]
            {
                // Unicode quote variations
                "\u0027; DROP TABLE session_injection_test; --", // Unicode single quote
                "\u02BC; DROP TABLE session_injection_test; --", // Modifier letter apostrophe
                "value\u0000'; DROP TABLE session_injection_test; --", // Null byte
                // Full-width characters
                "\uFF07; DROP TABLE session_injection_test; --", // Fullwidth apostrophe
                // Combining characters
                "val\u0301; DROP TABLE session_injection_test; --", // Combining acute accent
                // Control characters
                "value\x00\x1b[31m'; DROP TABLE session_injection_test; --"
            };

            foreach (var payload in unicodePayloads)
            {
                var token = CreateTestJWT(new Dictionary<string, string>
                {
                    { "user_id", payload },
                    { "tenant_id", "00000000-0000-0000-0000-000000000001" },
                    { "role", "user" }
                });

                // Act
                await Should.NotThrowAsync(async () =>
                {
                    _client.JWT(token);
                    await _client.From<SessionInjectionTest>().Get();
                });

                // Assert - Table should still exist with original data
                var verifyTableSql = "SELECT COUNT(*) FROM session_injection_test";
                using var verifyCmd = new NpgsqlCommand(verifyTableSql, _conn);
                var count = (long)(await verifyCmd.ExecuteScalarAsync() ?? 0);
                count.ShouldBe(2, $"Table data was modified by Unicode injection: {BitConverter.ToString(Encoding.UTF8.GetBytes(payload))}");
            }
        }

        /// <summary>
        /// INTENT: Test that session variables are correctly set with legitimate values.
        /// PURPOSE: Verify the fix doesn't break normal functionality.
        /// BUSINESS CONTEXT: Valid JWT claims must still work correctly.
        /// WHY IMPORTANT: Security fixes must not break legitimate use cases.
        /// ARCHITECTURAL SIGNIFICANCE: Validates end-to-end session variable functionality.
        /// FUTURE RESILIENCE: Ensures parameterization doesn't affect valid data.
        /// </summary>
        [Fact]
        public async Task SetJwtClaims_WithLegitimateValues_SetsSessionVariablesCorrectly()
        {
            // Arrange
            var userId = "11111111-1111-1111-1111-111111111111";
            var tenantId = "00000000-0000-0000-0000-000000000001";
            var role = "admin";

            var token = CreateTestJWT(new Dictionary<string, string>
            {
                { "user_id", userId },
                { "tenant_id", tenantId },
                { "role", role }
            });

            // Act
            _client.JWT(token);
            await _client.From<SessionInjectionTest>().Get();

            // Assert - Verify session variables are set correctly
            var checkUserIdSql = "SELECT current_setting('app.user_id', true)";
            using var checkUserIdCmd = new NpgsqlCommand(checkUserIdSql, _conn);
            var sessionUserId = (string?)(await checkUserIdCmd.ExecuteScalarAsync());
            sessionUserId.ShouldBe(userId);

            var checkTenantIdSql = "SELECT current_setting('app.tenant_id', true)";
            using var checkTenantIdCmd = new NpgsqlCommand(checkTenantIdSql, _conn);
            var sessionTenantId = (string?)(await checkTenantIdCmd.ExecuteScalarAsync());
            sessionTenantId.ShouldBe(tenantId);

            var checkRoleSql = "SELECT current_setting('app.role', true)";
            using var checkRoleCmd = new NpgsqlCommand(checkRoleSql, _conn);
            var sessionRole = (string?)(await checkRoleCmd.ExecuteScalarAsync());
            sessionRole.ShouldBe(role);
        }

        /// <summary>
        /// INTENT: Test that special characters in legitimate claim values are preserved.
        /// PURPOSE: Verify special characters work without being interpreted as SQL.
        /// BUSINESS CONTEXT: Legitimate values may contain quotes, special chars.
        /// WHY IMPORTANT: Data integrity must be maintained.
        /// ARCHITECTURAL SIGNIFICANCE: Validates proper value handling.
        /// FUTURE RESILIENCE: Ensures no data loss or corruption.
        /// </summary>
        [Fact]
        public async Task SetJwtClaims_WithSpecialCharactersInValues_PreservesData()
        {
            // Arrange - Values with special characters that should be preserved, not executed
            var specialValues = new[]
            {
                "O'Reilly", // Common name with apostrophe
                "Test \"quoted\" value", // Double quotes
                "User's \"special\" data", // Mixed quotes
                "SELECT * FROM users;", // SQL-like text (not injection)
                "foo\nbar\ttab", // Whitespace characters
                "<script>alert('xss')</script>", // XSS-like text
                "100% complete", // Percentage
                "a=b&c=d", // Query string like
                "{\"json\": \"value\"}", // JSON
                "path\\to\\file" // Backslashes
            };

            foreach (var value in specialValues)
            {
                // Create a fresh connection and client for each value to ensure clean state
                await using var testConn = new NpgsqlConnection(TestHelpers.GetConnectionStringFromEnv());
                await testConn.OpenAsync();
                var testClient = new PgClient(testConn);

                var token = CreateTestJWT(new Dictionary<string, string>
                {
                    { "user_id", value },
                    { "tenant_id", "00000000-0000-0000-0000-000000000001" },
                    { "role", "user" }
                });

                // Act
                testClient.JWT(token);
                await testClient.From<SessionInjectionTest>().Get();

                // Assert - Verify the value was preserved exactly
                var checkVarSql = "SELECT current_setting('app.user_id', true)";
                using var checkCmd = new NpgsqlCommand(checkVarSql, testConn);
                var sessionValue = (string?)(await checkCmd.ExecuteScalarAsync());
                sessionValue.ShouldBe(value, $"Value was not preserved correctly: {value}");
            }
        }

        // Helper method to create test JWTs
        private string CreateTestJWT(Dictionary<string, string> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claimsList = new List<Claim>();
            foreach (var claim in claims)
            {
                claimsList.Add(new Claim(claim.Key, claim.Value));
            }

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
    }

    /// <summary>
    /// Test entity for session injection tests.
    /// Named to match table "session_injection_test" (snake_case conversion)
    /// </summary>
    public class SessionInjectionTest
    {
        public Guid Id { get; set; }
        public string Data { get; set; } = "";
        public bool SecretFlag { get; set; }
    }
}
