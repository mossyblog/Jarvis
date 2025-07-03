using Npgsql;

namespace core.jarvis.tests.Helpers;

/// <summary>
/// Provides database setup and teardown for integration tests.
/// </summary>
public static class TestDatabaseSetup
{
    private static readonly string DefaultConnectionString = 
        Environment.GetEnvironmentVariable("TEST_DATABASE_URL") ?? 
        "Host=localhost;Port=5432;Database=jarvis_test;Username=supabase_admin;Password=postgres";

    /// <summary>
    /// Sets up the test database by running the setup script.
    /// </summary>
    public static async Task SetupAsync(string? connectionString = null)
    {
        connectionString ??= DefaultConnectionString;
        
        // For initial database creation, connect to postgres database
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database;
        builder.Database = "postgres";
        var adminConnectionString = builder.ToString();
        
        // First, ensure the database exists
        await using (var adminConn = new NpgsqlConnection(adminConnectionString))
        {
            await adminConn.OpenAsync();
            
            // Check if database exists
            var checkDbSql = "SELECT 1 FROM pg_database WHERE datname = @dbName";
            await using var checkCmd = new NpgsqlCommand(checkDbSql, adminConn);
            checkCmd.Parameters.AddWithValue("dbName", dbName);
            var exists = await checkCmd.ExecuteScalarAsync() != null;
            
            if (!exists)
            {
                // Create database if it doesn't exist
                var createDbSql = $"CREATE DATABASE {dbName}";
                await using var createCmd = new NpgsqlCommand(createDbSql, adminConn);
                await createCmd.ExecuteNonQueryAsync();
            }
        }
        
        // Now run the setup script on the actual database
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Read and execute the setup script, but skip the DROP/CREATE DATABASE commands
        var scriptPath = Path.Combine(GetProjectRoot(), "Scripts", "setup-test-database.sql");
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Database setup script not found at: {scriptPath}");
        }
        
        var script = await File.ReadAllTextAsync(scriptPath);
        
        // Remove DROP DATABASE and CREATE DATABASE lines, and the \c command
        var lines = script.Split('\n')
            .Where(line => !line.Trim().StartsWith("DROP DATABASE", StringComparison.OrdinalIgnoreCase) &&
                          !line.Trim().StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase) &&
                          !line.Trim().StartsWith("\\c ", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        var cleanedScript = string.Join('\n', lines);
        
        await using var command = new NpgsqlCommand(cleanedScript, connection);
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
            "work_order_component",
            "velocity_component",
            "position_component",
            "test_component",
            "audit_event",
            "security_audit_event_component",
            "auth_token_component",
            "security_profile_component",
            "role_component",
            "permission_component",
            "navigation_item"
            // Note: We don't clean the account_component table as it contains test accounts
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
            
            // Check if account_component table exists and has test account
            var sql = @"
                SELECT COUNT(*) 
                FROM ""account_component"" 
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
        
        // If we're in a bin directory, go up to the project root
        if (dir.Contains("/bin/") || dir.Contains("\\bin\\"))
        {
            // Go up from bin/Debug/net8.0 to the project directory
            for (int i = 0; i < 3 && dir != null; i++)
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
        }
        
        // Now search for the core.jarvis.tests directory
        while (dir != null)
        {
            var testsPath = Path.Combine(dir, "core.jarvis.tests");
            if (Directory.Exists(testsPath) && File.Exists(Path.Combine(testsPath, "core.jarvis.tests.csproj")))
            {
                return testsPath;
            }
            
            // Also check if we're already in core.jarvis.tests
            if (File.Exists(Path.Combine(dir, "core.jarvis.tests.csproj")))
            {
                return dir;
            }
            
            dir = Directory.GetParent(dir)?.FullName;
        }
        
        throw new InvalidOperationException($"Could not find project root directory. Current directory: {Directory.GetCurrentDirectory()}");
    }
    
    /// <summary>
    /// Gets the default test database connection string.
    /// </summary>
    public static string GetConnectionString()
    {
        return DefaultConnectionString;
    }
}