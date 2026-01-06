using Npgsql;

namespace core.jarvis.tests.Helpers;

/// <summary>
/// Provides connection string for integration tests.
/// Database setup is handled by Docker initialization scripts.
/// </summary>
public static class TestDatabaseSetup
{
    private static readonly string DefaultConnectionString = 
        Environment.GetEnvironmentVariable("TEST_DATABASE_URL") ?? 
        "Host=127.0.0.1;Port=5432;Database=jarvis_test;Username=supabase_admin;Password=postgres";
    

    
    
    
    
    /// <summary>
    /// Gets the connection string used by test infrastructure.
    /// </summary>
    public static string GetConnectionString()
    {
        return DefaultConnectionString;
    }
}