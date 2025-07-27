using System.Linq.Expressions;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace core.jarvis.tests;

public class ComponentQueryHandlerTests : IAsyncLifetime
{
    public class TestComponent : IComponent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid OwnerEntityId { get; set; } = Guid.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string Name { get; init; } = string.Empty;
        public int Status { get; init; } = 0;
    }

    private string _connectionString;
    private ServiceProvider _serviceProvider;

    public async Task InitializeAsync()
    {
        // Setup test database
        _connectionString = TestDatabaseSetup.GetConnectionString();
        await TestDatabaseSetup.EnsureSetupAsync(_connectionString);
    }

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ApplyExpressionFilter_WithEqualsExpression_ShouldAddFilter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Configure NpgsqlDataSource for connection pooling
        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var pooledConnectionString = $"{_connectionString};Pooling=true;Maximum Pool Size=50;Minimum Pool Size=5";
            return NpgsqlDataSource.Create(pooledConnectionString);
        });
        
        // Register PgClient using pooled connections
        services.AddScoped<IPgClient>(sp =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            var connection = dataSource.CreateConnection();
            var logger = sp.GetService<ILogger<PgClientWrapper>>();
            return new PgClientWrapper(connection, ownsConnection: true, logger: logger);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        _serviceProvider = serviceProvider;
        
        var pgClient = serviceProvider.GetRequiredService<IPgClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<ComponentQueryHandler<TestComponent>>>();
        var handler = new ComponentQueryHandler<TestComponent>(pgClient, logger);
        
        var testEntityId = Guid.NewGuid();
        Expression<Func<TestComponent, bool>> filter = c => c.OwnerEntityId == testEntityId;
        
        // Act & Assert - This should not throw an exception
        var result = await handler.QueryEntityIds(filter);
        
        // If we get here without exception, the expression parsing worked
        Assert.NotNull(result);
    }
    
    [Fact]  
    public async Task ApplyExpressionFilter_WithStringContains_ShouldAddLikeFilter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Configure NpgsqlDataSource for connection pooling
        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var pooledConnectionString = $"{_connectionString};Pooling=true;Maximum Pool Size=50;Minimum Pool Size=5";
            return NpgsqlDataSource.Create(pooledConnectionString);
        });
        
        // Register PgClient using pooled connections
        services.AddScoped<IPgClient>(sp =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            var connection = dataSource.CreateConnection();
            var logger = sp.GetService<ILogger<PgClientWrapper>>();
            return new PgClientWrapper(connection, ownsConnection: true, logger: logger);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        _serviceProvider = serviceProvider;
        
        var pgClient = serviceProvider.GetRequiredService<IPgClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<ComponentQueryHandler<TestComponent>>>();
        var handler = new ComponentQueryHandler<TestComponent>(pgClient, logger);
        
        Expression<Func<TestComponent, bool>> filter = c => c.Name.Contains("Test");
        
        // Act & Assert - This should not throw an exception  
        var result = await handler.QueryEntityIds(filter);
        
        Assert.NotNull(result);
    }
}