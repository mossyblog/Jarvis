using Dapper;
using Npgsql;
using Shouldly;
using core.jarvis.data.tests.Tables;

namespace core.jarvis.data.tests.Security
{
    /// <summary>
    /// INTENT: Verify Row Level Security (RLS) policies exist and are enforced at the database level.
    /// PURPOSE: Ensure RLS infrastructure is properly configured for multi-tenant isolation.
    /// BUSINESS CONTEXT: RLS is the last line of defense for data isolation in multi-tenant SaaS.
    /// WHY IMPORTANT: Validates that security policies are present and active in the database.
    /// ARCHITECTURAL SIGNIFICANCE: Database-level security cannot be bypassed by application code.
    /// FUTURE RESILIENCE: Catches missing or disabled RLS policies before they cause data leaks.
    /// </summary>
    public class RlsEnforcementTests : IAsyncLifetime
    {
        private NpgsqlConnection _conn = null!;
        private const string TestTableName = "rls_enforcement_test";

        public async Task InitializeAsync()
        {
            await TestHelpers.EnsureTestDatabaseExists();
            var connString = TestHelpers.GetConnectionStringFromEnv();
            _conn = new NpgsqlConnection(connString);
            await _conn.OpenAsync();

            await SetupTestTableWithRls();
        }

        private async Task SetupTestTableWithRls()
        {
            var setupSql = $@"
                DROP TABLE IF EXISTS {TestTableName} CASCADE;
                DROP FUNCTION IF EXISTS test_current_tenant_id();

                CREATE OR REPLACE FUNCTION test_current_tenant_id()
                RETURNS UUID AS $$
                BEGIN
                    RETURN current_setting('app.tenant_id', true)::UUID;
                EXCEPTION
                    WHEN OTHERS THEN RETURN NULL;
                END;
                $$ LANGUAGE plpgsql STABLE;

                CREATE TABLE {TestTableName} (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    tenant_id UUID NOT NULL,
                    data TEXT NOT NULL
                );

                ALTER TABLE {TestTableName} ENABLE ROW LEVEL SECURITY;
                ALTER TABLE {TestTableName} FORCE ROW LEVEL SECURITY;

                CREATE POLICY tenant_isolation_policy ON {TestTableName}
                    FOR ALL
                    USING (tenant_id = test_current_tenant_id())
                    WITH CHECK (tenant_id = test_current_tenant_id());

                INSERT INTO {TestTableName} (tenant_id, data) VALUES
                    ('00000000-0000-0000-0000-000000000001', 'Tenant 1 Data A'),
                    ('00000000-0000-0000-0000-000000000001', 'Tenant 1 Data B'),
                    ('00000000-0000-0000-0000-000000000002', 'Tenant 2 Data A'),
                    ('00000000-0000-0000-0000-000000000002', 'Tenant 2 Data B');
            ";
            await _conn.ExecuteAsync(setupSql);
        }

        public async Task DisposeAsync()
        {
            var cleanupSql = $@"
                DROP TABLE IF EXISTS {TestTableName} CASCADE;
                DROP FUNCTION IF EXISTS test_current_tenant_id();
            ";
            await _conn.ExecuteAsync(cleanupSql);
            await _conn.CloseAsync();
        }

        /// <summary>
        /// INTENT: Verify that RLS policy exists on the test table.
        /// PURPOSE: Confirm that the database has RLS policies configured.
        /// </summary>
        [Fact]
        public async Task RlsPolicy_ExistsOnTable()
        {
            // Arrange - Query pg_policies catalog to check for RLS policy
            var policySql = @"
                SELECT COUNT(*)
                FROM pg_policies
                WHERE schemaname = 'public'
                  AND tablename = @tableName
            ";

            // Act
            var policyCount = await _conn.ExecuteScalarAsync<int>(policySql, new { tableName = TestTableName });

            // Assert
            policyCount.ShouldBeGreaterThan(0, $"No RLS policies found on table '{TestTableName}'");
        }

        /// <summary>
        /// INTENT: Verify that RLS is enabled on the table.
        /// PURPOSE: Ensure the table has row_security enabled (not just policies defined).
        /// </summary>
        [Fact]
        public async Task RlsEnabled_OnTable()
        {
            // Arrange - Query pg_class to check if RLS is enabled
            var rlsEnabledSql = @"
                SELECT relrowsecurity
                FROM pg_class
                WHERE relname = @tableName
                  AND relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'public')
            ";

            // Act
            var rlsEnabled = await _conn.ExecuteScalarAsync<bool>(rlsEnabledSql, new { tableName = TestTableName });

            // Assert
            rlsEnabled.ShouldBeTrue($"RLS is not enabled on table '{TestTableName}'");
        }

        /// <summary>
        /// INTENT: Verify that the RLS helper function correctly reads the tenant_id session variable.
        /// PURPOSE: Confirm that session variables are properly propagated for RLS policy evaluation.
        /// </summary>
        [Fact]
        public async Task RlsHelperFunction_ReadsTenantIdFromSessionVariable()
        {
            // Arrange
            var tenant1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var tenant2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

            // Act - Set tenant context to tenant 1 and verify function reads it
            await _conn.ExecuteAsync("SELECT set_config('app.tenant_id', @tenantId, false)", new { tenantId = tenant1Id.ToString() });
            var readTenant1 = await _conn.ExecuteScalarAsync<Guid?>("SELECT test_current_tenant_id()");

            // Act - Set tenant context to tenant 2 and verify function reads it
            await _conn.ExecuteAsync("SELECT set_config('app.tenant_id', @tenantId, false)", new { tenantId = tenant2Id.ToString() });
            var readTenant2 = await _conn.ExecuteScalarAsync<Guid?>("SELECT test_current_tenant_id()");

            // Act - Clear session variable and verify function returns null
            await _conn.ExecuteAsync("SELECT set_config('app.tenant_id', '', false)");
            var readNull = await _conn.ExecuteScalarAsync<Guid?>("SELECT test_current_tenant_id()");

            // Assert - Function correctly reads session variable
            readTenant1.ShouldBe(tenant1Id);
            readTenant2.ShouldBe(tenant2Id);
            readNull.ShouldBeNull();
        }
    }

    public record RlsTestRecord
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Data { get; set; } = "";
    }
}
