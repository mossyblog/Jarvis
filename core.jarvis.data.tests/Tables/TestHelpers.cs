using System;
using System.Collections.Generic;
using System.IO;

namespace core.jarvis.data.tests.Tables
{
    public static class TestHelpers
    {
        public static string GetConnectionStringFromEnv()
        {
            // First check if TEST_DATABASE_URL environment variable is set (for CI/CD)
            var testDbUrl = Environment.GetEnvironmentVariable("TEST_DATABASE_URL");
            if (!string.IsNullOrEmpty(testDbUrl))
            {
                return testDbUrl;
            }

            // Fall back to .env.local file for local development
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env.local");
            if (!File.Exists(envPath))
            {
                // Try project root
                var dir = Directory.GetCurrentDirectory();
                while (dir != null && !File.Exists(Path.Combine(dir, ".env.local")))
                {
                    dir = Directory.GetParent(dir)?.FullName;
                }
                if (dir != null)
                    envPath = Path.Combine(dir, ".env.local");
                else
                {
                    // If no .env.local file, use default connection string
                    return "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres";
                }
            }

            var lines = File.ReadAllLines(envPath);
            string host = "localhost", port = "5432", user = "postgres", pass = "postgres", db = "postgres";
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("PGHOST=")) host = trimmed.Substring(7);
                if (trimmed.StartsWith("PGPORT=")) port = trimmed.Substring(7);
                if (trimmed.StartsWith("PGUSER=")) user = trimmed.Substring(7);
                if (trimmed.StartsWith("PGPASSWORD=")) pass = trimmed.Substring(11);
                if (trimmed.StartsWith("PGDATABASE=")) db = trimmed.Substring(11);
            }
            return $"Host={host};Port={port};Username={user};Password={pass};Database={db}";
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