using core.jarvis.data.RLS;
using Shouldly;

namespace core.jarvis.data.tests.RLS
{
    /// <summary>
    /// INTENT: Verify RLS WhereClause delegates are protected against SQL injection via claim values.
    /// PURPOSE: Security validation for CRIT-01 vulnerability fix (jarvis-39).
    /// BUSINESS CONTEXT: Attackers could inject SQL via malicious JWT claim values.
    /// WHY IMPORTANT: SQL injection in RLS policies could bypass tenant isolation entirely.
    /// ARCHITECTURAL SIGNIFICANCE: RLS is a critical security layer for multi-tenant isolation.
    /// FUTURE RESILIENCE: Ensures GUID validation in WhereClause delegates prevents injection.
    /// </summary>
    public class RLSWhereClauseInjectionTests
    {
        #region SQL Injection Prevention Tests

        /// <summary>
        /// INTENT: Verify SQL injection payloads in tenant_id claim are rejected.
        /// PURPOSE: Ensure malicious tenant_id values cannot execute SQL.
        /// </summary>
        [Theory]
        [InlineData("'; DROP TABLE tenant_data; --")]
        [InlineData("' OR '1'='1")]
        [InlineData("'; DELETE FROM tenant_data; --")]
        [InlineData("' UNION SELECT * FROM pg_user --")]
        [InlineData("'; UPDATE tenant_data SET tenant_id = null; --")]
        [InlineData("00000000-0000-0000-0000-000000000000' OR '1'='1")]
        [InlineData("$(sleep 10)")]
        [InlineData("<script>alert('xss')</script>")]
        [InlineData("1; EXEC xp_cmdshell('net user hacker password /add'); --")]
        public void TenantDataPolicy_WithSqlInjectionInTenantId_ReturnsDenyClause(string maliciousTenantId)
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", maliciousTenantId }
            };

            // Act
            var whereClause = registry.BuildWhereClause("tenant_data", claims);

            // Assert - Should return deny clause "1=0" since malicious value fails GUID validation
            whereClause.ShouldBe("(1=0)");
        }

        /// <summary>
        /// INTENT: Verify SQL injection payloads in user_id (sub) claim are rejected.
        /// PURPOSE: Ensure malicious user_id values cannot execute SQL.
        /// </summary>
        [Theory]
        [InlineData("'; DROP TABLE user_data; --")]
        [InlineData("' OR '1'='1")]
        [InlineData("' UNION SELECT password FROM users --")]
        [InlineData("admin'--")]
        [InlineData("1' OR EXISTS(SELECT * FROM pg_tables) --")]
        public void UserDataPolicy_WithSqlInjectionInUserId_ReturnsDenyClause(string maliciousUserId)
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var validTenantId = Guid.NewGuid().ToString();
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", validTenantId },
                { "sub", maliciousUserId }
            };

            // Act
            var whereClause = registry.BuildWhereClause("user_data", claims);

            // Assert - Should return deny clause since malicious user_id fails GUID validation
            whereClause.ShouldBe("(1=0)");
        }

        /// <summary>
        /// INTENT: Verify SQL injection via tenant_id in sensitive_data policy is rejected.
        /// </summary>
        [Theory]
        [InlineData("'; DROP TABLE sensitive_data; --")]
        [InlineData("' OR classification = 'secret' --")]
        public void SensitiveDataPolicy_WithSqlInjectionInTenantId_ReturnsDenyClause(string maliciousTenantId)
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", maliciousTenantId },
                { "role", "admin" }
            };

            // Act
            var whereClause = registry.BuildWhereClause("sensitive_data", claims);

            // Assert
            whereClause.ShouldBe("(1=0)");
        }

        #endregion

        #region Valid GUID Acceptance Tests

        /// <summary>
        /// INTENT: Verify valid GUIDs are accepted and produce correct SQL.
        /// </summary>
        [Fact]
        public void TenantDataPolicy_WithValidGuid_ProducesCorrectWhereClause()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var validTenantId = Guid.NewGuid();
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", validTenantId.ToString() }
            };

            // Act
            var whereClause = registry.BuildWhereClause("tenant_data", claims);

            // Assert - Should produce valid WHERE clause with the GUID
            whereClause.ShouldBe($"(tenant_id = '{validTenantId}'::uuid)");
        }

        /// <summary>
        /// INTENT: Verify user_data policy accepts valid GUIDs for both tenant and user.
        /// </summary>
        [Fact]
        public void UserDataPolicy_WithValidGuids_ProducesCorrectWhereClause()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var validTenantId = Guid.NewGuid();
            var validUserId = Guid.NewGuid();
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", validTenantId.ToString() },
                { "sub", validUserId.ToString() }
            };

            // Act
            var whereClause = registry.BuildWhereClause("user_data", claims);

            // Assert
            whereClause.ShouldContain($"tenant_id = '{validTenantId}'::uuid");
            whereClause.ShouldContain($"user_id = '{validUserId}'::uuid");
            whereClause.ShouldContain("is_public = TRUE");
        }

        /// <summary>
        /// INTENT: Verify sensitive_data policy accepts valid tenant GUID with role.
        /// </summary>
        [Theory]
        [InlineData("admin")]
        [InlineData("manager")]
        [InlineData("user")]
        public void SensitiveDataPolicy_WithValidGuidAndRole_ProducesCorrectWhereClause(string role)
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var validTenantId = Guid.NewGuid();
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", validTenantId.ToString() },
                { "role", role }
            };

            // Act
            var whereClause = registry.BuildWhereClause("sensitive_data", claims);

            // Assert - Should contain the validated tenant_id GUID
            whereClause.ShouldContain($"tenant_id = '{validTenantId}'::uuid");
            whereClause.ShouldNotContain("1=0");
        }

        #endregion

        #region Malformed Claims Tests

        /// <summary>
        /// INTENT: Verify missing tenant_id claim returns deny clause.
        /// </summary>
        [Fact]
        public void TenantDataPolicy_WithMissingTenantId_ReturnsDenyClause()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var claims = new Dictionary<string, string>(); // No tenant_id

            // Act
            var whereClause = registry.BuildWhereClause("tenant_data", claims);

            // Assert
            whereClause.ShouldBe("(1=0)");
        }

        /// <summary>
        /// INTENT: Verify empty tenant_id claim returns deny clause.
        /// </summary>
        [Fact]
        public void TenantDataPolicy_WithEmptyTenantId_ReturnsDenyClause()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", "" }
            };

            // Act
            var whereClause = registry.BuildWhereClause("tenant_data", claims);

            // Assert
            whereClause.ShouldBe("(1=0)");
        }

        /// <summary>
        /// INTENT: Verify whitespace-only tenant_id returns deny clause.
        /// </summary>
        [Fact]
        public void TenantDataPolicy_WithWhitespaceTenantId_ReturnsDenyClause()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", "   " }
            };

            // Act
            var whereClause = registry.BuildWhereClause("tenant_data", claims);

            // Assert
            whereClause.ShouldBe("(1=0)");
        }

        /// <summary>
        /// INTENT: Verify valid tenant_id but missing user_id returns deny clause for user_data.
        /// </summary>
        [Fact]
        public void UserDataPolicy_WithValidTenantButMissingUser_ReturnsDenyClause()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var validTenantId = Guid.NewGuid().ToString();
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", validTenantId }
                // Missing "sub" claim
            };

            // Act
            var whereClause = registry.BuildWhereClause("user_data", claims);

            // Assert
            whereClause.ShouldBe("(1=0)");
        }

        #endregion

        #region GUID Format Validation Tests

        /// <summary>
        /// INTENT: Verify various invalid GUID formats are rejected.
        /// </summary>
        [Theory]
        [InlineData("not-a-guid")]
        [InlineData("12345")]
        [InlineData("00000000-0000-0000-0000-00000000000Z")] // Invalid char
        [InlineData("00000000-0000-0000-0000-0000000000000")] // Too long
        [InlineData("00000000-0000-0000-0000-00000000000")] // Too short
        [InlineData("{00000000-0000-0000-0000-000000000000")] // Missing closing brace
        [InlineData("null")]
        [InlineData("undefined")]
        public void TenantDataPolicy_WithInvalidGuidFormat_ReturnsDenyClause(string invalidGuid)
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", invalidGuid }
            };

            // Act
            var whereClause = registry.BuildWhereClause("tenant_data", claims);

            // Assert
            whereClause.ShouldBe("(1=0)");
        }

        /// <summary>
        /// INTENT: Verify standard GUID formats are accepted.
        /// </summary>
        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000")] // Empty GUID
        [InlineData("a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11")] // Standard format
        [InlineData("A0EEBC99-9C0B-4EF8-BB6D-6BB9BD380A11")] // Uppercase
        public void TenantDataPolicy_WithValidGuidFormats_ProducesValidWhereClause(string validGuid)
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            DefaultRLSPolicies.RegisterDefaultPolicies(registry);
            var claims = new Dictionary<string, string>
            {
                { "tenant_id", validGuid }
            };

            // Act
            var whereClause = registry.BuildWhereClause("tenant_data", claims);

            // Assert - Should produce valid WHERE clause (not deny)
            whereClause.ShouldNotBe("(1=0)");
            whereClause.ShouldContain("tenant_id =");
            whereClause.ShouldContain("::uuid");
        }

        #endregion
    }
}
