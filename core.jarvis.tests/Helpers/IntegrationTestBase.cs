using System.Collections.Concurrent;
using core.jarvis.Data;
using core.jarvis.Data.Components;
using core.jarvis.Data.Query;
using core.jarvis.Logging;
using core.jarvis.tests.Components;
using core.jarvis.tests.Examples;
using core.jarvis.tests.Examples.Blog;
using core.jarvis.tests.Fixtures.Components;
using core.jarvis.tests.Fixtures.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog;
using Serilog.Events;

namespace core.jarvis.tests.Helpers;

/// <summary>
/// Universal base class for all integration tests.
/// Centralizes DI, TestDataContext, PgClient, Registry, Logger, and test environment setup.
/// Inherit from this in all integration test classes to avoid duplication.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected ServiceProvider _serviceProvider;
    private ILogger<TestHandler> _logger;
    private IDataContext _dataContext;
    private readonly ConcurrentDictionary<Guid, byte> _testEntities = new();
    private string _connectionString;

    protected ILogger<TestHandler> Logger() => _logger;
    protected IDataContext TestDataContext() => _dataContext;

    protected void TrackEntity(Guid entityId)
    {
        _testEntities.TryAdd(entityId, 0);
    }

    public async Task InitializeAsync()
    {
        // Setup test database
        _connectionString = TestDatabaseSetup.GetConnectionString();
        await TestDatabaseSetup.EnsureSetupAsync(_connectionString);
        
        var services = new ServiceCollection();
        services.AddSingleton<IComponentQueryHandlerRegistry, ComponentQueryHandlerRegistry>();
        
        // Configure Serilog for tests - minimal output unless debugging
        services.AddJarvisSerilog(config =>
        {
            config.MinimumLevel.Is(LogEventLevel.Warning)
                .MinimumLevel.Override("core.jarvis", LogEventLevel.Debug)
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: "[{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");
        });
        
        // Register PgClient as scoped to reuse connections per test
        services.AddScoped<IPgClient>(sp =>
        {
            var connection = new NpgsqlConnection(_connectionString);
            var pgClientWrapper = new PgClientWrapper(connection);
            
            // Authenticate with test user
            var jwt = pgClientWrapper.Client.Authenticate("test@example.com", "test123").GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(jwt))
            {
                pgClientWrapper.SetJwt(jwt);
            }
            
            return pgClientWrapper;
        });
        
        services.AddTransient<IDataContext, DataContext>();
        services.AddScoped<EventSubscriptionManager>();
        services.AddScoped<IAuditService, AuditService>();

        // Register the DataContext with the service provider
        services.AddTransient<TestHandler>();
        services.AddTransient<PositionTestHandler>();
        services.AddTransient<VelocityTestHandler>();

        // Register all handlers and query handlers from test assembly
        services.RegisterAllComponentHandlersAndQueriesFromAssembly(typeof(TestHandler).Assembly);
        
        // Also register handlers from main assembly
        services.RegisterAllComponentHandlersAndQueriesFromAssembly(typeof(DataContext).Assembly);
        
        // Register blog example handlers explicitly since Examples namespace may be excluded
        services.AddTransient<BlogComponentHandler>();
        services.AddTransient<BlogPostComponentHandler>();
        services.AddTransient<BlogHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<TestHandler>>();
        
        // Pass in the service provider to the DataContext
        _dataContext = _serviceProvider.GetRequiredService<IDataContext>();
    }

    public async Task DisposeAsync()
    {
        // Cleanup all tracked entities for this test
        foreach (var entityId in _testEntities.Keys)
        {
            try
            {
                await TestDataContext().Remove<TestComponent>(entityId);
                await TestDataContext().Remove<PositionComponent>(entityId);
                await TestDataContext().Remove<VelocityComponent>(entityId);
                await TestDataContext().Remove<BlogComponent>(entityId);
                await TestDataContext().Remove<BlogPostComponent>(entityId);
                await TestDataContext().Remove<OrderComponent>(entityId);
                await TestDataContext().Remove<InvoiceTestComponent>(entityId);
                await TestDataContext().Remove<PaymentTestComponent>(entityId);
                await TestDataContext().Remove<WorkOrderTestComponent>(entityId);
                await TestDataContext().Remove<AuditEvent>(entityId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up entity {EntityId}", entityId);
            }
        }
        
        _serviceProvider?.Dispose();
    }
}