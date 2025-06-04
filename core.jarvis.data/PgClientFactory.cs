using Npgsql;

namespace core.jarvis.data
{
    /// <summary>
    /// Factory for creating and initializing PgClient instances.
    /// Ensures required tables, columns, and RLS policies exist and are up-to-date.
    /// </summary>
    public static class PgClientFactory
    {
        static PgClientFactory()
        {
            // Configure Dapper to handle snake_case column names
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        }
        /// <summary>
        /// Ensures the minimum schema (users table, password_hash column, RLS policy) exists and is up-to-date.
        /// Creates or alters as needed.
        /// </summary>
        /// <param name="conn">The Npgsql database connection.</param>
        public static async Task EnsureMinimumSchema(NpgsqlConnection conn)
        {
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            // Create users table if not exists
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    email TEXT UNIQUE NOT NULL,
                    password_hash TEXT NOT NULL,
                    created_at TIMESTAMPTZ DEFAULT now()
                );";
            await new NpgsqlCommand(createTableSql, conn).ExecuteNonQueryAsync();

            // Ensure password_hash column exists (idempotent, no default value)
            var addColumnSql = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='users' AND column_name='password_hash'
                    ) THEN
                        ALTER TABLE users ADD COLUMN password_hash TEXT NOT NULL;
                    END IF;
                END
                $$;";
            await new NpgsqlCommand(addColumnSql, conn).ExecuteNonQueryAsync();

            // Enable RLS if not already enabled
            var enableRlsSql = "ALTER TABLE users ENABLE ROW LEVEL SECURITY;";
            await new NpgsqlCommand(enableRlsSql, conn).ExecuteNonQueryAsync();

            // Create a default RLS policy if none exists
            var policyExistsSql = "SELECT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'users')";
            var hasPolicy = (bool)(await new NpgsqlCommand(policyExistsSql, conn).ExecuteScalarAsync() ?? false);
            if (!hasPolicy)
            {
                var createPolicySql = @"
                    CREATE POLICY users_select_policy ON users
                    FOR SELECT
                    USING (true);";
                await new NpgsqlCommand(createPolicySql, conn).ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Creates a PgClient, ensuring the minimum schema and policies exist.
        /// </summary>
        /// <param name="conn">The Npgsql database connection.</param>
        /// <param name="rlsPolicies">Optional RLS policy registry. If not provided, uses default policies.</param>
        /// <returns>Initialized PgClient instance.</returns>
        public static async Task<PgClient> Create(NpgsqlConnection conn, RLS.RLSPolicyRegistry? rlsPolicies = null)
        {
            await EnsureMinimumSchema(conn);
            return new PgClient(conn, rlsPolicies);
        }
    }
}