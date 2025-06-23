using core.jarvis;
using core.jarvis.api.Handlers;
using core.jarvis.api.Middleware;
using core.jarvis.api.Services;
using core.jarvis.Data;
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
        // Get configuration
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        // Set the connection string as environment variable for core Jarvis services
        var connectionString = configuration.GetConnectionString("JarvisDb");
        if (!string.IsNullOrEmpty(connectionString))
        {
            Environment.SetEnvironmentVariable("TEST_DATABASE_URL", connectionString);
        }

        // Add core Jarvis services
        services.RegisterJarvis();

        // Register API-specific handlers
        services.AddScoped<IComponentHandler, SystemSetupHandler>();
        services.AddScoped<IComponentHandler, AuthHandler>();
        services.AddScoped<IComponentHandler, AuthTokenHandler>();
        services.AddScoped<IComponentHandler, SecurityTokenHandler>();
        services.AddScoped<IComponentHandler, UserHandler>();
        services.AddScoped<IComponentHandler, UserProfileHandler>();
        services.AddScoped<IComponentHandler, RoleHandler>();
        services.AddScoped<IComponentHandler, PermissionHandler>();
        services.AddScoped<IComponentHandler, NavigationItemHandler>();

        // Register background services
        services.AddHostedService<TokenCleanupService>();

        // Add authentication services
        services.AddSingleton<ITokenService>(provider =>
        {
            var issuer = configuration["Jwt:Issuer"] ?? "jarvis-api";
            var audience = configuration["Jwt:Audience"] ?? "jarvis-clients";
            var secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");
            var expirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");

            return new TokenService(issuer, audience, secretKey, expirationMinutes);
        });

        // AuthHandler will get refresh token days from configuration directly

        // Add middleware
        services.AddSingleton<ComponentValidationMiddleware>();
        services.AddSingleton<RateLimitingMiddleware>();
        services.AddSingleton<SecurityHeadersMiddleware>();
        services.AddSingleton<InputValidationMiddleware>();

        // Add security services
        services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();

        return services;
    }
}