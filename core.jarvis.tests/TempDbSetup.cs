using core.jarvis.tests.Helpers;

// Create a simple console app to run database setup
try
{
    Console.WriteLine("Setting up test database...");
    
    // Use the connection string that matches our Docker container with supabase_admin
    var connectionString = "Host=localhost;Port=5432;Database=jarvis_test;Username=supabase_admin;Password=postgres";
    
    await TestDatabaseSetup.SetupAsync(connectionString);
    
    Console.WriteLine("Database setup completed successfully!");
    
    // Verify setup
    var isSetup = await TestDatabaseSetup.IsDatabaseSetupAsync(connectionString);
    Console.WriteLine($"Database verification: {(isSetup ? "PASSED" : "FAILED")}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error setting up database: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
} 