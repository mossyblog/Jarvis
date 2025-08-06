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
        /// Ensures the minimum schema (user table, password_hash column, RLS policy) exists and is up-to-date.
        /// Creates or alters as needed.
        /// </summary>
        /// <param name="conn">The Npgsql database connection.</param>
        public static async Task EnsureMinimumSchema(NpgsqlConnection conn)
        {
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            // First, check if the schema already exists to avoid unnecessary work
            var tableExistsSql = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'account'
                )";
            var tableExists = (bool)(await new NpgsqlCommand(tableExistsSql, conn).ExecuteScalarAsync() ?? false);
            
            if (tableExists)
            {
                // Table exists, just ensure all components are up to date
                await EnsureSchemaComponents(conn);
                return;
            }

            // Table doesn't exist, need to create it
            // Use advisory lock to prevent concurrent schema creation
            const long schemaLockId = 12345; // Arbitrary but consistent lock ID for schema operations
            
            try
            {
                // Try to acquire an advisory lock
                var lockSql = "SELECT pg_try_advisory_lock(@lockId)";
                var lockAcquired = (bool)(await new NpgsqlCommand(lockSql, conn) { Parameters = { new("lockId", schemaLockId) } }.ExecuteScalarAsync() ?? false);
                
                if (!lockAcquired)
                {
                    // Another connection has the lock, wait for it to complete
                    await Task.Delay(100);
                    
                    // Try to acquire lock with timeout
                    var waitLockSql = "SELECT pg_advisory_lock(@lockId)";
                    await new NpgsqlCommand(waitLockSql, conn) { Parameters = { new("lockId", schemaLockId) } }.ExecuteNonQueryAsync();
                    
                    // Now we have the lock, but schema might already be created
                    tableExists = (bool)(await new NpgsqlCommand(tableExistsSql, conn).ExecuteScalarAsync() ?? false);
                    if (tableExists)
                    {
                        // Schema was created by another connection
                        await EnsureSchemaComponents(conn);
                        return;
                    }
                }

                // We have the lock and need to create the schema
                using var transaction = await conn.BeginTransactionAsync();
                
                try
                {
                    // Double-check table doesn't exist (in case of race condition)
                    tableExists = (bool)(await new NpgsqlCommand(tableExistsSql, conn, transaction).ExecuteScalarAsync() ?? false);
                    if (!tableExists)
                    {
                        // Create account table
                        var createTableSql = @"
                            CREATE TABLE IF NOT EXISTS ""account"" (
                                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                owner_entity_id UUID NOT NULL,
                                email TEXT UNIQUE NOT NULL,
                                password_hash TEXT NOT NULL,
                                is_active BOOLEAN NOT NULL DEFAULT true,
                                created_at TIMESTAMPTZ DEFAULT now()
                            );";
                        await new NpgsqlCommand(createTableSql, conn, transaction).ExecuteNonQueryAsync();
                    }

                    // Ensure all other schema components
                    await EnsureSchemaComponentsInTransaction(conn, transaction);

                    await transaction.CommitAsync();
                }
                catch (PostgresException ex) when (ex.SqlState == "23505") // duplicate_key
                {
                    // Another connection created the schema between our check and create
                    // This is fine, just rollback and verify
                    await transaction.RollbackAsync();
                    
                    // Verify the schema was created successfully
                    tableExists = (bool)(await new NpgsqlCommand(tableExistsSql, conn).ExecuteScalarAsync() ?? false);
                    if (!tableExists)
                    {
                        // Schema creation failed and table doesn't exist - this is a real error
                        throw;
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            finally
            {
                // Always release the advisory lock if connection is still open
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    try
                    {
                        var unlockSql = "SELECT pg_advisory_unlock(@lockId)";
                        await new NpgsqlCommand(unlockSql, conn) { Parameters = { new("lockId", schemaLockId) } }.ExecuteNonQueryAsync();
                    }
                    catch
                    {
                        // Best effort - lock will be released when connection closes anyway
                    }
                }
            }
        }

        /// <summary>
        /// Ensures all schema components (columns, RLS, policies) are up to date.
        /// Assumes the account table already exists.
        /// </summary>
        private static async Task EnsureSchemaComponents(NpgsqlConnection conn)
        {
            using var transaction = await conn.BeginTransactionAsync();
            try
            {
                await EnsureSchemaComponentsInTransaction(conn, transaction);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Ensures all schema components within an existing transaction.
        /// </summary>
        private static async Task EnsureSchemaComponentsInTransaction(NpgsqlConnection conn, NpgsqlTransaction transaction)
        {
            // Ensure password_hash column exists (idempotent, no default value)
            var addColumnSql = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='account' AND column_name='password_hash'
                    ) THEN
                        ALTER TABLE ""account"" ADD COLUMN password_hash TEXT NOT NULL;
                    END IF;
                END
                $$;";
            await new NpgsqlCommand(addColumnSql, conn, transaction).ExecuteNonQueryAsync();

            // Enable RLS if not already enabled
            try
            {
                var rlsEnabledSql = @"
                    SELECT rowsecurity 
                    FROM pg_tables 
                    WHERE schemaname = 'public' AND tablename = 'account'";
                var rlsEnabled = (bool)(await new NpgsqlCommand(rlsEnabledSql, conn, transaction).ExecuteScalarAsync() ?? false);
                
                if (!rlsEnabled)
                {
                    var enableRlsSql = @"ALTER TABLE ""account"" ENABLE ROW LEVEL SECURITY;";
                    await new NpgsqlCommand(enableRlsSql, conn, transaction).ExecuteNonQueryAsync();
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "42P27") // duplicate_object
            {
                // RLS is already enabled, which is fine
            }

            // Create a default RLS policy if none exists
            var policyExistsSql = "SELECT EXISTS (SELECT 1 FROM pg_policies WHERE tablename = 'account')";
            var hasPolicy = (bool)(await new NpgsqlCommand(policyExistsSql, conn, transaction).ExecuteScalarAsync() ?? false);
            if (!hasPolicy)
            {
                try
                {
                    var createPolicySql = @"
                        CREATE POLICY account_select_policy ON ""account""
                        FOR SELECT
                        USING (true);";
                    await new NpgsqlCommand(createPolicySql, conn, transaction).ExecuteNonQueryAsync();
                }
                catch (PostgresException ex) when (ex.SqlState == "42710") // duplicate_object
                {
                    // Policy already exists, which is fine
                }
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