using System.Collections.Concurrent;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace core.jarvis.api.tests.Helpers;

/// <summary>
/// Base class for API integration tests that use real database connections.
/// Extends the main IntegrationTestBase to provide API-specific services.
/// </summary>
public abstract class ApiIntegrationTestBase : IntegrationTestBase
{
    private IAuthenticationService? _authenticationService;
    private ITokenService? _tokenService;
    private IConfiguration? _configuration;
    
    protected IAuthenticationService AuthenticationService => _authenticationService ?? throw new InvalidOperationException("AuthenticationService not initialized");
    protected ITokenService TokenService => _tokenService ?? throw new InvalidOperationException("TokenService not initialized");
    protected IConfiguration Configuration => _configuration ?? throw new InvalidOperationException("Configuration not initialized");
    
    public new async Task InitializeAsync()
    {
        // First call base initialization
        await base.InitializeAsync();
        
        // Build configuration for API services
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:JarvisDb"] = TestDatabaseSetup.GetConnectionString(),
                ["Jwt:Issuer"] = "jarvis-api-test",
                ["Jwt:Audience"] = "jarvis-test-clients",
                ["Jwt:SecretKey"] = "TEST_SECRET_KEY_FOR_TESTING_ONLY_MINIMUM_256_BITS_LONG_TO_MEET_REQUIREMENTS",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "30"
            });
        
        _configuration = configBuilder.Build();
        
        // Create token service
        var issuer = _configuration["Jwt:Issuer"] ?? "jarvis-api-test";
        var audience = _configuration["Jwt:Audience"] ?? "jarvis-test-clients";
        var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");
        var expirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
        
        _tokenService = new TokenService(issuer, audience, secretKey, expirationMinutes);
        
        // Create authentication service
        var dataContext = _serviceProvider.GetRequiredService<IDataContext>();
        var logger = _serviceProvider.GetRequiredService<ILogger<AuthenticationService>>();
        
        // Create Npgsql connection for PgClient
        var connectionString = _configuration.GetConnectionString("JarvisDb") 
            ?? throw new InvalidOperationException("Connection string not configured");
        var connection = new NpgsqlConnection(connectionString);
        
        var refreshTokenDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");
        
        _authenticationService = new AuthenticationService(dataContext, _tokenService, connection, logger, refreshTokenDays);
    }
    
    /// <summary>
    /// Helper method to create test authentication request
    /// </summary>
    protected static core.jarvis.api.Models.AuthRequest CreateTestAuthRequest(string email = "test@example.com", string password = "test123")
    {
        return new core.jarvis.api.Models.AuthRequest
        {
            Email = email,
            Password = password,
            ClientId = "test-client"
        };
    }
    
    /// <summary>
    /// Helper method to create test refresh token request
    /// </summary>
    protected static core.jarvis.api.Models.RefreshTokenRequest CreateTestRefreshTokenRequest(string refreshToken, string? clientId = null)
    {
        return new core.jarvis.api.Models.RefreshTokenRequest
        {
            RefreshToken = refreshToken,
            ClientId = clientId
        };
    }
    
    public new async Task DisposeAsync()
    {
        // Clean up SecurityTokens for tracked entities
        foreach (var entityId in _testEntities.Keys)
        {
            try
            {
                await TestDataContext().Remove<SecurityToken>(entityId);
            }
            catch
            {
                // Ignore errors for entities that might not exist
            }
        }
        
        // The base class handles cleanup of all other tracked entities
        await base.DisposeAsync();
    }
    
    // Access to test entities from base class
    private ConcurrentDictionary<Guid, byte> _testEntities => 
        (ConcurrentDictionary<Guid, byte>)typeof(IntegrationTestBase)
            .GetField("_testEntities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(this)!;
}