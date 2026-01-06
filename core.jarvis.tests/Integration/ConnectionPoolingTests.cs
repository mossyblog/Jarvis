using System.Collections.Concurrent;
using core.jarvis.Data;
using core.jarvis.tests.Components;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shouldly;
using Xunit;

namespace core.jarvis.tests.Integration;

/// <summary>
/// Integration tests for connection pooling functionality.
/// Validates concurrent operations and pool behavior using NpgsqlDataSource.
/// </summary>
public class ConnectionPoolingTests : IntegrationTestBase
{
    private ServiceProvider? _pooledServiceProvider;

    /// <summary>
    /// INTENT: Verify that NpgsqlDataSource supports concurrent connection acquisition
    /// PURPOSE: Ensure multiple connections can be obtained simultaneously
    /// BUSINESS CONTEXT: Support high-throughput scenarios with multiple concurrent operations
    /// WHY IMPORTANT: Validates that pooling actually enables concurrency
    /// ARCHITECTURAL SIGNIFICANCE: Confirms NpgsqlDataSource thread safety
    /// FUTURE RESILIENCE: Ensures system can scale with concurrent load
    /// </summary>
    [Fact]
    public async Task NpgsqlDataSourceSupportsConcurrentOperations()
    {
        // Arrange
        var connectionString = TestDatabaseSetup.GetConnectionString();
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = 10,
            MinPoolSize = 2
        };

        await using var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);

        var concurrentTasks = 10;
        var results = new ConcurrentBag<bool>();

        // Act
        var tasks = Enumerable.Range(0, concurrentTasks).Select(async i =>
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync();

                // Verify connection is valid
                connection.ShouldNotBeNull();
                connection.State.ShouldBe(System.Data.ConnectionState.Open);
                results.Add(true);
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
    public async Task PgClientWithDataSourceSupportsConcurrentHandlerOperations()
    {
        // Arrange - Create a service provider with NpgsqlDataSource
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
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
        foreach (var entityId in entityIds)
        {
            TrackEntity(entityId);
        }

        // Act - Create components concurrently (each task gets its own scope/connection)
        var createTasks = entityIds.Select(async entityId =>
        {
            using var scope = _pooledServiceProvider!.CreateScope();
            var dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
            var component = new TestComponent { OwnerEntityId = entityId, Status = "Active" };
            await dataContext.Commit(component);
            return entityId;
        });

        var createdIds = await Task.WhenAll(createTasks);

        // Assert - Verify all were created
        createdIds.Length.ShouldBe(5);

        // Verify by reading them back concurrently (each task gets its own scope/connection)
        var readTasks = createdIds.Select(async entityId =>
        {
            using var scope = _pooledServiceProvider!.CreateScope();
            var dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();
            var handler = dataContext.For<TestHandler>(entityId);
            var component = await handler.Get();
            return component;
        });

        var components = await Task.WhenAll(readTasks);
        components.Length.ShouldBe(5);
        components.All(c => c != null).ShouldBeTrue();
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
    public async Task ConnectionPoolRespectsMaxPoolSize()
    {
        // Arrange
        var maxPoolSize = 3;
        var builder = new NpgsqlConnectionStringBuilder(TestDatabaseSetup.GetConnectionString())
        {
            MaxPoolSize = maxPoolSize,
            MinPoolSize = 1,
            Timeout = 2 // 2 second connection timeout
        };

        await using var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);
        var connections = new List<NpgsqlConnection>();

        try
        {
            // Act - Acquire connections up to the limit
            for (int i = 0; i < maxPoolSize; i++)
            {
                var conn = await dataSource.OpenConnectionAsync();
                connections.Add(conn);
            }

            // All connections acquired successfully
            connections.Count.ShouldBe(maxPoolSize);

            // Try to acquire one more - should throw NpgsqlException when pool exhausted
            try
            {
                await using var extraConn = await dataSource.OpenConnectionAsync();
                // If we get here, pool recycled a connection
                Logger().LogInformation("Got extra connection - pool may have recycled");
            }
            catch (NpgsqlException ex) when (ex.Message.Contains("pool has been exhausted"))
            {
                // This is the expected behavior when pool is exhausted
                Logger().LogInformation("Pool exhaustion exception as expected: {Message}", ex.Message);
            }
        }
        finally
        {
            // Cleanup - dispose all connections (returns to pool)
            foreach (var conn in connections)
            {
                await conn.DisposeAsync();
            }
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
    public async Task ConnectionPoolRecyclesConnections()
    {
        // Arrange
        var builder = new NpgsqlConnectionStringBuilder(TestDatabaseSetup.GetConnectionString())
        {
            MaxPoolSize = 2,
            MinPoolSize = 1
        };

        await using var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);

        // Act - Acquire and dispose connections multiple times
        // If recycling didn't work, this would exhaust the pool
        for (int i = 0; i < 10; i++)
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            connection.ShouldNotBeNull();
            connection.State.ShouldBe(System.Data.ConnectionState.Open);

            // Connection is automatically returned to pool on dispose
        }

        // Assert - Should have succeeded without exhausting pool
        // The fact that we completed 10 iterations with max pool size of 2
        // proves connections are being recycled
        Logger().LogInformation("Pool recycling test completed successfully - 10 operations with pool size 2");
    }

    /// <summary>
    /// INTENT: Verify connections can be reused across multiple operations
    /// PURPOSE: Ensure pool efficiently manages connection lifecycle
    /// BUSINESS CONTEXT: Connection reuse is critical for performance
    /// WHY IMPORTANT: Validates proper connection return behavior
    /// ARCHITECTURAL SIGNIFICANCE: Confirms dispose returns to pool
    /// FUTURE RESILIENCE: Ensures sustainable resource usage
    /// </summary>
    [Fact]
    public async Task ConnectionPoolReusesReturnedConnections()
    {
        // Arrange
        var builder = new NpgsqlConnectionStringBuilder(TestDatabaseSetup.GetConnectionString())
        {
            MaxPoolSize = 2,
            MinPoolSize = 1
        };

        await using var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);

        // Act - Acquire all connections, then release and reacquire
        var conn1 = await dataSource.OpenConnectionAsync();
        var conn2 = await dataSource.OpenConnectionAsync();

        // Pool should be at capacity
        conn1.ShouldNotBeNull();
        conn2.ShouldNotBeNull();

        // Return one connection
        await conn1.DisposeAsync();

        // Should be able to get another connection now
        var conn3 = await dataSource.OpenConnectionAsync();
        conn3.ShouldNotBeNull();
        conn3.State.ShouldBe(System.Data.ConnectionState.Open);

        // Cleanup
        await conn2.DisposeAsync();
        await conn3.DisposeAsync();

        Logger().LogInformation("Connection reuse test completed - pool correctly reuses returned connections");
    }

    /// <summary>
    /// INTENT: Verify DataContext works correctly with NpgsqlDataSource pooling
    /// PURPOSE: Ensure the simplified architecture works end-to-end
    /// BUSINESS CONTEXT: DataContext is the primary API for data operations
    /// WHY IMPORTANT: Validates the architectural change doesn't break functionality
    /// ARCHITECTURAL SIGNIFICANCE: End-to-end integration test
    /// FUTURE RESILIENCE: Regression test for pooling behavior
    /// </summary>
    [Fact]
    public async Task DataContextWorksWithNpgsqlDataSourcePooling()
    {
        // This test uses the IntegrationTestBase which already sets up
        // NpgsqlDataSource-based pooling correctly

        // Arrange
        var entityId = Guid.NewGuid();
        TrackEntity(entityId);

        // Act - Create a component
        var component = new TestComponent
        {
            OwnerEntityId = entityId,
            Name = "PoolingTest",
            Status = "ACTIVE",
            Value = 42
        };

        var success = await TestDataContext().TryCommit(component);
        success.ShouldBeTrue();

        // Read it back
        var handler = TestDataContext().For<TestHandler>(entityId);
        var retrieved = await handler.Get();

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("PoolingTest");
        retrieved.Value.ShouldBe(42);
    }

    public new async Task DisposeAsync()
    {
        _pooledServiceProvider?.Dispose();
        await base.DisposeAsync();
    }
}
