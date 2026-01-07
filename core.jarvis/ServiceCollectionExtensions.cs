using System.Reflection;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.Data.Schema;
using core.jarvis.Events;
using core.jarvis.Events.Emitters;
using core.jarvis.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace core.jarvis;

/// <summary>
/// Provides extension methods for setting up Jarvis services in an <see cref="IServiceCollection" />.
/// </summary>
public static class JarvisServiceCollectionExtensions
{
    // Database Configuration Constants
    /// <summary>
    /// Default maximum pool size for database connection pooling.
    /// Increased from 20 to 50 for better concurrent request handling.
    /// Can be overridden via JARVIS_DB_MAX_POOL_SIZE environment variable
    /// or Jarvis:Database:ConnectionPooling:MaxPoolSize configuration.
    /// </summary>
    private const int DefaultMaxPoolSize = 50;

    /// <summary>
    /// Default minimum pool size for database connection pooling.
    /// Ensures baseline connections are always available.
    /// Can be overridden via JARVIS_DB_MIN_POOL_SIZE environment variable
    /// or Jarvis:Database:ConnectionPooling:MinPoolSize configuration.
    /// </summary>
    private const int DefaultMinPoolSize = 10;

    /// <summary>
    /// Default connection lifetime in minutes for pooled connections.
    /// Prevents stale connections while maintaining efficiency.
    /// Can be overridden via JARVIS_DB_CONNECTION_LIFETIME_MINUTES environment variable
    /// or Jarvis:Database:ConnectionPooling:ConnectionLifetimeMinutes configuration.
    /// </summary>
    private const int DefaultConnectionLifetimeMinutes = 5;
    
    /// <summary>
    /// Standard PostgreSQL port number.
    /// Default port for PostgreSQL database connections.
    /// </summary>
    private const int DefaultPostgreSqlPort = 5432;
    /// <summary>
    /// Adds the core Jarvis framework services with handler-based architecture.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="minimumLogLevel">The minimum log level to configure for the Console logger (default: Information).</param>
    /// <param name="configuration">Optional configuration for event emission and other services.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection RegisterJarvis(
        this IServiceCollection services,
        LogLevel minimumLogLevel = LogLevel.Information,
        IConfiguration? configuration = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // 1. Add Serilog Logging
        services.AddJarvisSerilog(config =>
        {
            // Set minimum level based on parameter
            config.MinimumLevel.Is(ConvertToSerilogLevel(minimumLogLevel));
        });

        // 2. Add MediatR for event handling
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DataContext>());

        // 3. Register Query Handler Registry
        services.TryAddScoped<IComponentQueryHandlerRegistry, ComponentQueryHandlerRegistry>();

        // 4. Register generic query handler as open generic
        services.TryAddScoped(typeof(ComponentQueryHandler<>));

        // 5. Register EntityQuery factory
        services.TryAddScoped<IEntityQuery, EntityQuery>();
        
        // 5.5. Register Table Manager
        services.TryAddScoped<ITableManager, PostgreSqlTableManager>();

        // 6. Register DataContext services
        services.TryAddScoped<IDataContextCore, DataContextCore>();
        services.TryAddScoped<IAuditManager, AuditManager>();
        services.TryAddScoped<IRelationshipManager, RelationshipManager>();
        services.TryAddScoped<ITransactionManager, TransactionManager>();

        // Register the main DataContext facade (maintains backward compatibility)
        services.TryAddScoped<IDataContext, DataContext>();
        services.TryAddScoped<DataContext>();


        // 8. Register PostgreSQL Client
        // Register NpgsqlDataSource (singleton - manages the connection pool)
        services.TryAddSingleton<NpgsqlDataSource>(sp =>
        {
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
                ?? throw new ApplicationException("No connection string configured");

            // Get pool configuration with environment variable priority, then config, then defaults
            var maxPoolSize = GetPoolConfigValue("JARVIS_DB_MAX_POOL_SIZE",
                configuration?.GetValue<int?>("Jarvis:Database:ConnectionPooling:MaxPoolSize"),
                DefaultMaxPoolSize);
            var minPoolSize = GetPoolConfigValue("JARVIS_DB_MIN_POOL_SIZE",
                configuration?.GetValue<int?>("Jarvis:Database:ConnectionPooling:MinPoolSize"),
                DefaultMinPoolSize);
            var connectionLifetimeMinutes = GetPoolConfigValue("JARVIS_DB_CONNECTION_LIFETIME_MINUTES",
                configuration?.GetValue<int?>("Jarvis:Database:ConnectionPooling:ConnectionLifetimeMinutes"),
                DefaultConnectionLifetimeMinutes);

            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                MaxPoolSize = maxPoolSize,
                MinPoolSize = minPoolSize,
                ConnectionLifetime = connectionLifetimeMinutes * 60
            };

            return NpgsqlDataSource.Create(builder.ConnectionString);
        });

        // Register IPgClient (scoped - one connection per request)
        services.TryAddScoped<IPgClient>(sp =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            var connection = dataSource.CreateConnection();
            return new PgClientWrapper(connection, ownsConnection: true);
        });

        // 9. Register Audit Service
        services.TryAddScoped<IAuditService, AuditService>();
        
        // 10. Register Event Subscription Manager
        services.TryAddScoped<EventSubscriptionManager>();

        // 11. Configure Event Emission
        services.AddEventEmission(configuration);

        // 12. Add System layer for handler orchestration

        return services;
    }

    /// <summary>
    /// Gets a pool configuration value with priority: environment variable > config > default.
    /// </summary>
    private static int GetPoolConfigValue(string envVarName, int? configValue, int defaultValue)
    {
        var envValue = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrEmpty(envValue) && int.TryParse(envValue, out var parsed))
        {
            return parsed;
        }
        return configValue ?? defaultValue;
    }

    /// <summary>
    /// Builds a PostgreSQL connection string from Supabase URL and key.
    /// </summary>
    private static string BuildConnectionString(string supabaseUrl, string supabaseKey)
    {
        // Handle local/test URLs (localhost or 127.0.0.1)
        var uri = new Uri(supabaseUrl);
        if (uri.Host == "localhost" || uri.Host == "127.0.0.1")
        {
            // For local development/testing, use direct connection string
            return $"Host=localhost;Port={DefaultPostgreSqlPort};Database=postgres;Username=postgres;Password=postgres";
        }
        
        // Extract hostname from Supabase URL for production
        var projectRef = uri.Host.Split('.')[0];
        
        // Build PostgreSQL connection string
        // Note: In production, you'd want to extract actual database credentials
        // For now, using standard Supabase patterns
        return $"Host=db.{projectRef}.supabase.co;Database=postgres;Username=postgres;Password={supabaseKey};SSL Mode=Require;Trust Server Certificate=true";
    }
    
    /// <summary>
    /// Registers all component handlers and query handlers from the specified assembly.
    /// This method scans the assembly for:
    /// - IComponentHandler<T> implementations
    /// - IComponentQueryHandler<T> implementations
    /// - IComponent implementations (auto-registers ComponentQueryHandler<T> for them)
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="assembly">The assembly to scan for handlers and components.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection RegisterAllComponentHandlersAndQueriesFromAssembly(
        this IServiceCollection services, 
        Assembly assembly)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));
        
        var componentHandlerInterface = typeof(IComponentHandler<>);
        var queryHandlerInterface = typeof(IComponentQueryHandler<>);
        var componentQueryHandlerType = typeof(ComponentQueryHandler<>);
        
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Where(t => !t.Namespace?.Contains("Examples") ?? true); // Skip example handlers
        
        foreach (var type in types)
        {
            // Register IComponentHandler<T> implementations
            var componentHandlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == componentHandlerInterface);

            foreach (var handlerInterface in componentHandlerInterfaces)
            {
                // Use Scoped lifetime - handlers should be scoped with DataContext
                services.AddScoped(handlerInterface, type);
                // Also register to non-generic IComponentHandler base interface
                services.AddScoped(typeof(IComponentHandler), type);
                // Also register the concrete type for DataContext.For<THandler>()
                services.AddScoped(type);
            }

            // Register IComponentQueryHandler<T> implementations
            var queryHandlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == queryHandlerInterface);

            foreach (var queryInterface in queryHandlerInterfaces)
            {
                services.AddScoped(queryInterface, type);
            }
        }
        
        // Also scan for IComponent implementations and register default query handlers
        var componentInterface = typeof(IComponent);
        var componentTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && 
                   componentInterface.IsAssignableFrom(t));
            
        foreach (var componentType in componentTypes)
        {
            var queryHandlerInterfaceType = queryHandlerInterface.MakeGenericType(componentType);
            var concreteQueryHandler = componentQueryHandlerType.MakeGenericType(componentType);
            
            // Only register if not already registered by a custom implementation
            if (!services.Any(s => s.ServiceType == queryHandlerInterfaceType))
            {
                services.AddScoped(queryHandlerInterfaceType, sp => 
                    ActivatorUtilities.CreateInstance(sp, concreteQueryHandler));
            }
        }
        
        return services;
    }
    
    /// <summary>
    /// Converts Microsoft.Extensions.Logging LogLevel to Serilog LogEventLevel.
    /// </summary>
    private static Serilog.Events.LogEventLevel ConvertToSerilogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => Serilog.Events.LogEventLevel.Verbose,
            LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogLevel.Critical => Serilog.Events.LogEventLevel.Fatal,
            LogLevel.None => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Information
        };
    }

    /// <summary>
    /// Configures event emission based on configuration.
    /// </summary>
    private static IServiceCollection AddEventEmission(this IServiceCollection services, IConfiguration? configuration)
    {
        var eventEmitterType = configuration?["Jarvis:EventEmitter:Type"] ?? "NoOp";
        
        switch (eventEmitterType.ToLower())
        {
            case "inmemory":
                services.AddSingleton<InMemoryEventEmitter>();
                services.AddSingleton<IEventEmitter>(provider => provider.GetRequiredService<InMemoryEventEmitter>());
                break;
                
            case "noop":
            default:
                services.AddSingleton<IEventEmitter, NoOpEventEmitter>();
                break;
        }
        
        return services;
    }
}