using core.jarvis;
using core.jarvis.api.Middleware;
using core.jarvis.api.Services;
using core.jarvis.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

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

        // Add authentication services
        services.AddSingleton<ITokenService>(provider =>
        {
            var issuer = configuration["Jwt:Issuer"] ?? "jarvis-api";
            var audience = configuration["Jwt:Audience"] ?? "jarvis-clients";
            var secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");
            var expirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
            
            return new TokenService(issuer, audience, secretKey, expirationMinutes);
        });

        services.AddScoped<IAuthenticationService>(provider =>
        {
            var dataContext = provider.GetRequiredService<IDataContext>();
            var tokenService = provider.GetRequiredService<ITokenService>();
            var logger = provider.GetRequiredService<ILogger<AuthenticationService>>();
            
            // Create Npgsql connection for PgClient
            var connectionString = configuration.GetConnectionString("JarvisDb") 
                ?? throw new InvalidOperationException("Connection string not configured");
            var connection = new NpgsqlConnection(connectionString);
            
            var refreshTokenDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");
            
            return new AuthenticationService(dataContext, tokenService, connection, logger, refreshTokenDays);
        });

        // Add middleware
        services.AddSingleton<ComponentValidationMiddleware>();

        return services;
    }
}