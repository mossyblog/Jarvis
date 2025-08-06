using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Serilog;
using Serilog.Events;
using core.jarvis.Data;
using core.jarvis.Data.Components;
using core.jarvis.Data.Schema;
using core.jarvis.Data.Query;
using core.jarvis.Events;
using core.jarvis.Events.Emitters;
using core.jarvis.Logging;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;

namespace core.jarvis.Scripts;

/// <summary>
/// Simple script to activate a user account using AccountHandler.
/// This script connects to the jarvis_test database and activates the specified user.
/// </summary>
public class ActivateUserScript
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=jarvis_test;Username=supabase_admin;Password=postgres";
    private const string TargetEntityId = "021536e2-035c-450b-95e0-27732100db46";
    private const string TargetEmail = "curltest@example.com";

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== User Activation Script ===");
        Console.WriteLine($"Target Entity ID: {TargetEntityId}");
        Console.WriteLine($"Target Email: {TargetEmail}");
        Console.WriteLine($"Connection String: {ConnectionString}");
        Console.WriteLine();

        try
        {
            // Setup dependency injection container
            var services = new ServiceCollection();
            ConfigureServices(services);
            
            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            
            var dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ActivateUserScript>>();
            
            // Check if user exists before activation
            var entityGuid = Guid.Parse(TargetEntityId);
            var accountHandler = dataContext.For<AccountHandler>(entityGuid);
            
            Console.WriteLine("1. Checking if account exists...");
            var existingAccount = await accountHandler.GetOrDefault();
            
            if (existingAccount == null)
            {
                Console.WriteLine($"❌ ERROR: No account found for entity ID {TargetEntityId}");
                Console.WriteLine("Please verify the entity ID is correct.");
                return;
            }
            
            Console.WriteLine($"✅ Account found:");
            Console.WriteLine($"   - Email: {existingAccount.Email}");
            Console.WriteLine($"   - IsActive: {existingAccount.IsActive}");
            Console.WriteLine($"   - CreatedAt: {existingAccount.CreatedAt}");
            Console.WriteLine();
            
            if (existingAccount.IsActive)
            {
                Console.WriteLine("ℹ️  Account is already active. No action needed.");
                return;
            }
            
            // Activate the account
            Console.WriteLine("2. Activating account...");
            var activatedAccount = await accountHandler.Activate();
            
            Console.WriteLine($"✅ Account activated successfully!");
            Console.WriteLine($"   - Email: {activatedAccount.Email}");
            Console.WriteLine($"   - IsActive: {activatedAccount.IsActive}");
            Console.WriteLine($"   - LastUpdated: {activatedAccount.LastUpdated}");
            Console.WriteLine();
            
            // Verify activation by fetching again
            Console.WriteLine("3. Verifying activation...");
            var verifiedAccount = await accountHandler.GetOrDefault();
            
            if (verifiedAccount?.IsActive == true)
            {
                Console.WriteLine("✅ Activation verified! User is now active.");
            }
            else
            {
                Console.WriteLine("❌ ERROR: Activation verification failed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Script completed. Press any key to exit...");
        Console.ReadKey();
    }
    
    private static void ConfigureServices(ServiceCollection services)
    {
        // Configure logging
        services.AddJarvisSerilog(config =>
        {
            config.MinimumLevel.Is(LogEventLevel.Information)
                .MinimumLevel.Override("core.jarvis", LogEventLevel.Debug)
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: "[{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");
        });
        
        // Configure database connection with pooling
        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var pooledConnectionString = $"{ConnectionString};Pooling=true;Maximum Pool Size=50;Minimum Pool Size=5";
            return NpgsqlDataSource.Create(pooledConnectionString);
        });
        
        // Register PgClient
        services.AddScoped<IPgClient>(sp =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            var connection = dataSource.CreateConnection();
            var logger = sp.GetService<ILogger<PgClientWrapper>>();
            return new PgClientWrapper(connection, ownsConnection: true, logger: logger);
        });
        
        // Register core Jarvis services
        services.AddScoped<IDataContext, DataContext>();
        services.AddScoped<EventSubscriptionManager>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEntityQuery, EntityQuery>();
        services.AddScoped<ITableManager, PostgreSqlTableManager>();
        services.AddSingleton<IComponentQueryHandlerRegistry, ComponentQueryHandlerRegistry>();
        services.AddSingleton<IEventEmitter, InMemoryEventEmitter>();
        
        // Register API handlers and services
        services.AddTransient<AccountHandler>();
        services.AddTransient<IComponentHandler, AccountHandler>();
        
        // Register configuration
        services.AddSingleton<IConfiguration>(sp =>
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:SecretKey", "Script-Secret-Key-Must-Be-At-Least-256-Bits-For-Security-Testing"},
                {"Jwt:Issuer", "script-issuer"},
                {"Jwt:Audience", "script-audience"},
                {"Jwt:AccessTokenExpirationMinutes", "15"}
            });
            configBuilder.AddEnvironmentVariables();
            return configBuilder.Build();
        });
    }
}