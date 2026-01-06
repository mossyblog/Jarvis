using System.Collections.Concurrent;
using System.Linq;
using core.jarvis.Data;
using core.jarvis.Data.Components;
using core.jarvis.Data.Query;
using core.jarvis.Logging;
using core.jarvis.tests.Components;
using core.jarvis.tests.Examples;
using core.jarvis.tests.Examples.Blog;
using core.jarvis.tests.Fixtures.Handlers;
using core.jarvis.tests.Examples.WorkOrder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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
    private IServiceScope _scope;
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

    public virtual async Task InitializeAsync()
    {
        // Set test environment to help services detect they're running in tests
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        
        // For GraphQL tests, ensure JWT environment variables are set
        // This is needed because PgClient validates JWT signatures
        if (this.GetType().Namespace?.Contains("GraphQL") == true)
        {
            Environment.SetEnvironmentVariable("Jwt__SecretKey", "GraphQL-Test-Secret-Key-Must-Be-At-Least-256-Bits-For-Security");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "graphql-test-issuer");
            Environment.SetEnvironmentVariable("Jwt__Audience", "graphql-test-audience");
        }
        
        // Get connection string to Docker-provisioned database
        _connectionString = TestDatabaseSetup.GetConnectionString();
        
        var services = new ServiceCollection();
        services.AddScoped<IComponentQueryHandlerRegistry, ComponentQueryHandlerRegistry>();
        
        // Configure Serilog for tests - minimal output unless debugging
        services.AddJarvisSerilog(config =>
        {
            config.MinimumLevel.Is(LogEventLevel.Warning)
                .MinimumLevel.Override("core.jarvis", LogEventLevel.Debug)
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: "[{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");
        });
        
        // Configure NpgsqlDataSource for connection pooling
        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            // Add pooling parameters to connection string
            var pooledConnectionString = $"{_connectionString};Pooling=true;Maximum Pool Size=50;Minimum Pool Size=5";
            var dataSource = NpgsqlDataSource.Create(pooledConnectionString);
            return dataSource;
        });
        
        // Register PgClient as scoped using pooled connections
        services.AddScoped<IPgClient>(sp =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            var connection = dataSource.CreateConnection();
            var logger = sp.GetService<ILogger<PgClientWrapper>>();
            var pgClientWrapper = new PgClientWrapper(connection, ownsConnection: true, logger: logger);
            
            // For API tests, don't authenticate during setup as we're testing the auth service itself
            // Other tests can authenticate if needed
            
            return pgClientWrapper;
        });
        
        // Register new refactored services
        services.AddScoped<IDataContextCore, DataContextCore>();
        services.AddScoped<IAuditManager, AuditManager>();
        services.AddScoped<IRelationshipManager, RelationshipManager>();
        services.AddScoped<ITransactionManager, TransactionManager>();

        // Register main DataContext facade (maintains backward compatibility)
        services.AddScoped<IDataContext, DataContext>();
        services.AddScoped<EventSubscriptionManager>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEntityQuery, EntityQuery>();
        services.AddScoped<core.jarvis.Data.Schema.ITableManager, core.jarvis.Data.Schema.PostgreSqlTableManager>();
        
        // Register default event emitter for tests
        services.AddSingleton<core.jarvis.Events.IEventEmitter, core.jarvis.Events.Emitters.InMemoryEventEmitter>();

        // Register only the specific handlers we need for tests
        // NO DYNAMIC ASSEMBLY SCANNING - just explicit registrations
        
        // Test handlers
        services.AddTransient<TestHandler>();
        services.AddTransient<IComponentHandler<TestComponent>, TestHandler>();
        services.AddTransient<PositionTestHandler>();
        services.AddTransient<IComponentHandler<PositionComponent>, PositionTestHandler>();
        services.AddTransient<VelocityTestHandler>();
        services.AddTransient<IComponentHandler<VelocityComponent>, VelocityTestHandler>();
        
        // Blog example handlers
        services.AddTransient<BlogComponentHandler>();
        services.AddTransient<IComponentHandler<BlogComponent>, BlogComponentHandler>();
        services.AddTransient<BlogPostComponentHandler>();
        services.AddTransient<IComponentHandler<BlogPostComponent>, BlogPostComponentHandler>();
        services.AddTransient<BlogHandler>();
        
        // WorkOrder example handler
        services.AddTransient<Examples.WorkOrder.WorkOrderHandler>();
        services.AddTransient<IComponentHandler<Examples.WorkOrder.FakeWorkOrderComponent>, Examples.WorkOrder.WorkOrderHandler>();
        
        // Register query handlers for components we use in tests
        services.AddScoped<IComponentQueryHandler<tests.Components.TestComponent>, ComponentQueryHandler<tests.Components.TestComponent>>();
        services.AddScoped<IComponentQueryHandler<tests.Components.VersionedTestComponent>, ComponentQueryHandler<tests.Components.VersionedTestComponent>>();
        services.AddScoped<IComponentQueryHandler<tests.Components.RequiredFieldTestComponent>, ComponentQueryHandler<tests.Components.RequiredFieldTestComponent>>();
        services.AddScoped<IComponentQueryHandler<tests.Components.PaymentTestComponent>, ComponentQueryHandler<tests.Components.PaymentTestComponent>>();
        services.AddScoped<IComponentQueryHandler<tests.Components.PositionComponent>, ComponentQueryHandler<tests.Components.PositionComponent>>();
        services.AddScoped<IComponentQueryHandler<tests.Components.VelocityComponent>, ComponentQueryHandler<tests.Components.VelocityComponent>>();
        services.AddScoped<IComponentQueryHandler<tests.Components.NavigationItemTestComponent>, ComponentQueryHandler<tests.Components.NavigationItemTestComponent>>();
        services.AddScoped<IComponentQueryHandler<BlogComponent>, ComponentQueryHandler<BlogComponent>>();
        services.AddScoped<IComponentQueryHandler<BlogPostComponent>, ComponentQueryHandler<BlogPostComponent>>();
        services.AddScoped<IComponentQueryHandler<OrderComponent>, ComponentQueryHandler<OrderComponent>>();
        services.AddScoped<IComponentQueryHandler<InvoiceTestComponent>, ComponentQueryHandler<InvoiceTestComponent>>();
        services.AddScoped<IComponentQueryHandler<WorkOrderTestComponent>, ComponentQueryHandler<WorkOrderTestComponent>>();
        services.AddScoped<IComponentQueryHandler<Examples.WorkOrder.FakeWorkOrderComponent>, ComponentQueryHandler<Examples.WorkOrder.FakeWorkOrderComponent>>();
        services.AddScoped<IComponentQueryHandler<AuditEvent>, ComponentQueryHandler<AuditEvent>>();
        services.AddScoped<IComponentQueryHandler<Integration.HandlerIntegrationTests.NavigationItemTestComponent>, ComponentQueryHandler<Integration.HandlerIntegrationTests.NavigationItemTestComponent>>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _logger = _scope.ServiceProvider.GetRequiredService<ILogger<TestHandler>>();
        _dataContext = _scope.ServiceProvider.GetRequiredService<IDataContext>();
    }

    public virtual async Task DisposeAsync()
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
                await TestDataContext().Remove<Examples.WorkOrder.FakeWorkOrderComponent>(entityId);
                await TestDataContext().Remove<AuditEvent>(entityId);
                await TestDataContext().Remove<Integration.HandlerIntegrationTests.NavigationItemTestComponent>(entityId);

                // API component cleanup removed - should only be in ApiIntegrationTestBase
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up entity {EntityId}", entityId);
            }
        }

        // Dispose of scope first to ensure all scoped services are disposed
        _scope?.Dispose();

        // Dispose of the NpgsqlDataSource to properly close all pooled connections
        var dataSource = _serviceProvider?.GetService<NpgsqlDataSource>();
        dataSource?.Dispose();

        _serviceProvider?.Dispose();
    }
}