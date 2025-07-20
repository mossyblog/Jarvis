using core.jarvis;
using core.jarvis.api.Handlers;
using core.jarvis.api.Middleware;
using core.jarvis.api.Models;
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
        
        // Validate database connection string
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string not configured. Please set ConnectionStrings__JarvisDb in .env.local or environment variables.");
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

        // Register query handlers for API-specific components
        services.AddScoped<IComponentQueryHandler<SecurityProfile>, ComponentQueryHandler<SecurityProfile>>();
        services.AddScoped<IComponentQueryHandler<Account>, ComponentQueryHandler<Account>>();
        services.AddScoped<IComponentQueryHandler<AuthToken>, ComponentQueryHandler<AuthToken>>();
        services.AddScoped<IComponentQueryHandler<Role>, ComponentQueryHandler<Role>>();
        services.AddScoped<IComponentQueryHandler<Permission>, ComponentQueryHandler<Permission>>();
        services.AddScoped<IComponentQueryHandler<NavigationItem>, ComponentQueryHandler<NavigationItem>>();

        // Register API-specific handlers - both as interface and concrete type
        // This allows DataContext.For<THandler> to resolve them by concrete type
        services.AddScoped<IComponentHandler, SystemSetupHandler>();
        services.AddScoped<SystemSetupHandler>();
        
        services.AddScoped<IComponentHandler, AuthHandler>();
        services.AddScoped<AuthHandler>();
        
        services.AddScoped<IComponentHandler, AuthTokenHandler>();
        services.AddScoped<AuthTokenHandler>();
        
        services.AddScoped<IComponentHandler, SecurityTokenHandler>();
        services.AddScoped<SecurityTokenHandler>();
        
        services.AddScoped<IComponentHandler, AccountHandler>();
        services.AddScoped<AccountHandler>();
        
        services.AddScoped<IComponentHandler, AccountProfileHandler>();
        services.AddScoped<AccountProfileHandler>();
        
        services.AddScoped<IComponentHandler, RoleHandler>();
        services.AddScoped<RoleHandler>();
        
        services.AddScoped<IComponentHandler, PermissionHandler>();
        services.AddScoped<PermissionHandler>();
        
        services.AddScoped<IComponentHandler, NavigationItemHandler>();
        services.AddScoped<NavigationItemHandler>();
        
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

        // Add middleware
        services.AddSingleton<ComponentValidationMiddleware>();
        services.AddSingleton<RateLimitingMiddleware>();
        services.AddSingleton<SecurityHeadersMiddleware>();
        services.AddSingleton<InputValidationMiddleware>();
        services.AddSingleton<AuthorizationMiddleware>();

        // Add security services
        services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IConstantTimeService, ConstantTimeService>();
        
        // Add permission service with caching
        services.AddMemoryCache();
        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }
}