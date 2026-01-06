using core.jarvis.data.RLS;
using Npgsql;
using Shouldly;
using Xunit;

namespace core.jarvis.data.tests.RLS
{
    /// <summary>
    /// Unit tests for RLSMigrationHelper.
    /// Tests SQL generation logic without requiring a real database connection.
    /// </summary>
    public class RLSMigrationHelperTests
    {
        /// <summary>
        /// Creates a test PgClient with a connection that will never be used.
        /// The connection string points to a non-existent server, but since we're
        /// only testing SQL generation (no actual execution), this is fine.
        /// </summary>
        private static PgClient CreateTestPgClient(RLSPolicyRegistry registry)
        {
            // Connection will never be opened - we only test SQL generation
            var conn = new NpgsqlConnection("Host=localhost;Database=nonexistent;Username=test;Password=test");
            return new PgClient(conn, registry);
        }

        #region GenerateMigrationScript Tests

        [Fact]
        public void GenerateMigrationScript_WithRegisteredPolicies_GeneratesValidSql()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "test_table",
                Type = PolicyType.All,
                PolicyName = "test_policy",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid",
                WithCheckExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldContain("ALTER TABLE \"test_table\" ENABLE ROW LEVEL SECURITY;");
            script.ShouldContain("ALTER TABLE \"test_table\" FORCE ROW LEVEL SECURITY;");
            script.ShouldContain("DROP POLICY IF EXISTS \"test_policy\" ON \"test_table\";");
            script.ShouldContain("CREATE POLICY \"test_policy\" ON \"test_table\"");
            script.ShouldContain("FOR ALL");
            script.ShouldContain("USING (tenant_id = current_setting('app.tenant_id', true)::uuid)");
            script.ShouldContain("WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid)");
        }

        [Fact]
        public void GenerateMigrationScript_WithSelectPolicy_GeneratesCorrectForClause()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "read_only_table",
                Type = PolicyType.Select,
                PolicyName = "select_policy",
                UsingExpression = "visible = true"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldContain("FOR SELECT");
            script.ShouldContain("USING (visible = true)");
            script.ShouldNotContain("WITH CHECK"); // SELECT policies don't have WITH CHECK
        }

        [Fact]
        public void GenerateMigrationScript_WithInsertPolicy_GeneratesWithCheckOnly()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "insert_table",
                Type = PolicyType.Insert,
                PolicyName = "insert_policy",
                UsingExpression = "true", // INSERT doesn't use USING
                WithCheckExpression = "owner_id = current_user_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldContain("FOR INSERT");
            script.ShouldContain("WITH CHECK (owner_id = current_user_id())");
            // INSERT policies should NOT have USING clause in the generated SQL
            script.ShouldNotContain("USING (true)");
        }

        [Fact]
        public void GenerateMigrationScript_WithUpdatePolicy_GeneratesBothClauses()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "update_table",
                Type = PolicyType.Update,
                PolicyName = "update_policy",
                UsingExpression = "owner_id = current_user_id()",
                WithCheckExpression = "owner_id = current_user_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldContain("FOR UPDATE");
            script.ShouldContain("USING (owner_id = current_user_id())");
            script.ShouldContain("WITH CHECK (owner_id = current_user_id())");
        }

        [Fact]
        public void GenerateMigrationScript_WithDeletePolicy_GeneratesUsingOnly()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "delete_table",
                Type = PolicyType.Delete,
                PolicyName = "delete_policy",
                UsingExpression = "owner_id = current_user_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldContain("FOR DELETE");
            script.ShouldContain("USING (owner_id = current_user_id())");
            script.ShouldNotContain("WITH CHECK"); // DELETE policies don't have WITH CHECK
        }

        [Fact]
        public void GenerateMigrationScript_WithNoPolicies_ReturnsNoPoliciesMessage()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldContain("-- No PostgreSQL-native RLS policies found.");
        }

        [Fact]
        public void GenerateMigrationScript_WithPolicyWithoutPostgresExpressions_SkipsPolicy()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            // Policy without PolicyName or UsingExpression (SDK-only policy)
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "sdk_only_table",
                Type = PolicyType.Select,
                WhereClause = claims => "tenant_id = 'some-id'"
                // No PolicyName or UsingExpression - not a Postgres policy
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldNotContain("sdk_only_table");
            script.ShouldContain("-- No PostgreSQL-native RLS policies found.");
        }

        #endregion

        #region Wildcard Pattern Skipping Tests

        [Fact]
        public void GenerateMigrationScript_SkipsWildcardPatterns_WithAsteriskPrefix()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            // Wildcard pattern - should be skipped
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "*_component",
                Type = PolicyType.All,
                PolicyName = "component_policy",
                UsingExpression = "owner_id = current_user_id()",
                WithCheckExpression = "owner_id = current_user_id()"
            });

            // Regular table - should be included
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "regular_table",
                Type = PolicyType.All,
                PolicyName = "regular_policy",
                UsingExpression = "tenant_id = current_tenant_id()",
                WithCheckExpression = "tenant_id = current_tenant_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldNotContain("*_component");
            script.ShouldNotContain("component_policy");
            script.ShouldContain("regular_table");
            script.ShouldContain("regular_policy");
        }

        [Fact]
        public void GenerateMigrationScript_SkipsWildcardPatterns_WithAsteriskSuffix()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            // Wildcard pattern with suffix asterisk
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "tenant_*",
                Type = PolicyType.All,
                PolicyName = "tenant_wildcard_policy",
                UsingExpression = "tenant_id = current_tenant_id()",
                WithCheckExpression = "tenant_id = current_tenant_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldNotContain("tenant_*");
            script.ShouldNotContain("tenant_wildcard_policy");
            script.ShouldContain("-- No PostgreSQL-native RLS policies found.");
        }

        [Fact]
        public void GenerateMigrationScript_SkipsWildcardPatterns_WithAsteriskInMiddle()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            // Wildcard pattern with asterisk in the middle
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "prefix_*_suffix",
                Type = PolicyType.All,
                PolicyName = "middle_wildcard_policy",
                UsingExpression = "owner_id = current_user_id()",
                WithCheckExpression = "owner_id = current_user_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldNotContain("prefix_*_suffix");
            script.ShouldNotContain("middle_wildcard_policy");
        }

        #endregion

        #region Policy Grouping Tests

        [Fact]
        public void GenerateMigrationScript_GroupsPoliciesByTable()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            // Multiple policies for same table
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "shared_table",
                Type = PolicyType.Select,
                PolicyName = "select_policy",
                UsingExpression = "visible = true"
            });

            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "shared_table",
                Type = PolicyType.Insert,
                PolicyName = "insert_policy",
                UsingExpression = "true",
                WithCheckExpression = "owner_id = current_user_id()"
            });

            // Policy for different table
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "other_table",
                Type = PolicyType.All,
                PolicyName = "other_policy",
                UsingExpression = "tenant_id = current_tenant_id()",
                WithCheckExpression = "tenant_id = current_tenant_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            // Verify grouping - ENABLE and FORCE RLS should appear only once per table
            var sharedTableEnableCount = CountOccurrences(script, "ALTER TABLE \"shared_table\" ENABLE ROW LEVEL SECURITY;");
            var sharedTableForceCount = CountOccurrences(script, "ALTER TABLE \"shared_table\" FORCE ROW LEVEL SECURITY;");
            var otherTableEnableCount = CountOccurrences(script, "ALTER TABLE \"other_table\" ENABLE ROW LEVEL SECURITY;");

            sharedTableEnableCount.ShouldBe(1);
            sharedTableForceCount.ShouldBe(1);
            otherTableEnableCount.ShouldBe(1);

            // Both policies for shared_table should be present
            script.ShouldContain("select_policy");
            script.ShouldContain("insert_policy");
            script.ShouldContain("other_policy");
        }

        [Fact]
        public void GenerateMigrationScript_IncludesTableHeaderComments()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "commented_table",
                Type = PolicyType.All,
                PolicyName = "test_policy",
                UsingExpression = "true",
                WithCheckExpression = "true"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldContain("-- Policies for table: commented_table");
        }

        [Fact]
        public void GenerateMigrationScript_IncludesScriptHeader()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "any_table",
                Type = PolicyType.All,
                PolicyName = "any_policy",
                UsingExpression = "true",
                WithCheckExpression = "true"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldContain("-- RLS Migration Script");
            script.ShouldContain("-- Generated by RLSMigrationHelper");
            script.ShouldContain("-- Generated at:");
        }

        #endregion

        #region Wildcard Pattern Matching Tests (via ApplyComponentPolicyAsync behavior)

        [Fact]
        public async Task ApplyComponentPolicyAsync_WithMatchingWildcardPrefix_FindsPolicy()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "*_component",
                Type = PolicyType.All,
                PolicyName = "component_isolation",
                UsingExpression = "owner_id = current_user_id()",
                WithCheckExpression = "owner_id = current_user_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act & Assert
            // Since we don't have a real database, the operation will fail when trying to execute SQL
            // but NOT with InvalidOperationException (which means the pattern was matched)
            var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
                helper.ApplyComponentPolicyAsync("account_component"));

            // The exception should NOT be InvalidOperationException about no matching pattern
            // It should be some database connection error instead
            exception.ShouldNotBeOfType<InvalidOperationException>();
        }

        [Fact]
        public async Task ApplyComponentPolicyAsync_WithMatchingWildcardSuffix_FindsPolicy()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "tenant_*",
                Type = PolicyType.All,
                PolicyName = "tenant_isolation",
                UsingExpression = "tenant_id = current_tenant_id()",
                WithCheckExpression = "tenant_id = current_tenant_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act & Assert
            var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
                helper.ApplyComponentPolicyAsync("tenant_documents"));

            exception.ShouldNotBeOfType<InvalidOperationException>();
        }

        [Fact]
        public async Task ApplyComponentPolicyAsync_WithMatchingMiddleWildcard_FindsPolicy()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "audit_*_log",
                Type = PolicyType.All,
                PolicyName = "audit_log_policy",
                UsingExpression = "tenant_id = current_tenant_id()",
                WithCheckExpression = "tenant_id = current_tenant_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act & Assert
            var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
                helper.ApplyComponentPolicyAsync("audit_user_log"));

            exception.ShouldNotBeOfType<InvalidOperationException>();
        }

        [Fact]
        public async Task ApplyComponentPolicyAsync_WithNonMatchingTable_ThrowsInvalidOperationException()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "*_component",
                Type = PolicyType.All,
                PolicyName = "component_isolation",
                UsingExpression = "owner_id = current_user_id()",
                WithCheckExpression = "owner_id = current_user_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                helper.ApplyComponentPolicyAsync("completely_different_table"));

            exception.Message.ShouldContain("No wildcard policy found that matches table");
            exception.Message.ShouldContain("completely_different_table");
        }

        [Fact]
        public async Task ApplyComponentPolicyAsync_WithNoWildcardPolicies_ThrowsInvalidOperationException()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            // Only register non-wildcard policies
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "specific_table",
                Type = PolicyType.All,
                PolicyName = "specific_policy",
                UsingExpression = "tenant_id = current_tenant_id()",
                WithCheckExpression = "tenant_id = current_tenant_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                helper.ApplyComponentPolicyAsync("any_component"));

            exception.Message.ShouldContain("No wildcard policy found");
        }

        [Fact]
        public async Task ApplyComponentPolicyAsync_WithEmptyTableName_ThrowsArgumentException()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                helper.ApplyComponentPolicyAsync(""));
        }

        [Fact]
        public async Task ApplyComponentPolicyAsync_WithNullTableName_ThrowsArgumentException()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                helper.ApplyComponentPolicyAsync(null!));
        }

        [Fact]
        public async Task ApplyComponentPolicyAsync_WithWhitespaceTableName_ThrowsArgumentException()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                helper.ApplyComponentPolicyAsync("   "));
        }

        #endregion

        #region Wildcard Pattern Case Sensitivity Tests

        [Fact]
        public async Task ApplyComponentPolicyAsync_MatchesPatternCaseInsensitive()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "*_COMPONENT",
                Type = PolicyType.All,
                PolicyName = "component_isolation",
                UsingExpression = "owner_id = current_user_id()",
                WithCheckExpression = "owner_id = current_user_id()"
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act & Assert - lowercase table name should match uppercase pattern
            var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
                helper.ApplyComponentPolicyAsync("account_component"));

            // Should NOT throw InvalidOperationException (pattern matched)
            exception.ShouldNotBeOfType<InvalidOperationException>();
        }

        #endregion

        #region Constructor Validation Tests

        [Fact]
        public void Constructor_WithNullPgClient_ThrowsArgumentNullException()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RLSMigrationHelper(null!, registry));
        }

        [Fact]
        public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
        {
            // Arrange
            var conn = new NpgsqlConnection("Host=localhost;Database=test;Username=test;Password=test");
            var client = new PgClient(conn);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RLSMigrationHelper(client, null!));
        }

        #endregion

        #region Policy WITH CHECK Fallback Tests

        [Fact]
        public void GenerateMigrationScript_WithCheckFallsBackToUsingExpression_WhenNotSpecified()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            registry.RegisterPolicy(new RLSPolicy
            {
                TableName = "fallback_table",
                Type = PolicyType.All, // ALL requires WITH CHECK
                PolicyName = "fallback_policy",
                UsingExpression = "tenant_id = current_tenant_id()"
                // WithCheckExpression not set - should fall back to UsingExpression
            });

            var helper = new RLSMigrationHelper(CreateTestPgClient(registry), registry);

            // Act
            var script = helper.GenerateMigrationScript();

            // Assert
            script.ShouldContain("USING (tenant_id = current_tenant_id())");
            script.ShouldContain("WITH CHECK (tenant_id = current_tenant_id())");
        }

        #endregion

        #region Helper Methods

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }

        #endregion
    }
}
