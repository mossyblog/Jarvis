namespace core.jarvis.data.tests.Tables
{
    public static class TestHelpers
    {
        public static string GetConnectionStringFromEnv()
        {
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
                    throw new InvalidOperationException(".env.local file not found for test database settings.");
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
    }
}