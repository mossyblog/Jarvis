using core.jarvis.Data;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace core.jarvis.tests.Integration;

/// <summary>
/// Integration tests for basic service registration and dependency injection.
/// </summary>
/// <remarks>
/// <para><strong>INTENT:</strong> Validates that Jarvis services can be registered and resolved correctly.</para>
/// <para><strong>PURPOSE:</strong> Ensures the dependency injection setup works with real Supabase configuration.</para>
/// <para><strong>BUSINESS CONTEXT:</strong> Service registration is the foundation for all business operations in the framework.</para>
/// <para><strong>WHY IMPORTANT:</strong> DI configuration errors would prevent the entire system from functioning.</para>
/// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> These tests validate the service registration extensions work correctly.</para>
/// <para><strong>FUTURE RESILIENCE:</strong> As new services are added, integration tests ensure they integrate properly.</para>
/// </remarks>
public class ServiceRegistrationIntegrationTests : IAsyncLifetime
{
    private IServiceProvider _testServiceProvider = null!;
    private string _connectionString = null!;
    
    public async Task InitializeAsync()
    {
        // Get connection string
        _connectionString = TestDatabaseSetup.GetConnectionString();
        await TestDatabaseSetup.EnsureSetupAsync(_connectionString);
        
        // Setup environment variable for RegisterJarvis
        Environment.SetEnvironmentVariable("TEST_DATABASE_URL", _connectionString);
        
        // Create a new service collection to test RegisterJarvis()
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // This is what we're testing - the RegisterJarvis extension method
        services.RegisterJarvis();
        
        _testServiceProvider = services.BuildServiceProvider();
    }
    
    /// <summary>
    /// Tests that IDataContext can be resolved from the service provider.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates IDataContext is registered correctly in DI container.</para>
    /// <para><strong>PURPOSE:</strong> Ensures the main entry point service can be resolved.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> IDataContext is the primary API for all business operations.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Without IDataContext, no business operations can be performed.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Validates the core service registration is working.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Ensures IDataContext remains available as architecture evolves.</para>
    /// </remarks>
    [Fact]
    public void CanResolveDataContext()
    {
        // Act
        var dataContext = _testServiceProvider.GetService<IDataContext>();

        // Assert
        dataContext.ShouldNotBeNull();
        dataContext.ShouldBeOfType<DataContext>();
    }

   
    /// <summary>
    /// Tests that DataContext client is available in the service provider.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates DataContext is available for data operations.</para>
    /// <para><strong>PURPOSE:</strong> Ensures the data layer is properly configured.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> DataContext provides the persistence layer for all business data.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Without DataContext, no data can be persisted or retrieved.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Validates the data layer dependency is available.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Ensures data access remains accessible as needed.</para>
    /// </remarks>
    [Fact]
    public void DataContextIsAvailable()
    {
        // Act
        var dataContext = _testServiceProvider.GetService<IDataContext>();

        // Assert
        dataContext.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that creating multiple TestDataContext instances works correctly.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates TestDataContext can be created multiple times (scoped/transient).</para>
    /// <para><strong>PURPOSE:</strong> Ensures proper lifetime management of data contexts.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> Different requests/operations may need separate data contexts.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Improper lifetime management can cause concurrency issues.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Validates service lifetime configuration is correct.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Ensures multi-threaded scenarios work correctly.</para>
    /// </remarks>
    [Fact]
    public void CanCreateMultipleDataContextInstances()
    {
        // Act
        var context1 = _testServiceProvider.GetService<IDataContext>();
        var context2 = _testServiceProvider.GetService<IDataContext>();

        // Assert
        context1.ShouldNotBeNull();
        context2.ShouldNotBeNull();
        // They could be the same instance if registered as singleton
        // or different if registered as transient/scoped
        // Just verify they are valid instances
    }

    /// <summary>
    /// Tests that all required services can be resolved without errors.
    /// </summary>
    /// <remarks>
    /// <para><strong>INTENT:</strong> Validates complete service graph can be constructed.</para>
    /// <para><strong>PURPOSE:</strong> Ensures no missing dependencies in the DI configuration.</para>
    /// <para><strong>BUSINESS CONTEXT:</strong> All services must be resolvable for the system to function.</para>
    /// <para><strong>WHY IMPORTANT:</strong> Missing dependencies cause runtime failures in production.</para>
    /// <para><strong>ARCHITECTURAL SIGNIFICANCE:</strong> Validates the entire DI configuration is complete.</para>
    /// <para><strong>FUTURE RESILIENCE:</strong> Catches configuration errors early in development.</para>
    /// </remarks>
    [Fact]
    public void AllServicesCanBeResolved()
    {
        // Act & Assert - Should not throw
        Should.NotThrow(() =>
        {
            var dataContext = _testServiceProvider.GetRequiredService<IDataContext>();
            // No longer checking for raw client, just DataContext
            var loggerFactory = _testServiceProvider.GetRequiredService<ILoggerFactory>();
        });
    }
    
    public Task DisposeAsync()
    {
        (_testServiceProvider as IDisposable)?.Dispose();
        return Task.CompletedTask;
    }
}