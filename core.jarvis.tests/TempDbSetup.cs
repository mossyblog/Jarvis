using core.jarvis.tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Create a simple console app to run database setup
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLogging();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Setting up test database...");
    
    // Use the connection string that matches our Docker container with supabase_admin
    var connectionString = "Host=localhost;Port=5432;Database=jarvis_test;Username=supabase_admin;Password=postgres";
    
    await TestDatabaseSetup.SetupAsync(connectionString);
    
    logger.LogInformation("Database setup completed successfully!");
    
    // Verify setup
    var isSetup = await TestDatabaseSetup.IsDatabaseSetupAsync(connectionString);
    logger.LogInformation("Database verification: {Status}", isSetup ? "PASSED" : "FAILED");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error setting up database: {Message}", ex.Message);
    logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
    Environment.Exit(1);
} 