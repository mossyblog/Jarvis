using System.Collections.Concurrent;
using System.Linq;
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
    private ILogger<TestHandler> _logger;
    private IDataContext _dataContext;
    private readonly ConcurrentDictionary<Guid, byte> _testEntities = new();
    private string _connectionString;
    private System.Reflection.Assembly? _apiAssembly;

    protected ILogger<TestHandler> Logger() => _logger;
    protected IDataContext TestDataContext() => _dataContext;

    protected void TrackEntity(Guid entityId)
    {
        _testEntities.TryAdd(entityId, 0);
    }

    public async Task InitializeAsync()
    {
        // For GraphQL tests, ensure JWT environment variables are set
        // This is needed because PgClient validates JWT signatures
        if (this.GetType().Namespace?.Contains("GraphQL") == true)
        {
            Environment.SetEnvironmentVariable("Jwt__SecretKey", "GraphQL-Test-Secret-Key-Must-Be-At-Least-256-Bits-For-Security");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "graphql-test-issuer");
            Environment.SetEnvironmentVariable("Jwt__Audience", "graphql-test-audience");
        }
        
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
            
            // For API tests, don't authenticate during setup as we're testing the auth service itself
            // Other tests can authenticate if needed
            
            return pgClientWrapper;
        });
        
        services.AddTransient<IDataContext, DataContext>();
        services.AddScoped<EventSubscriptionManager>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEntityQuery, EntityQuery>();
        
        // Register default event emitter for tests
        services.AddSingleton<core.jarvis.Events.IEventEmitter, core.jarvis.Events.Emitters.NoOpEventEmitter>();

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
        
        // Register WorkOrder example handler
        services.AddTransient<Examples.WorkOrder.WorkOrderHandler>();
        
        // Register API handlers if available (for API tests)
        _apiAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "core.jarvis.api");
        if (_apiAssembly != null)
        {
            services.RegisterAllComponentHandlersAndQueriesFromAssembly(_apiAssembly);
            
            // Register System services
            services.AddTransient<core.jarvis.api.Systems.RegistrationSystem>();
            
            // Register API services for authentication tests
            services.AddTransient<core.jarvis.api.Services.IPasswordPolicyService, core.jarvis.api.Services.PasswordPolicyService>();
            
            // TokenService needs specific constructor parameters
            services.AddTransient<core.jarvis.api.Services.ITokenService>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                return new core.jarvis.api.Services.TokenService(
                    issuer: config["Jwt:Issuer"] ?? "test-issuer",
                    audience: config["Jwt:Audience"] ?? "test-audience",
                    secretKey: config["Jwt:SecretKey"] ?? "Test-Secret-Key-Must-Be-At-Least-256-Bits-For-Security-Testing",
                    accessTokenExpirationMinutes: int.Parse(config["Jwt:AccessTokenExpirationMinutes"] ?? "15")
                );
            });
            
            services.AddTransient<core.jarvis.api.Services.ISecurityAuditService, core.jarvis.api.Services.SecurityAuditService>();
            services.AddTransient<core.jarvis.api.Services.IConstantTimeService, core.jarvis.api.Services.ConstantTimeService>();
            services.AddSingleton<IConfiguration>(sp =>
            {
                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddEnvironmentVariables();
                return configBuilder.Build();
            });
        }

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
                await TestDataContext().Remove<Examples.WorkOrder.WorkOrderComponent>(entityId);
                await TestDataContext().Remove<AuditEvent>(entityId);
                
                // Clean up API components if available
                if (_apiAssembly != null)
                {
                    // For Account, we need to check if the entity actually owns an Account component
                    try
                    {
                        await TestDataContext().Remove<core.jarvis.api.Models.Account>(entityId);
                    }
                    catch { }
                    
                    try
                    {
                        await TestDataContext().Remove<core.jarvis.api.Models.AuthToken>(entityId);
                    }
                    catch { }
                    
                    try
                    {
                        await TestDataContext().Remove<core.jarvis.api.Models.SecurityProfile>(entityId);
                    }
                    catch { }
                    
                    try
                    {
                        await TestDataContext().Remove<core.jarvis.api.Models.Role>(entityId);
                    }
                    catch { }
                    
                    try
                    {
                        await TestDataContext().Remove<core.jarvis.api.Models.Permission>(entityId);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up entity {EntityId}", entityId);
            }
        }
        
        _serviceProvider?.Dispose();
    }
}