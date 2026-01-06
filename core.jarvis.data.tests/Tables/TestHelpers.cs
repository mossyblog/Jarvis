using System;
using System.Collections.Generic;
using System.IO;
using Npgsql;

namespace core.jarvis.data.tests.Tables
{
    public static class TestHelpers
    {
        /// <summary>
        /// Ensures the jarvis_test database exists before running tests.
        /// </summary>
        public static async Task EnsureTestDatabaseExists()
        {
            var testConnString = GetConnectionStringFromEnv();
            var builder = new NpgsqlConnectionStringBuilder(testConnString);
            var dbName = builder.Database;
            
            // Connect to postgres database to create test database if needed
            builder.Database = "postgres";
            using var adminConn = new NpgsqlConnection(builder.ToString());
            await adminConn.OpenAsync();
            
            // Check if database exists
            var checkDbSql = "SELECT 1 FROM pg_database WHERE datname = @dbName";
            using var checkCmd = new NpgsqlCommand(checkDbSql, adminConn);
            checkCmd.Parameters.AddWithValue("dbName", dbName);
            var exists = await checkCmd.ExecuteScalarAsync() != null;
            
            if (!exists)
            {
                // Create database if it doesn't exist
                var createDbSql = $"CREATE DATABASE {dbName}";
                using var createCmd = new NpgsqlCommand(createDbSql, adminConn);
                await createCmd.ExecuteNonQueryAsync();
            }
        }
        public static string GetConnectionStringFromEnv()
        {
            // First check if TEST_DATABASE_URL environment variable is set (for CI/CD)
            var testDbUrl = Environment.GetEnvironmentVariable("TEST_DATABASE_URL");
            if (!string.IsNullOrEmpty(testDbUrl))
            {
                return testDbUrl;
            }

            // Use jarvis_test database with supabase_admin user to match integration tests
            return "Host=127.0.0.1;Port=5432;Username=supabase_admin;Password=postgres;Database=jarvis_test";
        }

        public static string GetSupabaseAdminConnectionString()
        {
            // Get the base connection string
            var baseConnStr = GetConnectionStringFromEnv();
            
            // Parse and update the username to supabase_admin
            var parts = baseConnStr.Split(';');
            var updatedParts = new List<string>();
            
            foreach (var part in parts)
            {
                if (part.StartsWith("Username="))
                {
                    updatedParts.Add("Username=supabase_admin");
                }
                else
                {
                    updatedParts.Add(part);
                }
            }
            
            return string.Join(";", updatedParts);
        }
    }
}