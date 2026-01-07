using core.jarvis;
using core.jarvis.api.Handlers;
using core.jarvis.api.Systems;
using core.jarvis.api.Models;
using core.jarvis.api.Security;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Extensions;

/// <summary>
/// Extension methods for service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Jarvis API services to the service collection.
    /// </summary>
    public static IServiceCollection AddJarvisApiServices(this IServiceCollection services)
    {
        // Get configuration - check if it's already registered
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetService<IConfiguration>();
        
        if (configuration == null)
        {
            throw new InvalidOperationException("IConfiguration must be registered before calling AddJarvisApiServices");
        }

        // Set the connection string as environment variable for core Jarvis services
        var connectionString = configuration.GetConnectionString("JarvisDb");
        var isTestEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Test";

        // Validate database connection string (use test default in Test environment)
        if (string.IsNullOrEmpty(connectionString))
        {
            if (isTestEnvironment)
            {
                // Use test database connection string from TestDatabaseSetup
                connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
                    ?? "Host=localhost;Port=5432;Database=jarvis_test;Username=postgres;Password=postgres";
            }
            else
            {
                throw new InvalidOperationException(
                    "Database connection string not configured. Please set ConnectionStrings__JarvisDb in .env.local or environment variables.");
            }
        }
        
        if (connectionString.Contains("Password=CHANGE_ME"))
        {
            throw new InvalidOperationException(
                "Database password not properly configured. Please update ConnectionStrings__JarvisDb with a secure password.");
        }
        
        // Allow "postgres" password only in test environment
        if (connectionString.Contains("Password=postgres") && 
            !connectionString.Contains("jarvis_test") && 
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Test")
        {
            throw new InvalidOperationException(
                "Database password 'postgres' is not allowed in production. Please update ConnectionStrings__JarvisDb with a secure password.");
        }
        
        Environment.SetEnvironmentVariable("TEST_DATABASE_URL", connectionString);

        // Add core Jarvis services
        services.RegisterJarvis();

        // Auto-register all handlers and query handlers from this assembly
        // This replaces manual registration of 11 handlers (22 lines) with assembly scanning
        services.RegisterAllComponentHandlersAndQueriesFromAssembly(typeof(AuthHandler).Assembly);

        // Add System services
        services.AddScoped<Systems.RegistrationSystem>();
        services.AddScoped<Systems.AuthSystem>();

        // Add authentication services
        services.AddSingleton<ITokenService>(provider =>
        {
            var issuer = configuration["Jwt:Issuer"] ?? "jarvis-api";
            var audience = configuration["Jwt:Audience"] ?? "jarvis-clients";
            var secretKey = configuration["Jwt:SecretKey"];
            
            // Validate JWT secret key
            if (string.IsNullOrEmpty(secretKey) || secretKey.Contains("CHANGE_ME"))
            {
                throw new InvalidOperationException(
                    "JWT secret key not properly configured. Please set Jwt__SecretKey in .env.local or environment variables.");
            }
            
            // Only validate against DEVELOPMENT_SECRET_KEY in non-test environments
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Test" && 
                secretKey.Contains("DEVELOPMENT_SECRET_KEY"))
            {
                throw new InvalidOperationException(
                    "Development secret key is not allowed in production. Please set Jwt__SecretKey to a secure value.");
            }
            
            var expirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");

            return new TokenService(issuer, audience, secretKey, expirationMinutes);
        });

        // AuthHandler will get refresh token days from configuration directly

        // Add security services
        services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IConstantTimeService, ConstantTimeService>();
        
        // Add permission service with caching
        services.AddMemoryCache();
        services.AddScoped<IPermissionService, PermissionService>();

        // Add GraphQL query validator with configurable options
        services.AddSingleton<IGraphQLQueryValidator>(provider =>
        {
            var options = new GraphQLValidationOptions
            {
                MaxQueryDepth = int.Parse(configuration["GraphQL:MaxQueryDepth"] ?? "10"),
                MaxFieldCount = int.Parse(configuration["GraphQL:MaxFieldCount"] ?? "100"),
                MaxQueryLength = int.Parse(configuration["GraphQL:MaxQueryLength"] ?? "10000"),
                BlockIntrospection = bool.Parse(configuration["GraphQL:BlockIntrospection"] ?? "true")
            };
            return new GraphQLQueryValidator(options);
        });

        return services;
    }
}