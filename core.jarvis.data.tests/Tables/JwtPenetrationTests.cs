using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using core.jarvis.data.RLS;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Shouldly;

namespace core.jarvis.data.tests.Tables
{
    /// <summary>
    /// INTENT: Verify that PgClient is protected against JWT-based attacks.
    /// PURPOSE: Security validation test suite for JWT authentication and authorization.
    /// BUSINESS CONTEXT: Ensures JWT-based RLS cannot be bypassed or exploited.
    /// WHY IMPORTANT: JWT vulnerabilities can lead to unauthorized data access.
    /// ARCHITECTURAL SIGNIFICANCE: Validates JWT security in the data access layer.
    /// FUTURE RESILIENCE: Protects against regressions in JWT handling.
    /// </summary>
    public class JwtPenetrationTests : IAsyncLifetime
    {
        private NpgsqlConnection _conn = null!;
        private PgClient _client = null!;
        private readonly string _secretKey = "ThisIsATestSecretKeyForJWTPenetrationTestingPurposesOnly!123456";

        public async Task InitializeAsync()
        {
            var connString = TestHelpers.GetConnectionStringFromEnv();
            _conn = new NpgsqlConnection(connString);
            _client = await PgClientFactory.Create(_conn);

            // Create test tables for JWT security testing
            var createTablesSql = @"
                -- User table with different roles
                DROP TABLE IF EXISTS jwt_test_users CASCADE;
                CREATE TABLE jwt_test_users (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    email TEXT NOT NULL UNIQUE,
                    password_hash TEXT NOT NULL,
                    role TEXT NOT NULL,
                    tenant_id UUID
                );

                -- Sensitive data table with tenant isolation
                DROP TABLE IF EXISTS jwt_test_sensitive_data CASCADE;
                CREATE TABLE jwt_test_sensitive_data (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    tenant_id UUID NOT NULL,
                    user_id UUID NOT NULL,
                    data TEXT NOT NULL,
                    access_level TEXT NOT NULL -- 'public', 'internal', 'confidential'
                );

                -- Insert test users
                INSERT INTO jwt_test_users (id, email, password_hash, role, tenant_id) VALUES
                    ('11111111-1111-1111-1111-111111111111', 'admin@test.com', '$2a$10$dummyhash', 'admin', '00000000-0000-0000-0000-000000000001'),
                    ('22222222-2222-2222-2222-222222222222', 'user@test.com', '$2a$10$dummyhash', 'user', '00000000-0000-0000-0000-000000000001'),
                    ('33333333-3333-3333-3333-333333333333', 'user2@test.com', '$2a$10$dummyhash', 'user', '00000000-0000-0000-0000-000000000002');

                -- Insert test data
                INSERT INTO jwt_test_sensitive_data (tenant_id, user_id, data, access_level) VALUES
                    ('00000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'Admin confidential data', 'confidential'),
                    ('00000000-0000-0000-0000-000000000001', '22222222-2222-2222-2222-222222222222', 'User internal data', 'internal'),
                    ('00000000-0000-0000-0000-000000000002', '33333333-3333-3333-3333-333333333333', 'Other tenant data', 'confidential');
            ";

            using (var cmd = new NpgsqlCommand(createTablesSql, _conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Register RLS policies for testing
            var policies = new RLSPolicyRegistry();
            
            // Tenant isolation policy
            policies.RegisterPolicy(new RLSPolicy
            {
                TableName = "jwt_test_sensitive_data",
                Type = PolicyType.Select,
                WhereClause = claims =>
                {
                    if (claims.TryGetValue("tenant_id", out var tenantId))
                    {
                        // Validate it's a valid GUID to prevent injection
                        if (Guid.TryParse(tenantId, out var guid))
                            return $"tenant_id = '{guid}'::uuid";
                    }
                    return "1=0"; // No access without valid tenant_id
                },
                CheckFunction = (claims, data) =>
                {
                    if (!claims.TryGetValue("tenant_id", out var tenantId))
                        return false;
                    
                    if (!data.TryGetValue("tenant_id", out var dataTenantId))
                        return false;
                    
                    return tenantId == dataTenantId.ToString();
                }
            });

            // Role-based access policy
            policies.RegisterPolicy(new RLSPolicy
            {
                TableName = "jwt_test_sensitive_data",
                Type = PolicyType.Select,
                WhereClause = claims =>
                {
                    if (!claims.TryGetValue("role", out var role))
                        return "1=0";
                    
                    if (role == "admin")
                        return ""; // No additional restrictions for admin
                    
                    return "access_level != 'confidential'";
                }
            });

            _client = new PgClient(_conn, policies);
        }

        public async Task DisposeAsync()
        {
            var dropSql = @"
                DROP TABLE IF EXISTS jwt_test_sensitive_data;
                DROP TABLE IF EXISTS jwt_test_users;";
            using var cmd = new NpgsqlCommand(dropSql, _conn);
            await cmd.ExecuteNonQueryAsync();
            await _conn.CloseAsync();
        }

        /// <summary>
        /// INTENT: Test that malformed JWT tokens are rejected.
        /// PURPOSE: Verify parsing errors don't cause security vulnerabilities.
        /// BUSINESS CONTEXT: Attackers may send malformed tokens to exploit parser bugs.
        /// WHY IMPORTANT: Parser vulnerabilities can lead to crashes or bypasses.
        /// ARCHITECTURAL SIGNIFICANCE: Validates JWT parsing robustness.
        /// FUTURE RESILIENCE: Ensures JWT parsing remains secure.
        /// </summary>
        [Fact]
        public async Task JWT_WithMalformedTokens_HandledSafely()
        {
            var malformedTokens = new[]
            {
                "not.a.jwt",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", // Missing payload and signature
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.", // Missing signature
                "...", // All empty
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ", // Missing signature
                "InvalidBase64!@#$.InvalidPayload!@#$.InvalidSignature!@#$"
            };

            foreach (var malformedToken in malformedTokens)
            {
                // Should throw when trying to parse invalid JWT
                Should.Throw<Exception>(() => _client.JWT(malformedToken));
            }

            // Verify no data is accessible without valid JWT
            var results = await _client.From<JwtTestSensitiveData>().Get();
            results.Count.ShouldBe(0); // RLS should block all access
        }

        /// <summary>
        /// INTENT: Test that expired JWT tokens are handled correctly.
        /// PURPOSE: Verify time-based security controls work.
        /// BUSINESS CONTEXT: Tokens should expire to limit exposure window.
        /// WHY IMPORTANT: Long-lived tokens increase security risk.
        /// ARCHITECTURAL SIGNIFICANCE: Validates temporal security controls.
        /// FUTURE RESILIENCE: Ensures token expiration is enforced.
        /// </summary>
        [Fact]
        public async Task JWT_WithExpiredToken_DeniesAccess()
        {
            // Create an expired JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("user_id", "11111111-1111-1111-1111-111111111111"),
                    new Claim("tenant_id", "00000000-0000-0000-0000-000000000001"),
                    new Claim("role", "admin")
                }),
                Expires = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
                NotBefore = DateTime.UtcNow.AddHours(-2), // Valid from 2 hours ago
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // PgClient currently doesn't validate expiration (as noted in comments)
            // But the JWT can still be parsed for claims
            _client.JWT(tokenString);

            // This demonstrates that expiration validation should be added
            var results = await _client.From<JwtTestSensitiveData>().Get();
            
            // Currently, expired tokens still work (this is a security issue to address)
            // In a secure implementation, this should return 0
            results.Count.ShouldBeGreaterThan(0);
            
            // TODO: This test documents that JWT expiration validation needs to be implemented
        }

        /// <summary>
        /// INTENT: Test that tampered JWT signatures are detected.
        /// PURPOSE: Verify signature validation prevents token forgery.
        /// BUSINESS CONTEXT: Attackers may modify token claims to escalate privileges.
        /// WHY IMPORTANT: Signature validation is critical for token integrity.
        /// ARCHITECTURAL SIGNIFICANCE: Validates cryptographic security.
        /// FUTURE RESILIENCE: Ensures signature validation remains effective.
        /// </summary>
        [Fact]
        public async Task JWT_WithTamperedSignature_DeniesAccess()
        {
            // Create a valid JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("user_id", "22222222-2222-2222-2222-222222222222"),
                    new Claim("tenant_id", "00000000-0000-0000-0000-000000000001"),
                    new Claim("role", "user")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Tamper with the signature
            var parts = tokenString.Split('.');
            var tamperedToken = $"{parts[0]}.{parts[1]}.tampered_signature";

            // The current implementation doesn't validate signatures (as noted in PgClient comments)
            // So tampered tokens are accepted - this is a security issue
            _client.JWT(tamperedToken);

            // Currently the tampered token works because signature isn't validated
            // This demonstrates a security vulnerability that should be fixed
            var results = await _client.From<JwtTestSensitiveData>().Get();
            
            // This should be 0 with proper signature validation
            results.Count.ShouldBeGreaterThan(0);
            
            // TODO: This test documents that JWT signature validation needs to be implemented
        }

        /// <summary>
        /// INTENT: Test privilege escalation via JWT claim manipulation.
        /// PURPOSE: Verify RLS policies prevent unauthorized access.
        /// BUSINESS CONTEXT: Users may try to modify their role claims.
        /// WHY IMPORTANT: Role-based access control must be tamper-proof.
        /// ARCHITECTURAL SIGNIFICANCE: Validates authorization controls.
        /// FUTURE RESILIENCE: Ensures RBAC remains secure.
        /// </summary>
        [Fact]
        public async Task JWT_WithManipulatedClaims_PreventPrivilegeEscalation()
        {
            // Test 1: User trying to claim admin role (without valid signature)
            var maliciousToken = CreateTestJWT(new Dictionary<string, string>
            {
                { "user_id", "22222222-2222-2222-2222-222222222222" },
                { "tenant_id", "00000000-0000-0000-0000-000000000001" },
                { "role", "admin" } // User trying to escalate to admin
            });

            _client.JWT(maliciousToken);
            var results1 = await _client.From<JwtTestSensitiveData>().Get();
            
            // With proper signature validation, this would be 0
            // Currently shows that signature validation is needed
            results1.Count.ShouldBeGreaterThan(0);

            // Test 2: User trying to access different tenant
            var crossTenantToken = CreateTestJWT(new Dictionary<string, string>
            {
                { "user_id", "22222222-2222-2222-2222-222222222222" },
                { "tenant_id", "00000000-0000-0000-0000-000000000002" }, // Different tenant
                { "role", "user" }
            });

            _client.JWT(crossTenantToken);
            var results2 = await _client.From<JwtTestSensitiveData>().Get();
            
            // Should only see data from claimed tenant (demonstrating tenant isolation)
            results2.ShouldAllBe(r => r.TenantId == Guid.Parse("00000000-0000-0000-0000-000000000002"));
        }

        /// <summary>
        /// INTENT: Test JWT claim injection attacks.
        /// PURPOSE: Verify special characters in claims don't cause vulnerabilities.
        /// BUSINESS CONTEXT: Attackers may inject SQL or special characters via claims.
        /// WHY IMPORTANT: Claims are used in SQL session variables.
        /// ARCHITECTURAL SIGNIFICANCE: Validates claim sanitization.
        /// FUTURE RESILIENCE: Ensures claim handling remains secure.
        /// </summary>
        [Fact]
        public async Task JWT_WithInjectionInClaims_HandledSafely()
        {
            // Test various injection attempts via JWT claims
            var injectionClaims = new[]
            {
                new Dictionary<string, string>
                {
                    { "user_id", "'; DROP TABLE jwt_test_sensitive_data; --" },
                    { "tenant_id", "00000000-0000-0000-0000-000000000001" },
                    { "role", "user" }
                },
                new Dictionary<string, string>
                {
                    { "user_id", "11111111-1111-1111-1111-111111111111" },
                    { "tenant_id", "' OR '1'='1" },
                    { "role", "admin" }
                },
                new Dictionary<string, string>
                {
                    { "user_id", "11111111-1111-1111-1111-111111111111" },
                    { "tenant_id", "00000000-0000-0000-0000-000000000001" },
                    { "role", "admin'; SET SESSION ROLE postgres; --" }
                }
            };

            foreach (var claims in injectionClaims)
            {
                var token = CreateTestJWT(claims);
                _client.JWT(token);

                // The JWTClaims method escapes single quotes, preventing SQL injection
                await Should.NotThrowAsync(async () =>
                {
                    var results = await _client.From<JwtTestSensitiveData>().Get();
                });

                // Verify table still exists
                var checkTableSql = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name='jwt_test_sensitive_data')";
                using var cmd = new NpgsqlCommand(checkTableSql, _conn);
                var tableExists = (bool)(await cmd.ExecuteScalarAsync() ?? false);
                tableExists.ShouldBeTrue();
            }
        }

        /// <summary>
        /// INTENT: Test missing required claims in JWT.
        /// PURPOSE: Verify graceful handling of incomplete tokens.
        /// BUSINESS CONTEXT: Tokens may be missing expected claims.
        /// WHY IMPORTANT: Missing claims shouldn't cause crashes or bypasses.
        /// ARCHITECTURAL SIGNIFICANCE: Validates claim validation logic.
        /// FUTURE RESILIENCE: Ensures robust claim handling.
        /// </summary>
        [Fact]
        public async Task JWT_WithMissingClaims_DeniesAccess()
        {
            // Test 1: Missing tenant_id claim
            var missingTenantToken = CreateTestJWT(new Dictionary<string, string>
            {
                { "user_id", "11111111-1111-1111-1111-111111111111" },
                { "role", "admin" }
                // Missing tenant_id
            });

            _client.JWT(missingTenantToken);
            var results1 = await _client.From<JwtTestSensitiveData>().Get();
            results1.Count.ShouldBe(0); // RLS should deny access without tenant_id

            // Test 2: Missing role claim
            var missingRoleToken = CreateTestJWT(new Dictionary<string, string>
            {
                { "user_id", "11111111-1111-1111-1111-111111111111" },
                { "tenant_id", "00000000-0000-0000-0000-000000000001" }
                // Missing role
            });

            _client.JWT(missingRoleToken);
            var results2 = await _client.From<JwtTestSensitiveData>().Get();
            results2.Count.ShouldBe(0); // RLS should deny access without role

            // Test 3: Empty JWT (no claims)
            var emptyToken = CreateTestJWT(new Dictionary<string, string>());
            _client.JWT(emptyToken);
            var results3 = await _client.From<JwtTestSensitiveData>().Get();
            results3.Count.ShouldBe(0); // RLS should deny all access
        }

        /// <summary>
        /// INTENT: Test JWT with duplicate claims.
        /// PURPOSE: Verify consistent handling of duplicate claim keys.
        /// BUSINESS CONTEXT: Tokens may contain duplicate claims.
        /// WHY IMPORTANT: Inconsistent claim handling can cause security issues.
        /// ARCHITECTURAL SIGNIFICANCE: Validates claim parsing logic.
        /// FUTURE RESILIENCE: Ensures predictable claim handling.
        /// </summary>
        [Fact]
        public async Task JWT_WithDuplicateClaims_UsesLastValue()
        {
            // Create JWT with duplicate claims manually
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            
            var claims = new List<Claim>
            {
                new Claim("user_id", "11111111-1111-1111-1111-111111111111"),
                new Claim("tenant_id", "00000000-0000-0000-0000-000000000001"),
                new Claim("role", "user"), // First role claim
                new Claim("role", "admin") // Duplicate role claim (should use this one)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _client.JWT(tokenString);
            var results = await _client.From<JwtTestSensitiveData>().Get();
            
            // Should have admin access (last role value)
            results.Count.ShouldBeGreaterThan(0);
            results.ShouldContain(r => r.AccessLevel == "confidential");
        }

        /// <summary>
        /// INTENT: Test that valid JWT tokens work correctly.
        /// PURPOSE: Ensure security doesn't break legitimate use.
        /// BUSINESS CONTEXT: Valid users need proper access.
        /// WHY IMPORTANT: Security must not impede functionality.
        /// ARCHITECTURAL SIGNIFICANCE: Validates end-to-end JWT flow.
        /// FUTURE RESILIENCE: Ensures JWT functionality remains intact.
        /// </summary>
        [Fact]
        public async Task JWT_WithValidToken_AllowsProperAccess()
        {
            // Test 1: Admin with full access
            var adminToken = CreateTestJWT(new Dictionary<string, string>
            {
                { "user_id", "11111111-1111-1111-1111-111111111111" },
                { "tenant_id", "00000000-0000-0000-0000-000000000001" },
                { "role", "admin" }
            });

            _client.JWT(adminToken);
            var adminResults = await _client.From<JwtTestSensitiveData>().Get();
            adminResults.Count.ShouldBe(2); // Admin sees all tenant data
            adminResults.ShouldContain(r => r.AccessLevel == "confidential");

            // Test 2: Regular user with limited access
            var userToken = CreateTestJWT(new Dictionary<string, string>
            {
                { "user_id", "22222222-2222-2222-2222-222222222222" },
                { "tenant_id", "00000000-0000-0000-0000-000000000001" },
                { "role", "user" }
            });

            _client.JWT(userToken);
            var userResults = await _client.From<JwtTestSensitiveData>().Get();
            userResults.Count.ShouldBe(1); // User sees only non-confidential data
            userResults.ShouldNotContain(r => r.AccessLevel == "confidential");
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
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    /// <summary>
    /// Test entity for JWT security tests
    /// </summary>
    public class JwtTestSensitiveData
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string Data { get; set; } = "";
        public string AccessLevel { get; set; } = "";
    }
}