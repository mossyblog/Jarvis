using core.jarvis.data.RLS;
using Shouldly;

namespace core.jarvis.data.tests.RLS
{
    /// <summary>
    /// INTENT: Validate RLSPolicyRegistry SQL generation methods produce correct PostgreSQL syntax.
    /// PURPOSE: Ensure SQL statements for enabling RLS, creating policies, and dropping policies are correct.
    /// BUSINESS CONTEXT: Enables automated RLS migration script generation.
    /// WHY IMPORTANT: Incorrect SQL syntax would break database migrations.
    /// ARCHITECTURAL SIGNIFICANCE: RLS is a critical security layer for multi-tenant isolation.
    /// FUTURE RESILIENCE: Protects against regressions in SQL generation logic.
    /// </summary>
    public class RLSPolicyRegistryTests
    {
        #region GenerateEnableRLSSql Tests

        /// <summary>
        /// INTENT: Verify GenerateEnableRLSSql produces correct ALTER TABLE ENABLE ROW LEVEL SECURITY syntax.
        /// </summary>
        [Fact]
        public void GenerateEnableRLSSql_ProducesCorrectAlterTableSyntax()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var tableName = "tenant_data";

            // Act
            var sql = registry.GenerateEnableRLSSql(tableName);

            // Assert
            sql.ShouldBe("ALTER TABLE tenant_data ENABLE ROW LEVEL SECURITY;");
        }

        /// <summary>
        /// INTENT: Verify GenerateEnableRLSSql works with schema-qualified table names.
        /// </summary>
        [Fact]
        public void GenerateEnableRLSSql_WithSchemaQualifiedTable_ProducesCorrectSyntax()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var tableName = "public.user_data";

            // Act
            var sql = registry.GenerateEnableRLSSql(tableName);

            // Assert
            sql.ShouldBe("ALTER TABLE public.user_data ENABLE ROW LEVEL SECURITY;");
        }

        #endregion

        #region GenerateForceRLSSql Tests

        /// <summary>
        /// INTENT: Verify GenerateForceRLSSql produces FORCE variant of ALTER TABLE.
        /// </summary>
        [Fact]
        public void GenerateForceRLSSql_ProducesForceVariant()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var tableName = "sensitive_data";

            // Act
            var sql = registry.GenerateForceRLSSql(tableName);

            // Assert
            sql.ShouldBe("ALTER TABLE sensitive_data FORCE ROW LEVEL SECURITY;");
        }

        /// <summary>
        /// INTENT: Verify GenerateForceRLSSql works with schema-qualified table names.
        /// </summary>
        [Fact]
        public void GenerateForceRLSSql_WithSchemaQualifiedTable_ProducesCorrectSyntax()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var tableName = "app.confidential_records";

            // Act
            var sql = registry.GenerateForceRLSSql(tableName);

            // Assert
            sql.ShouldBe("ALTER TABLE app.confidential_records FORCE ROW LEVEL SECURITY;");
        }

        #endregion

        #region GenerateCreatePolicySql - PolicyType Tests

        /// <summary>
        /// INTENT: Verify GenerateCreatePolicySql handles PolicyType.Select correctly.
        /// </summary>
        [Fact]
        public void GenerateCreatePolicySql_WithSelectType_ProducesSelectPolicy()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var policy = new RLSPolicy
            {
                TableName = "user_data",
                Type = PolicyType.Select,
                PolicyName = "user_select_policy",
                UsingExpression = "user_id = current_setting('app.user_id', true)::uuid"
            };

            // Act
            var sql = registry.GenerateCreatePolicySql(policy);

            // Assert
            sql.ShouldBe("CREATE POLICY user_select_policy ON user_data FOR SELECT USING (user_id = current_setting('app.user_id', true)::uuid);");
        }

        /// <summary>
        /// INTENT: Verify GenerateCreatePolicySql handles PolicyType.Insert correctly.
        /// </summary>
        [Fact]
        public void GenerateCreatePolicySql_WithInsertType_ProducesInsertPolicy()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var policy = new RLSPolicy
            {
                TableName = "audit_log",
                Type = PolicyType.Insert,
                PolicyName = "audit_insert_policy",
                UsingExpression = "true"
            };

            // Act
            var sql = registry.GenerateCreatePolicySql(policy);

            // Assert
            sql.ShouldBe("CREATE POLICY audit_insert_policy ON audit_log FOR INSERT USING (true);");
        }

        /// <summary>
        /// INTENT: Verify GenerateCreatePolicySql handles PolicyType.Update correctly.
        /// </summary>
        [Fact]
        public void GenerateCreatePolicySql_WithUpdateType_ProducesUpdatePolicy()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var policy = new RLSPolicy
            {
                TableName = "documents",
                Type = PolicyType.Update,
                PolicyName = "doc_update_policy",
                UsingExpression = "owner_id = current_setting('app.user_id', true)::uuid"
            };

            // Act
            var sql = registry.GenerateCreatePolicySql(policy);

            // Assert
            sql.ShouldBe("CREATE POLICY doc_update_policy ON documents FOR UPDATE USING (owner_id = current_setting('app.user_id', true)::uuid);");
        }

        /// <summary>
        /// INTENT: Verify GenerateCreatePolicySql handles PolicyType.Delete correctly.
        /// </summary>
        [Fact]
        public void GenerateCreatePolicySql_WithDeleteType_ProducesDeletePolicy()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var policy = new RLSPolicy
            {
                TableName = "temp_records",
                Type = PolicyType.Delete,
                PolicyName = "temp_delete_policy",
                UsingExpression = "created_by = current_setting('app.user_id', true)::uuid"
            };

            // Act
            var sql = registry.GenerateCreatePolicySql(policy);

            // Assert
            sql.ShouldBe("CREATE POLICY temp_delete_policy ON temp_records FOR DELETE USING (created_by = current_setting('app.user_id', true)::uuid);");
        }

        /// <summary>
        /// INTENT: Verify GenerateCreatePolicySql handles PolicyType.All correctly.
        /// </summary>
        [Fact]
        public void GenerateCreatePolicySql_WithAllType_ProducesAllPolicy()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var policy = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = "tenant_isolation",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            };

            // Act
            var sql = registry.GenerateCreatePolicySql(policy);

            // Assert
            sql.ShouldBe("CREATE POLICY tenant_isolation ON tenant_data FOR ALL USING (tenant_id = current_setting('app.tenant_id', true)::uuid);");
        }

        #endregion

        #region GenerateCreatePolicySql - WithCheckExpression Tests

        /// <summary>
        /// INTENT: Verify GenerateCreatePolicySql includes WITH CHECK when WithCheckExpression is provided.
        /// </summary>
        [Fact]
        public void GenerateCreatePolicySql_WithCheckExpression_IncludesWithCheckClause()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var policy = new RLSPolicy
            {
                TableName = "user_data",
                Type = PolicyType.Insert,
                PolicyName = "user_insert_policy",
                UsingExpression = "true",
                WithCheckExpression = "user_id = current_setting('app.user_id', true)::uuid"
            };

            // Act
            var sql = registry.GenerateCreatePolicySql(policy);

            // Assert
            sql.ShouldBe("CREATE POLICY user_insert_policy ON user_data FOR INSERT USING (true) WITH CHECK (user_id = current_setting('app.user_id', true)::uuid);");
        }

        /// <summary>
        /// INTENT: Verify GenerateCreatePolicySql omits WITH CHECK when WithCheckExpression is null.
        /// </summary>
        [Fact]
        public void GenerateCreatePolicySql_WithoutWithCheckExpression_OmitsWithCheckClause()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var policy = new RLSPolicy
            {
                TableName = "read_only_data",
                Type = PolicyType.Select,
                PolicyName = "read_only_policy",
                UsingExpression = "is_visible = true",
                WithCheckExpression = null
            };

            // Act
            var sql = registry.GenerateCreatePolicySql(policy);

            // Assert
            sql.ShouldNotContain("WITH CHECK");
            sql.ShouldBe("CREATE POLICY read_only_policy ON read_only_data FOR SELECT USING (is_visible = true);");
        }

        /// <summary>
        /// INTENT: Verify GenerateCreatePolicySql handles complex USING and WITH CHECK expressions.
        /// </summary>
        [Fact]
        public void GenerateCreatePolicySql_WithComplexExpressions_ProducesCorrectSql()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var policy = new RLSPolicy
            {
                TableName = "sensitive_data",
                Type = PolicyType.Update,
                PolicyName = "role_based_update",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid AND owner_id = current_setting('app.user_id', true)::uuid",
                WithCheckExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid AND classification IN ('public', 'internal')"
            };

            // Act
            var sql = registry.GenerateCreatePolicySql(policy);

            // Assert
            sql.ShouldStartWith("CREATE POLICY role_based_update ON sensitive_data FOR UPDATE USING (");
            sql.ShouldContain("tenant_id = current_setting('app.tenant_id', true)::uuid AND owner_id = current_setting('app.user_id', true)::uuid");
            sql.ShouldContain("WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid AND classification IN ('public', 'internal'))");
            sql.ShouldEndWith(";");
        }

        #endregion

        #region GenerateDropPolicySql Tests

        /// <summary>
        /// INTENT: Verify GenerateDropPolicySql produces DROP POLICY IF EXISTS syntax.
        /// </summary>
        [Fact]
        public void GenerateDropPolicySql_ProducesDropPolicyIfExistsSyntax()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var tableName = "tenant_data";
            var policyName = "tenant_isolation";

            // Act
            var sql = registry.GenerateDropPolicySql(tableName, policyName);

            // Assert
            sql.ShouldBe("DROP POLICY IF EXISTS tenant_isolation ON tenant_data;");
        }

        /// <summary>
        /// INTENT: Verify GenerateDropPolicySql works with schema-qualified table names.
        /// </summary>
        [Fact]
        public void GenerateDropPolicySql_WithSchemaQualifiedTable_ProducesCorrectSyntax()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();
            var tableName = "public.user_data";
            var policyName = "user_access_policy";

            // Act
            var sql = registry.GenerateDropPolicySql(tableName, policyName);

            // Assert
            sql.ShouldBe("DROP POLICY IF EXISTS user_access_policy ON public.user_data;");
        }

        #endregion

        #region GetAllPostgresPolicies Tests

        /// <summary>
        /// INTENT: Verify GetAllPostgresPolicies returns only policies with PolicyName set.
        /// </summary>
        [Fact]
        public void GetAllPostgresPolicies_ReturnsOnlyPoliciesWithPolicyName()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            // Policy WITH PostgreSQL definition
            var postgresPolicy = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = "tenant_isolation",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            };

            // Policy WITHOUT PostgreSQL definition (SDK-level only)
            var sdkOnlyPolicy = new RLSPolicy
            {
                TableName = "user_data",
                Type = PolicyType.Select,
                WhereClause = claims => "user_id = '123'"
            };

            // Another policy WITH PostgreSQL definition
            var anotherPostgresPolicy = new RLSPolicy
            {
                TableName = "sensitive_data",
                Type = PolicyType.Select,
                PolicyName = "sensitive_select",
                UsingExpression = "classification = 'public'"
            };

            registry.RegisterPolicy(postgresPolicy);
            registry.RegisterPolicy(sdkOnlyPolicy);
            registry.RegisterPolicy(anotherPostgresPolicy);

            // Act
            var postgresPolicies = registry.GetAllPostgresPolicies().ToList();

            // Assert
            postgresPolicies.Count.ShouldBe(2);
            postgresPolicies.ShouldContain(p => p.PolicyName == "tenant_isolation");
            postgresPolicies.ShouldContain(p => p.PolicyName == "sensitive_select");
            postgresPolicies.ShouldNotContain(p => p.PolicyName == null);
        }

        /// <summary>
        /// INTENT: Verify GetAllPostgresPolicies returns empty when no policies have PolicyName.
        /// </summary>
        [Fact]
        public void GetAllPostgresPolicies_WhenNoPoliciesHavePolicyName_ReturnsEmpty()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            var sdkOnlyPolicy1 = new RLSPolicy
            {
                TableName = "table1",
                Type = PolicyType.Select,
                WhereClause = claims => "1=1"
            };

            var sdkOnlyPolicy2 = new RLSPolicy
            {
                TableName = "table2",
                Type = PolicyType.Insert,
                CheckFunction = (claims, data) => true
            };

            registry.RegisterPolicy(sdkOnlyPolicy1);
            registry.RegisterPolicy(sdkOnlyPolicy2);

            // Act
            var postgresPolicies = registry.GetAllPostgresPolicies().ToList();

            // Assert
            postgresPolicies.ShouldBeEmpty();
        }

        /// <summary>
        /// INTENT: Verify GetAllPostgresPolicies returns empty when registry has no policies.
        /// </summary>
        [Fact]
        public void GetAllPostgresPolicies_WhenRegistryEmpty_ReturnsEmpty()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            // Act
            var postgresPolicies = registry.GetAllPostgresPolicies().ToList();

            // Assert
            postgresPolicies.ShouldBeEmpty();
        }

        #endregion

        #region GetAllPolicies Tests

        /// <summary>
        /// INTENT: Verify GetAllPolicies returns all registered policies regardless of type.
        /// </summary>
        [Fact]
        public void GetAllPolicies_ReturnsAllRegisteredPolicies()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            var policy1 = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = "tenant_isolation",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            };

            var policy2 = new RLSPolicy
            {
                TableName = "user_data",
                Type = PolicyType.Select,
                WhereClause = claims => "user_id = '123'"
            };

            var policy3 = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.Insert,
                PolicyName = "tenant_insert",
                UsingExpression = "true",
                WithCheckExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            };

            registry.RegisterPolicy(policy1);
            registry.RegisterPolicy(policy2);
            registry.RegisterPolicy(policy3);

            // Act
            var allPolicies = registry.GetAllPolicies().ToList();

            // Assert
            allPolicies.Count.ShouldBe(3);
            allPolicies.ShouldContain(policy1);
            allPolicies.ShouldContain(policy2);
            allPolicies.ShouldContain(policy3);
        }

        /// <summary>
        /// INTENT: Verify GetAllPolicies returns empty when registry has no policies.
        /// </summary>
        [Fact]
        public void GetAllPolicies_WhenRegistryEmpty_ReturnsEmpty()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            // Act
            var allPolicies = registry.GetAllPolicies().ToList();

            // Assert
            allPolicies.ShouldBeEmpty();
        }

        /// <summary>
        /// INTENT: Verify GetAllPolicies returns policies from multiple tables.
        /// </summary>
        [Fact]
        public void GetAllPolicies_WithMultipleTables_ReturnsAllPolicies()
        {
            // Arrange
            var registry = new RLSPolicyRegistry();

            var tables = new[] { "table_a", "table_b", "table_c" };
            foreach (var table in tables)
            {
                registry.RegisterPolicy(new RLSPolicy
                {
                    TableName = table,
                    Type = PolicyType.Select,
                    PolicyName = $"{table}_select",
                    UsingExpression = "true"
                });
            }

            // Act
            var allPolicies = registry.GetAllPolicies().ToList();

            // Assert
            allPolicies.Count.ShouldBe(3);
            allPolicies.Select(p => p.TableName).ShouldBe(tables, ignoreOrder: true);
        }

        #endregion

        #region HasPostgresPolicy Property Tests

        /// <summary>
        /// INTENT: Verify HasPostgresPolicy returns true when both PolicyName and UsingExpression are set.
        /// </summary>
        [Fact]
        public void HasPostgresPolicy_WhenBothPolicyNameAndUsingExpressionSet_ReturnsTrue()
        {
            // Arrange
            var policy = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = "tenant_isolation",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            };

            // Act & Assert
            policy.HasPostgresPolicy.ShouldBeTrue();
        }

        /// <summary>
        /// INTENT: Verify HasPostgresPolicy returns false when PolicyName is null.
        /// </summary>
        [Fact]
        public void HasPostgresPolicy_WhenPolicyNameNull_ReturnsFalse()
        {
            // Arrange
            var policy = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = null,
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            };

            // Act & Assert
            policy.HasPostgresPolicy.ShouldBeFalse();
        }

        /// <summary>
        /// INTENT: Verify HasPostgresPolicy returns false when PolicyName is empty string.
        /// </summary>
        [Fact]
        public void HasPostgresPolicy_WhenPolicyNameEmpty_ReturnsFalse()
        {
            // Arrange
            var policy = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = "",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            };

            // Act & Assert
            policy.HasPostgresPolicy.ShouldBeFalse();
        }

        /// <summary>
        /// INTENT: Verify HasPostgresPolicy returns false when UsingExpression is null.
        /// </summary>
        [Fact]
        public void HasPostgresPolicy_WhenUsingExpressionNull_ReturnsFalse()
        {
            // Arrange
            var policy = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = "tenant_isolation",
                UsingExpression = null
            };

            // Act & Assert
            policy.HasPostgresPolicy.ShouldBeFalse();
        }

        /// <summary>
        /// INTENT: Verify HasPostgresPolicy returns false when UsingExpression is empty string.
        /// </summary>
        [Fact]
        public void HasPostgresPolicy_WhenUsingExpressionEmpty_ReturnsFalse()
        {
            // Arrange
            var policy = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = "tenant_isolation",
                UsingExpression = ""
            };

            // Act & Assert
            policy.HasPostgresPolicy.ShouldBeFalse();
        }

        /// <summary>
        /// INTENT: Verify HasPostgresPolicy returns false when both PolicyName and UsingExpression are null.
        /// </summary>
        [Fact]
        public void HasPostgresPolicy_WhenBothNull_ReturnsFalse()
        {
            // Arrange
            var policy = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = null,
                UsingExpression = null
            };

            // Act & Assert
            policy.HasPostgresPolicy.ShouldBeFalse();
        }

        /// <summary>
        /// INTENT: Verify HasPostgresPolicy is independent of WithCheckExpression.
        /// </summary>
        [Fact]
        public void HasPostgresPolicy_WithCheckExpressionDoesNotAffectResult()
        {
            // Arrange
            var policyWithCheck = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = "tenant_isolation",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid",
                WithCheckExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid"
            };

            var policyWithoutCheck = new RLSPolicy
            {
                TableName = "tenant_data",
                Type = PolicyType.All,
                PolicyName = "tenant_isolation",
                UsingExpression = "tenant_id = current_setting('app.tenant_id', true)::uuid",
                WithCheckExpression = null
            };

            // Act & Assert
            policyWithCheck.HasPostgresPolicy.ShouldBeTrue();
            policyWithoutCheck.HasPostgresPolicy.ShouldBeTrue();
        }

        #endregion
    }
}
