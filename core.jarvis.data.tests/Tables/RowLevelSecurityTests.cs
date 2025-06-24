using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Shouldly;
using Xunit;

namespace core.jarvis.data.tests.Tables
{
    /// <summary>
    /// INTENT: Validate Row Level Security (RLS) implementation for multi-tenant data isolation.
    /// PURPOSE: Ensure JWT claims are properly propagated to PostgreSQL for RLS policy enforcement.
    /// BUSINESS CONTEXT: Enables secure multi-tenant SaaS applications with data isolation.
    /// WHY IMPORTANT: Prevents data leaks between tenants and enforces access control at the database level.
    /// ARCHITECTURAL SIGNIFICANCE: Database-level security is the last line of defense.
    /// FUTURE RESILIENCE: Protects against application-level security bypasses.
    /// </summary>
    [Collection("JWT Tests")]
    public class RowLevelSecurityTests : IAsyncLifetime
    {
        private NpgsqlConnection _conn = null!;
        private PgClient _adminClient = null!;
        private readonly string _jwtSecret = "test-secret-key-must-be-at-least-256-bits-long!!!";

        public async Task InitializeAsync()
        {
            // Set up JWT environment variables for testing
            Environment.SetEnvironmentVariable("Jwt__SecretKey", _jwtSecret);
            Environment.SetEnvironmentVariable("Jwt__Issuer", "jarvis-api-test");
            Environment.SetEnvironmentVariable("Jwt__Audience", "jarvis-clients-test");
            
            var connString = TestHelpers.GetConnectionStringFromEnv();
            _conn = new NpgsqlConnection(connString);
            _adminClient = await PgClientFactory.Create(_conn);

            // Create test tables with RLS
            await SetupRLSTables();
        }

        private async Task SetupRLSTables()
        {
            // Drop existing objects
            var dropSql = @"
                DROP POLICY IF EXISTS tenant_isolation ON tenant_data;
                DROP POLICY IF EXISTS user_own_records ON user_data;
                DROP POLICY IF EXISTS role_based_access ON sensitive_data;
                DROP TABLE IF EXISTS tenant_data;
                DROP TABLE IF EXISTS user_data;
                DROP TABLE IF EXISTS sensitive_data;
                DROP FUNCTION IF EXISTS current_user_id();
                DROP FUNCTION IF EXISTS current_tenant_id();
                DROP FUNCTION IF EXISTS current_user_role();
                DROP FUNCTION IF EXISTS current_jwt_claim(text);
            ";
            await _conn.ExecuteAsync(dropSql);

            // Create helper functions to extract JWT claims
            var functionsSql = @"
                -- Generic function to get any JWT claim
                CREATE OR REPLACE FUNCTION current_jwt_claim(claim TEXT) 
                RETURNS TEXT AS $$
                BEGIN
                    RETURN current_setting('jwt.claims.' || claim, true);
                END;
                $$ LANGUAGE plpgsql STABLE;

                -- Convenience functions for common claims
                CREATE OR REPLACE FUNCTION current_user_id() 
                RETURNS UUID AS $$
                BEGIN
                    RETURN current_setting('jwt.claims.sub', true)::UUID;
                EXCEPTION
                    WHEN OTHERS THEN RETURN NULL;
                END;
                $$ LANGUAGE plpgsql STABLE;

                CREATE OR REPLACE FUNCTION current_tenant_id() 
                RETURNS UUID AS $$
                BEGIN
                    RETURN current_setting('jwt.claims.tenant_id', true)::UUID;
                EXCEPTION
                    WHEN OTHERS THEN RETURN NULL;
                END;
                $$ LANGUAGE plpgsql STABLE;

                CREATE OR REPLACE FUNCTION current_user_role() 
                RETURNS TEXT AS $$
                BEGIN
                    RETURN current_setting('jwt.claims.role', true);
                END;
                $$ LANGUAGE plpgsql STABLE;
            ";
            await _conn.ExecuteAsync(functionsSql);

            // Create tenant_data table with multi-tenant RLS
            var tenantTableSql = @"
                CREATE TABLE tenant_data (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    tenant_id UUID NOT NULL,
                    name TEXT NOT NULL,
                    data JSONB,
                    created_at TIMESTAMP DEFAULT NOW()
                );

                ALTER TABLE tenant_data ENABLE ROW LEVEL SECURITY;

                -- Policy: Users can only see data from their own tenant
                CREATE POLICY tenant_isolation ON tenant_data
                    FOR ALL
                    USING (tenant_id = current_tenant_id())
                    WITH CHECK (tenant_id = current_tenant_id());
            ";
            await _conn.ExecuteAsync(tenantTableSql);

            // Create user_data table with user-level RLS
            var userTableSql = @"
                CREATE TABLE user_data (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    user_id UUID NOT NULL,
                    tenant_id UUID NOT NULL,
                    title TEXT NOT NULL,
                    content TEXT,
                    is_public BOOLEAN DEFAULT FALSE,
                    created_at TIMESTAMP DEFAULT NOW()
                );

                ALTER TABLE user_data ENABLE ROW LEVEL SECURITY;

                -- Policy: Users can see their own records OR public records from their tenant
                CREATE POLICY user_own_records ON user_data
                    FOR SELECT
                    USING (
                        tenant_id = current_tenant_id() AND 
                        (user_id = current_user_id() OR is_public = TRUE)
                    );

                -- Policy: Users can only insert/update/delete their own records
                CREATE POLICY user_modify_own ON user_data
                    FOR INSERT
                    WITH CHECK (user_id = current_user_id() AND tenant_id = current_tenant_id());

                CREATE POLICY user_update_own ON user_data
                    FOR UPDATE
                    USING (user_id = current_user_id() AND tenant_id = current_tenant_id())
                    WITH CHECK (user_id = current_user_id() AND tenant_id = current_tenant_id());

                CREATE POLICY user_delete_own ON user_data
                    FOR DELETE
                    USING (user_id = current_user_id() AND tenant_id = current_tenant_id());
            ";
            await _conn.ExecuteAsync(userTableSql);

            // Create sensitive_data table with role-based RLS
            var sensitiveTableSql = @"
                CREATE TABLE sensitive_data (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    tenant_id UUID NOT NULL,
                    classification TEXT NOT NULL CHECK (classification IN ('public', 'internal', 'confidential', 'secret')),
                    data TEXT NOT NULL,
                    created_by UUID NOT NULL,
                    created_at TIMESTAMP DEFAULT NOW()
                );

                ALTER TABLE sensitive_data ENABLE ROW LEVEL SECURITY;

                -- Policy: Access based on role and classification
                CREATE POLICY role_based_access ON sensitive_data
                    FOR SELECT
                    USING (
                        tenant_id = current_tenant_id() AND (
                            (classification = 'public') OR
                            (classification = 'internal' AND current_user_role() IN ('user', 'manager', 'admin')) OR
                            (classification = 'confidential' AND current_user_role() IN ('manager', 'admin')) OR
                            (classification = 'secret' AND current_user_role() = 'admin')
                        )
                    );

                -- Only managers and admins can insert sensitive data
                CREATE POLICY role_based_insert ON sensitive_data
                    FOR INSERT
                    WITH CHECK (
                        tenant_id = current_tenant_id() AND 
                        current_user_role() IN ('manager', 'admin') AND
                        created_by = current_user_id()
                    );
            ";
            await _conn.ExecuteAsync(sensitiveTableSql);

            // Insert test data as superuser (bypasses RLS)
            await InsertTestData();
        }

        private async Task InsertTestData()
        {
            // Insert data for multiple tenants
            var tenantDataSql = @"
                INSERT INTO tenant_data (id, tenant_id, name, data) VALUES
                    ('11111111-1111-1111-1111-111111111111', '00000000-0000-0000-0000-000000000001', 'Tenant 1 Resource', '{}'),
                    ('22222222-2222-2222-2222-222222222222', '00000000-0000-0000-0000-000000000001', 'Another Tenant 1 Resource', '{}'),
                    ('33333333-3333-3333-3333-333333333333', '00000000-0000-0000-0000-000000000002', 'Tenant 2 Resource', '{}'),
                    ('44444444-4444-4444-4444-444444444444', '00000000-0000-0000-0000-000000000002', 'Another Tenant 2 Resource', '{}');
            ";
            await _conn.ExecuteAsync(tenantDataSql);

            // Insert user data - use parameterized query to avoid parsing issues
            var userData = new[]
            {
                new { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"), TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Title = "User 1 Private", Content = "Private content", IsPublic = false },
                new { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"), TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Title = "User 1 Public", Content = "Public content", IsPublic = true },
                new { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"), TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Title = "User 2 Private", Content = "Private content", IsPublic = false },
                new { Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"), TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Title = "User 2 Public", Content = "Public content", IsPublic = true },
                new { Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"), TenantId = Guid.Parse("00000000-0000-0000-0000-000000000002"), Title = "User 3 Private", Content = "Private content", IsPublic = false }
            };
            
            var userDataSql = "INSERT INTO user_data (id, user_id, tenant_id, title, content, is_public) VALUES (@Id, @UserId, @TenantId, @Title, @Content, @IsPublic)";
            foreach (var data in userData)
            {
                await _conn.ExecuteAsync(userDataSql, data);
            }

            // Insert sensitive data - use parameterized query
            var sensitiveData = new[]
            {
                new { Id = Guid.Parse("f1111111-1111-1111-1111-111111111111"), TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Classification = "public", Data = "Public information", CreatedBy = Guid.Parse("11111111-1111-1111-1111-111111111111") },
                new { Id = Guid.Parse("f2222222-2222-2222-2222-222222222222"), TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Classification = "internal", Data = "Internal memo", CreatedBy = Guid.Parse("11111111-1111-1111-1111-111111111111") },
                new { Id = Guid.Parse("f3333333-3333-3333-3333-333333333333"), TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Classification = "confidential", Data = "Confidential report", CreatedBy = Guid.Parse("11111111-1111-1111-1111-111111111111") },
                new { Id = Guid.Parse("f4444444-4444-4444-4444-444444444444"), TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Classification = "secret", Data = "Secret plans", CreatedBy = Guid.Parse("11111111-1111-1111-1111-111111111111") },
                new { Id = Guid.Parse("f5555555-5555-5555-5555-555555555555"), TenantId = Guid.Parse("00000000-0000-0000-0000-000000000002"), Classification = "public", Data = "Tenant 2 public", CreatedBy = Guid.Parse("33333333-3333-3333-3333-333333333333") }
            };
            
            var sensitiveDataSql = "INSERT INTO sensitive_data (id, tenant_id, classification, data, created_by) VALUES (@Id, @TenantId, @Classification, @Data, @CreatedBy)";
            foreach (var data in sensitiveData)
            {
                await _conn.ExecuteAsync(sensitiveDataSql, data);
            }
        }

        public async Task DisposeAsync()
        {
            // Note: Environment variables are restored by JwtTestFixture
            
            // Cleanup
            var dropSql = @"
                DROP POLICY IF EXISTS tenant_isolation ON tenant_data;
                DROP POLICY IF EXISTS user_own_records ON user_data;
                DROP POLICY IF EXISTS user_modify_own ON user_data;
                DROP POLICY IF EXISTS user_update_own ON user_data;
                DROP POLICY IF EXISTS user_delete_own ON user_data;
                DROP POLICY IF EXISTS role_based_access ON sensitive_data;
                DROP POLICY IF EXISTS role_based_insert ON sensitive_data;
                DROP TABLE IF EXISTS tenant_data;
                DROP TABLE IF EXISTS user_data;
                DROP TABLE IF EXISTS sensitive_data;
                DROP FUNCTION IF EXISTS current_user_id();
                DROP FUNCTION IF EXISTS current_tenant_id();
                DROP FUNCTION IF EXISTS current_user_role();
                DROP FUNCTION IF EXISTS current_jwt_claim(text);
            ";
            await _conn.ExecuteAsync(dropSql);
            await _conn.CloseAsync();
        }

        private string GenerateJWT(Guid userId, Guid tenantId, string role, Dictionary<string, string>? additionalClaims = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("role", role)
            };

            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "jarvis-api-test",
                Audience = "jarvis-clients-test",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// INTENT: Test that tenant isolation works - users can only see their own tenant's data.
        /// PURPOSE: Verify core multi-tenant RLS functionality.
        /// </summary>
        [Fact]
        public async Task TenantIsolation_PreventsCrossTenantDataAccess()
        {
            // Arrange
            var tenant1UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var tenant1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var tenant2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

            var tenant1Jwt = GenerateJWT(tenant1UserId, tenant1Id, "user");
            var tenant2Jwt = GenerateJWT(Guid.NewGuid(), tenant2Id, "user");

            var tenant1Client = new PgClient(_conn);
            tenant1Client.JWT(tenant1Jwt);

            var tenant2Client = new PgClient(_conn);
            tenant2Client.JWT(tenant2Jwt);

            // Act
            var tenant1Data = await tenant1Client.From<TenantData>().Get();
            var tenant2Data = await tenant2Client.From<TenantData>().Get();

            // Assert
            tenant1Data.Count.ShouldBe(2);
            tenant1Data.ShouldAllBe(d => d.TenantId == tenant1Id);

            tenant2Data.Count.ShouldBe(2);
            tenant2Data.ShouldAllBe(d => d.TenantId == tenant2Id);
        }

        /// <summary>
        /// INTENT: Test that users can only see their own private data and public data from their tenant.
        /// PURPOSE: Verify user-level RLS with mixed public/private access.
        /// </summary>
        [Fact]
        public async Task UserLevelSecurity_RestrictsPrivateDataAccess()
        {
            // Arrange
            var user1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var user2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var user1Jwt = GenerateJWT(user1Id, tenantId, "user");
            var user2Jwt = GenerateJWT(user2Id, tenantId, "user");

            var user1Client = new PgClient(_conn);
            user1Client.JWT(user1Jwt);

            var user2Client = new PgClient(_conn);
            user2Client.JWT(user2Jwt);

            // Act
            var user1Data = await user1Client.From<UserData>().Get();
            var user2Data = await user2Client.From<UserData>().Get();

            // Assert
            // User 1 should see: their own 2 records + User 2's 1 public record = 3 total
            user1Data.Count.ShouldBe(3);
            user1Data.Count(d => d.UserId == user1Id).ShouldBe(2); // All of user 1's records
            user1Data.Count(d => d.UserId == user2Id && d.IsPublic).ShouldBe(1); // Only public from user 2

            // User 2 should see: their own 2 records + User 1's 1 public record = 3 total
            user2Data.Count.ShouldBe(3);
            user2Data.Count(d => d.UserId == user2Id).ShouldBe(2); // All of user 2's records
            user2Data.Count(d => d.UserId == user1Id && d.IsPublic).ShouldBe(1); // Only public from user 1
        }

        /// <summary>
        /// INTENT: Test role-based access control for sensitive data classifications.
        /// PURPOSE: Verify that data classification levels are enforced based on user roles.
        /// </summary>
        [Fact]
        public async Task RoleBasedAccess_EnforcesDataClassification()
        {
            // Arrange
            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var userJwt = GenerateJWT(userId, tenantId, "user");
            var managerJwt = GenerateJWT(userId, tenantId, "manager");
            var adminJwt = GenerateJWT(userId, tenantId, "admin");

            var userClient = new PgClient(_conn);
            userClient.JWT(userJwt);

            var managerClient = new PgClient(_conn);
            managerClient.JWT(managerJwt);

            var adminClient = new PgClient(_conn);
            adminClient.JWT(adminJwt);

            // Act
            var userData = await userClient.From<SensitiveData>().Get();
            var managerData = await managerClient.From<SensitiveData>().Get();
            var adminData = await adminClient.From<SensitiveData>().Get();

            // Assert
            // Regular user sees: public + internal = 2
            userData.Count.ShouldBe(2);
            userData.ShouldAllBe(d => d.Classification == "public" || d.Classification == "internal");

            // Manager sees: public + internal + confidential = 3
            managerData.Count.ShouldBe(3);
            managerData.ShouldAllBe(d => d.Classification != "secret");

            // Admin sees: all 4 records
            adminData.Count.ShouldBe(4);
        }

        /// <summary>
        /// INTENT: Test that users can only modify their own records.
        /// PURPOSE: Verify write operations respect RLS policies.
        /// </summary>
        [Fact]
        public async Task UserCanOnlyModifyOwnRecords()
        {
            // Arrange
            var user1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var user2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var user1Jwt = GenerateJWT(user1Id, tenantId, "user");
            var user1Client = new PgClient(_conn);
            user1Client.JWT(user1Jwt);

            // Act & Assert - User can insert their own record
            var newRecord = new UserData
            {
                UserId = user1Id,
                TenantId = tenantId,
                Title = "New User 1 Record",
                Content = "Test content",
                IsPublic = false
            };

            await user1Client.From<UserData>().Insert(newRecord);

            // Verify it was inserted
            var results = await user1Client.From<UserData>()
                .Filter("title", "eq", "New User 1 Record")
                .Get();
            results.Count.ShouldBe(1);

            // Act & Assert - User cannot insert record for another user (should fail silently due to RLS)
            var wrongUserRecord = new UserData
            {
                UserId = user2Id, // Different user!
                TenantId = tenantId,
                Title = "Attempted Cross-User Insert",
                Content = "Should not work",
                IsPublic = false
            };

            await user1Client.From<UserData>().Insert(wrongUserRecord);

            // Verify it was NOT inserted
            var wrongResults = await user1Client.From<UserData>()
                .Filter("title", "eq", "Attempted Cross-User Insert")
                .Get();
            wrongResults.Count.ShouldBe(0);
        }

        /// <summary>
        /// INTENT: Test that JWT claims are properly extracted and used in SQL functions.
        /// PURPOSE: Verify the JWT to PostgreSQL session variable bridge works correctly.
        /// </summary>
        [Fact]
        public async Task JWTClaimsArePropagatedToPostgreSQL()
        {
            // Arrange
            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var customClaims = new Dictionary<string, string>
            {
                { "department", "engineering" },
                { "clearance_level", "top_secret" }
            };

            var jwt = GenerateJWT(userId, tenantId, "admin", customClaims);
            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act - Query the current JWT claims using PostgreSQL functions
            var claimsSql = @"
                SELECT 
                    current_user_id() as user_id,
                    current_tenant_id() as tenant_id,
                    current_user_role() as role,
                    current_jwt_claim('department') as department,
                    current_jwt_claim('clearance_level') as clearance_level
            ";

            // We need to set the JWT first by making any query
            await client.From<TenantData>().Get();

            // Now query the claims
            var claims = await _conn.QuerySingleAsync<JWTClaimsResult>(claimsSql);

            // Assert
            claims.UserId.ShouldBe(userId);
            claims.TenantId.ShouldBe(tenantId);
            claims.Role.ShouldBe("admin");
            claims.Department.ShouldBe("engineering");
            claims.ClearanceLevel.ShouldBe("top_secret");
        }

        /// <summary>
        /// INTENT: Test that without a JWT, no data is accessible.
        /// PURPOSE: Verify secure-by-default behavior.
        /// </summary>
        [Fact]
        public async Task NoJWT_NoDataAccess()
        {
            // Arrange
            var noAuthClient = new PgClient(_conn);
            // Not setting any JWT

            // Act
            var tenantData = await noAuthClient.From<TenantData>().Get();
            var userData = await noAuthClient.From<UserData>().Get();
            var sensitiveData = await noAuthClient.From<SensitiveData>().Get();

            // Assert
            tenantData.ShouldBeEmpty();
            userData.ShouldBeEmpty();
            sensitiveData.ShouldBeEmpty();
        }

        /// <summary>
        /// INTENT: Test complex RLS scenario with filtering.
        /// PURPOSE: Ensure RLS works correctly with additional WHERE clauses.
        /// </summary>
        [Fact]
        public async Task RLSWorksWithAdditionalFilters()
        {
            // Arrange
            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var jwt = GenerateJWT(userId, tenantId, "user");

            var client = new PgClient(_conn);
            client.JWT(jwt);

            // Act - Filter for public records only
            var publicOnly = await client.From<UserData>()
                .Filter("is_public", "eq", true)
                .Get();

            // Assert
            // Should see 2 public records (1 from user1, 1 from user2)
            publicOnly.Count.ShouldBe(2);
            publicOnly.ShouldAllBe(d => d.IsPublic == true);
            publicOnly.ShouldAllBe(d => d.TenantId == tenantId);
        }
    }

    // Test entities
    public record TenantData
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = "";
        public string? Data { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record UserData
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string Title { get; set; } = "";
        public string? Content { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record SensitiveData
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Classification { get; set; } = "";
        public string Data { get; set; } = "";
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record JWTClaimsResult
    {
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public string? Role { get; set; }
        public string? Department { get; set; }
        public string? ClearanceLevel { get; set; }
    }
}