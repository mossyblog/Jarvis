using System.Collections.Concurrent;
using core.jarvis.Data;
using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace core.jarvis.tests.Integration;

/// <summary>
/// Integration tests for connection pooling functionality.
/// Validates concurrent operations and pool behavior.
/// </summary>
public class ConnectionPoolingTests : IntegrationTestBase
{
    private ServiceProvider? _pooledServiceProvider;
    
    /// <summary>
    /// INTENT: Verify that connection factory supports concurrent connection acquisition
    /// PURPOSE: Ensure multiple connections can be obtained simultaneously
    /// BUSINESS CONTEXT: Support high-throughput scenarios with multiple concurrent operations
    /// WHY IMPORTANT: Validates that pooling actually enables concurrency
    /// ARCHITECTURAL SIGNIFICANCE: Confirms connection factory thread safety
    /// FUTURE RESILIENCE: Ensures system can scale with concurrent load
    /// </summary>
    [Fact]
    public async Task ConnectionFactory_Should_Support_Concurrent_Operations()
    {
        // Arrange
        var connectionString = TestDatabaseSetup.GetConnectionString();
        var factory = new NpgsqlConnectionFactory(
            connectionString,
            _serviceProvider.GetRequiredService<ILogger<NpgsqlConnectionFactory>>(),
            maxPoolSize: 10,
            minPoolSize: 2);
        
        var concurrentTasks = 10;
        var results = new ConcurrentBag<bool>();
        
        // Act
        var tasks = Enumerable.Range(0, concurrentTasks).Select(async i =>
        {
            try
            {
                var connection = await factory.GetConnectionAsync();
                try
                {
                    // Simulate some work
                    await Task.Delay(100);
                    results.Add(true);
                }
                finally
                {
                    await factory.ReturnConnectionAsync(connection);
                }
            }
            catch (Exception ex)
            {
                Logger().LogError(ex, "Task {TaskId} failed", i);
                results.Add(false);
            }
        });
        
        await Task.WhenAll(tasks);
        
        // Assert
        results.Count.ShouldBe(concurrentTasks);
        results.All(r => r).ShouldBeTrue();
        
        // Check pool statistics
        var stats = factory.GetPoolStatistics();
        stats.MaxPoolSize.ShouldBe(10);
        stats.MinPoolSize.ShouldBe(2);
        
        // Cleanup
        factory.Dispose();
    }
    
    /// <summary>
    /// INTENT: Verify that pooled PgClient supports concurrent handler operations
    /// PURPOSE: Ensure handlers can execute concurrently with pooled connections
    /// BUSINESS CONTEXT: Enable parallel processing of business operations
    /// WHY IMPORTANT: Validates end-to-end concurrent operation support
    /// ARCHITECTURAL SIGNIFICANCE: Confirms handler pattern works with pooling
    /// FUTURE RESILIENCE: Ensures business logic can scale horizontally
    /// </summary>
    [Fact]
    public async Task PgClientPooled_Should_Support_Concurrent_Handler_Operations()
    {
        // Arrange - Create a service provider with pooling enabled
        var services = new ServiceCollection();
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Jarvis:Database:ConnectionPooling:Enabled", "true"),
                new KeyValuePair<string, string?>("Jarvis:Database:ConnectionPooling:MaxPoolSize", "20"),
                new KeyValuePair<string, string?>("Jarvis:Database:ConnectionPooling:MinPoolSize", "5")
            })
            .Build();
        
        services.RegisterJarvis(LogLevel.Warning, config);
        services.RegisterAllComponentHandlersAndQueriesFromAssembly(typeof(TestHandler).Assembly);
        
        Environment.SetEnvironmentVariable("TEST_DATABASE_URL", TestDatabaseSetup.GetConnectionString());
        
        _pooledServiceProvider = services.BuildServiceProvider();
        
        // Create test entities concurrently
        var entityIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        
        // Act - Create components concurrently
        using (var scope = _pooledServiceProvider.CreateScope())
        {
            var dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
            
            var createTasks = entityIds.Select(async entityId =>
            {
                TrackEntity(entityId);
                var handler = dataContext.For<TestHandler>(entityId);
                var component = new TestComponent { OwnerEntityId = entityId, Status = "Active" };
                await dataContext.Commit(component);
                return entityId;
            });
            
            var createdIds = await Task.WhenAll(createTasks);
            
            // Assert - Verify all were created
            createdIds.Length.ShouldBe(5);
            
            // Verify by reading them back concurrently
            var readTasks = createdIds.Select(async entityId =>
            {
                var handler = dataContext.For<TestHandler>(entityId);
                var component = await handler.Get();
                return component;
            });
            
            var components = await Task.WhenAll(readTasks);
            components.Length.ShouldBe(5);
            components.All(c => c != null).ShouldBeTrue();
        }
    }
    
    /// <summary>
    /// INTENT: Verify that connection pool respects maximum pool size
    /// PURPOSE: Ensure pool doesn't exceed configured limits
    /// BUSINESS CONTEXT: Prevent database connection exhaustion
    /// WHY IMPORTANT: Validates resource limits are enforced
    /// ARCHITECTURAL SIGNIFICANCE: Confirms pool configuration is respected
    /// FUTURE RESILIENCE: Prevents runaway connection growth
    /// </summary>
    [Fact]
    public async Task ConnectionPool_Should_Respect_MaxPoolSize()
    {
        // Arrange
        var maxPoolSize = 3;
        var factory = new NpgsqlConnectionFactory(
            TestDatabaseSetup.GetConnectionString(),
            _serviceProvider.GetRequiredService<ILogger<NpgsqlConnectionFactory>>(),
            maxPoolSize: maxPoolSize,
            minPoolSize: 1);
        
        var connections = new List<Npgsql.NpgsqlConnection>();
        
        try
        {
            // Act - Acquire connections up to the limit
            for (int i = 0; i < maxPoolSize; i++)
            {
                var conn = await factory.GetConnectionAsync();
                connections.Add(conn);
            }
            
            // Try to acquire one more - should timeout or throw
            var acquireTask = factory.GetConnectionAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));
            
            var completedTask = await Task.WhenAny(acquireTask, timeoutTask);
            
            // Assert
            if (completedTask == acquireTask)
            {
                // If it completed, it should have thrown
                await Should.ThrowAsync<InvalidOperationException>(async () => await acquireTask);
            }
            else
            {
                // It timed out waiting for a connection
                completedTask.ShouldBe(timeoutTask);
            }
            
            var stats = factory.GetPoolStatistics();
            stats.ActiveConnections.ShouldBe(maxPoolSize);
        }
        finally
        {
            // Cleanup - return all connections
            foreach (var conn in connections)
            {
                await factory.ReturnConnectionAsync(conn);
            }
            factory.Dispose();
        }
    }
    
    /// <summary>
    /// INTENT: Verify that disposed connections are properly returned to pool
    /// PURPOSE: Ensure connections are recycled correctly
    /// BUSINESS CONTEXT: Maximize connection reuse efficiency
    /// WHY IMPORTANT: Validates pool recycling behavior
    /// ARCHITECTURAL SIGNIFICANCE: Confirms proper resource lifecycle
    /// FUTURE RESILIENCE: Ensures sustainable connection management
    /// </summary>
    [Fact]
    public async Task ConnectionPool_Should_Recycle_Connections()
    {
        // Arrange
        var factory = new NpgsqlConnectionFactory(
            TestDatabaseSetup.GetConnectionString(),
            _serviceProvider.GetRequiredService<ILogger<NpgsqlConnectionFactory>>(),
            maxPoolSize: 2,
            minPoolSize: 1);
        
        // Act - Acquire and return a connection multiple times
        for (int i = 0; i < 5; i++)
        {
            var connection = await factory.GetConnectionAsync();
            connection.ShouldNotBeNull();
            
            // Do some work
            await Task.Delay(50);
            
            // Return to pool
            await factory.ReturnConnectionAsync(connection);
        }
        
        // Assert - Should have succeeded without exhausting pool
        var stats = factory.GetPoolStatistics();
        stats.ActiveConnections.ShouldBe(0); // All returned
        stats.MaxPoolSize.ShouldBe(2);
        
        // Cleanup
        factory.Dispose();
    }
    
    public new async Task DisposeAsync()
    {
        _pooledServiceProvider?.Dispose();
        await base.DisposeAsync();
    }
}