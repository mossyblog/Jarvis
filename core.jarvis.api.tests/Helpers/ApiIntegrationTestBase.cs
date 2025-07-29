using System.Collections.Concurrent;
using core.jarvis.api.Extensions;
using core.jarvis.api.Functions.Security;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.tests.Helpers;
using core.jarvis.tests.Fixtures.Handlers;
using Microsoft.Azure.Functions.Worker;
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
    private ITokenService? _tokenService;
    private IConfiguration? _configuration;
    private ServiceProvider? _apiServiceProvider;
    private bool _apiServicesInitialized = false;
    
    protected ITokenService TokenService
    {
        get
        {
            EnsureApiServicesInitialized();
            return _tokenService ?? throw new InvalidOperationException("TokenService not initialized");
        }
    }
    
    protected IConfiguration Configuration
    {
        get
        {
            EnsureApiServicesInitialized();
            return _configuration ?? throw new InvalidOperationException("Configuration not initialized");
        }
    }
    
    // Override the base _serviceProvider to return our API-configured one
    protected new ServiceProvider _serviceProvider
    {
        get
        {
            EnsureApiServicesInitialized();
            return _apiServiceProvider ?? throw new InvalidOperationException("API service provider not initialized");
        }
    }
    
    // Override TestDataContext to use our API-configured service provider
    protected new IDataContext TestDataContext()
    {
        EnsureApiServicesInitialized();
        return _apiServiceProvider!.GetRequiredService<IDataContext>();
    }
    
    private void EnsureApiServicesInitialized()
    {
        if (!_apiServicesInitialized)
        {
            InitializeApiServices();
            _apiServicesInitialized = true;
        }
    }
    
    private void InitializeApiServices()
    {
        // Get the test connection string
        var testConnectionString = TestDatabaseSetup.GetConnectionString();
        
        // Build configuration for API services
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:JarvisDb"] = testConnectionString,
                ["Jwt:Issuer"] = "jarvis-api-test",
                ["Jwt:Audience"] = "jarvis-test-clients",
                ["Jwt:SecretKey"] = "TEST_JARVIS_KEY_FOR_UNIT_TESTING_PURPOSES_ONLY_MINIMUM_256_BITS_LONG_TO_MEET_ALL_REQUIREMENTS",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "30"
            });
        
        _configuration = configBuilder.Build();
        
        // Create a new service collection with API services
        var services = new ServiceCollection();
        
        // Add configuration FIRST so AddJarvisApiServices can access it
        services.AddSingleton<IConfiguration>(_configuration);
        
        // Register API services (this will build a service provider internally to get IConfiguration)
        // This will also register all core Jarvis services including IDataContext
        services.AddJarvisApiServices();
        
        // Register Azure Functions
        services.AddScoped<AuthFunction>();
        services.AddScoped<DeauthFunction>();
        services.AddScoped<ValidateFunction>();
        services.AddScoped<RegisterFunction>();
        
        // Build the API service provider
        _apiServiceProvider = services.BuildServiceProvider();
        
        // Store token service reference for convenience
        _tokenService = _apiServiceProvider.GetRequiredService<ITokenService>();
    }
    
    /// <summary>
    /// Helper method to create test authentication request
    /// </summary>
    protected static core.jarvis.api.Models.Account CreateTestAuthRequest(string email = "test@example.com", string password = "test123")
    {
        return new core.jarvis.api.Models.Account
        {
            Email = email,
            Password = password,
            ClientId = "test-client"
        };
    }
    
    /// <summary>
    /// Helper method to create test refresh token request
    /// </summary>
    protected static core.jarvis.api.Models.AuthToken CreateTestRefreshTokenRequest(string refreshToken, string? clientId = null)
    {
        return new core.jarvis.api.Models.AuthToken
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
                await TestDataContext().Remove<AuthToken>(entityId);
            }
            catch
            {
                // Ignore errors for entities that might not exist
            }
        }
        
        // Dispose API service provider
        _apiServiceProvider?.Dispose();
        
        // The base class handles cleanup of all other tracked entities
        await base.DisposeAsync();
    }
    
    // Access to test entities from base class
    private ConcurrentDictionary<Guid, byte> _testEntities => 
        (ConcurrentDictionary<Guid, byte>)typeof(IntegrationTestBase)
            .GetField("_testEntities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(this)!;
}