using Npgsql;

namespace core.jarvis.tests.Helpers;

/// <summary>
/// Provides database setup and teardown for integration tests.
/// </summary>
public static class TestDatabaseSetup
{
    private static readonly string DefaultConnectionString = 
        Environment.GetEnvironmentVariable("TEST_DATABASE_URL") ?? 
        "Host=localhost;Port=5432;Database=jarvis_test;Username=postgres;Password=postgres";

    /// <summary>
    /// Sets up the test database by running the setup script.
    /// </summary>
    public static async Task SetupAsync(string? connectionString = null)
    {
        connectionString ??= DefaultConnectionString;
        
        // Read the setup script
        var scriptPath = Path.Combine(GetProjectRoot(), "Scripts", "setup-test-database.sql");
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Database setup script not found at: {scriptPath}");
        }
        
        var script = await File.ReadAllTextAsync(scriptPath);
        
        // Execute the script
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        await using var command = new NpgsqlCommand(script, connection);
        await command.ExecuteNonQueryAsync();
    }
    
    /// <summary>
    /// Cleans all data from test tables without dropping them.
    /// Useful for resetting state between tests.
    /// </summary>
    public static async Task CleanAsync(string? connectionString = null)
    {
        connectionString ??= DefaultConnectionString;
        
        var tables = new[]
        {
            "component_snapshots",
            "entity_relationship",
            "blog_post_component",
            "blog_component",
            "order_component",
            "invoice_test_component",
            "payment_test_component",
            "work_order_test_component",
            "velocity_component",
            "position_component",
            "test_component",
            "audit_event",
            "security_audit_event",
            "security_token"
            // Note: We don't clean the users table as it contains test users
        };
        
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        foreach (var table in tables)
        {
            var sql = $"TRUNCATE TABLE {table} CASCADE";
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
    
    /// <summary>
    /// Checks if the test database is properly set up.
    /// </summary>
    public static async Task<bool> IsDatabaseSetupAsync(string? connectionString = null)
    {
        connectionString ??= DefaultConnectionString;
        
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Check if users table exists and has test user
            var sql = @"
                SELECT COUNT(*) 
                FROM users 
                WHERE email = 'test@example.com'";
                
            await using var command = new NpgsqlCommand(sql, connection);
            var count = (long)(await command.ExecuteScalarAsync() ?? 0);
            
            return count > 0;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Ensures the database is set up, creating it if necessary.
    /// </summary>
    public static async Task EnsureSetupAsync(string? connectionString = null)
    {
        if (!await IsDatabaseSetupAsync(connectionString))
        {
            await SetupAsync(connectionString);
        }
    }
    
    private static string GetProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null && !File.Exists(Path.Combine(dir, "core.jarvis.tests.csproj")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        
        if (dir == null)
        {
            throw new InvalidOperationException("Could not find project root directory");
        }
        
        return dir;
    }
    
    /// <summary>
    /// Gets the default test database connection string.
    /// </summary>
    public static string GetConnectionString()
    {
        return DefaultConnectionString;
    }
}