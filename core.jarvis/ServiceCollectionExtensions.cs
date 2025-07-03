using System.Reflection;
using core.jarvis.Data;
using core.jarvis.Data.Query;
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

        // 6. Register DataContext
        services.TryAddScoped<IDataContext, DataContext>();
        services.TryAddScoped<DataContext>();


        // 8. Register PostgreSQL Client
        // Check for connection pooling feature flag
        var useConnectionPooling = configuration?.GetValue<bool>("Jarvis:Database:ConnectionPooling:Enabled") ?? false;
        
        if (useConnectionPooling)
        {
            // Register connection factory as singleton for connection pooling
            services.TryAddSingleton<INpgsqlConnectionFactory>(sp =>
            {
                var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                    ?? Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
                    ?? throw new ApplicationException("No connection string configured");
                
                var logger = sp.GetRequiredService<ILogger<NpgsqlConnectionFactory>>();
                
                // Get pool configuration
                var maxPoolSize = configuration?.GetValue<int>("Jarvis:Database:ConnectionPooling:MaxPoolSize") ?? 20;
                var minPoolSize = configuration?.GetValue<int>("Jarvis:Database:ConnectionPooling:MinPoolSize") ?? 5;
                var connectionLifetimeMinutes = configuration?.GetValue<int>("Jarvis:Database:ConnectionPooling:ConnectionLifetimeMinutes") ?? 5;
                
                return new NpgsqlConnectionFactory(
                    connectionString,
                    logger,
                    maxPoolSize,
                    minPoolSize,
                    TimeSpan.FromMinutes(connectionLifetimeMinutes));
            });
            
            // Register pooled PgClient
            services.TryAddScoped<IPgClient, PgClientPooled>();
        }
        else
        {
            // Use traditional single-connection model
            services.TryAddScoped<IPgClient>(sp =>
            {
                var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                    ?? Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
                    ?? throw new ApplicationException("No connection string configured");
                
                var connection = new NpgsqlConnection(connectionString);
                return new PgClientWrapper(connection);
            });
        }

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
    /// Builds a PostgreSQL connection string from Supabase URL and key.
    /// </summary>
    private static string BuildConnectionString(string supabaseUrl, string supabaseKey)
    {
        // Handle local/test URLs (localhost or 127.0.0.1)
        var uri = new Uri(supabaseUrl);
        if (uri.Host == "localhost" || uri.Host == "127.0.0.1")
        {
            // For local development/testing, use direct connection string
            return "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
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
                services.AddTransient(handlerInterface, type);
                // Also register the concrete type for DataContext.For<THandler>()
                services.AddTransient(type);
            }
            
            // Register IComponentQueryHandler<T> implementations
            var queryHandlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == queryHandlerInterface);
                
            foreach (var queryInterface in queryHandlerInterfaces)
            {
                services.AddTransient(queryInterface, type);
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