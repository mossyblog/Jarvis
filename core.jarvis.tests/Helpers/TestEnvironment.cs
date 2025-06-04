using DotNetEnv;

namespace core.jarvis.tests.Helpers;

/// <summary>
/// Helper class to load test environment configuration.
/// </summary>
public static class TestEnvironment
{
    private static bool _isLoaded = false;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures the .env.local file is loaded for integration tests.
    /// </summary>
    public static void EnsureLoaded()
    {
        lock (_lock)
        {
            if (_isLoaded) return;

            // Try to load .env.local from various locations
            var possiblePaths = new[]
            {
                ".env.local",
                "../.env.local",
                "../../.env.local",
                "../../../.env.local",
                "../../../../.env.local",
                "../../../../../.env.local"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    Env.Load(path);
                    _isLoaded = true;
                    Console.WriteLine($"Loaded environment from: {Path.GetFullPath(path)}");
                    break;
                }
            }

            if (!_isLoaded)
            {
                Console.WriteLine("Warning: .env.local file not found. Using system environment variables.");
                _isLoaded = true;
            }
        }
    }

    /// <summary>
    /// Gets the Supabase URL from environment variables.
    /// </summary>
    public static string SupabaseUrl => 
        Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "http://127.0.0.1:54321";

    /// <summary>
    /// Gets the Supabase anonymous key from environment variables.
    /// </summary>
    public static string SupabaseKey => 
        Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? 
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";

    /// <summary>
    /// Gets the Supabase service role key from environment variables.
    /// </summary>
    public static string SupabaseServiceKey => 
        Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY") ?? 
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU";

    /// <summary>
    /// Checks if we're connected to a local Supabase instance.
    /// </summary>
    public static bool IsLocalSupabase => 
        SupabaseUrl.Contains("localhost") || SupabaseUrl.Contains("127.0.0.1");
}