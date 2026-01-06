using core.jarvis.Data.GraphQL;
using core.jarvis.Data.Query;
using core.jarvis.Data.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Core implementation of data context operations.
/// </summary>
public class DataContextCore : IDataContextCore
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPgClient _pgClient;
    private readonly ITableManager _tableManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<DataContextCore> _logger;

    public DataContextCore(
        IServiceProvider serviceProvider,
        IPgClient pgClient,
        ITableManager tableManager,
        IAuditService auditService,
        ILogger<DataContextCore> logger)
    {
        _serviceProvider = serviceProvider;
        _pgClient = pgClient;
        _tableManager = tableManager;
        _auditService = auditService;
        _logger = logger;
    }

    public IComponentHandler For(Type componentType, Guid entityId)
    {
        var handlerType = typeof(ComponentHandler<>).MakeGenericType(componentType);
        var handler = (IComponentHandler)_serviceProvider.GetRequiredService(handlerType);
        handler.InitializeContext(entityId);
        return handler;
    }

    public THandler For<THandler>(Guid entityId) where THandler : class, IComponentHandler
    {
        var handler = _serviceProvider.GetRequiredService<THandler>();
        handler.InitializeContext(entityId);
        return handler;
    }

    public Entity NewEntity()
    {
        return new Entity { Id = Guid.NewGuid() };
    }

    public async Task<TComponent?> GetExistingComponent<TComponent>(TComponent component)
        where TComponent : class, IComponent, new()
    {
        try
        {
            var results = await _pgClient.From<TComponent>()
                .Filter("owner_entity_id", "eq", component.OwnerEntityId)
                .Get();
            return results.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public async Task Commit<TComponent>(TComponent component)
        where TComponent : class, IComponent, new()
    {
        await _tableManager.EnsureTableExists<TComponent>();
        await _pgClient.From<TComponent>().Upsert(component);
    }

    public async Task<bool> TryCommit<TComponent>(TComponent component)
        where TComponent : class, IComponent, new()
    {
        try
        {
            await _tableManager.EnsureTableExists<TComponent>();

            if (component is IVersionedComponent versioned)
            {
                // For versioned components, check version before upsert
                var existing = await GetExistingComponent(component);
                if (existing is IVersionedComponent existingVersioned)
                {
                    if (existingVersioned.Version != versioned.Version)
                    {
                        _logger.LogWarning("Version conflict: expected {Expected}, got {Actual}",
                            versioned.Version, existingVersioned.Version);
                        return false;
                    }
                }
                // Increment version
                versioned.Version = (versioned.Version ?? 0) + 1;
            }

            await _pgClient.From<TComponent>().Upsert(component);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TryCommit failed for {ComponentType}", typeof(TComponent).Name);
            return false;
        }
    }

    public Task CreateSnapshot<TComponent>(TComponent component, TComponent? existing, string operation)
        where TComponent : class, IComponent, new()
    {
        _logger.LogDebug("Creating snapshot for {ComponentType} with operation {Operation}",
            typeof(TComponent).Name, operation);
        return Task.CompletedTask;
    }

    public async Task Remove<TComponent>(Guid entityId)
        where TComponent : class, IComponent, new()
    {
        await _pgClient.From<TComponent>()
            .Filter("owner_entity_id", "eq", entityId)
            .Delete();
    }

    public async Task Insert<TModel>(TModel model)
        where TModel : class, IComponent, new()
    {
        await _tableManager.EnsureTableExists<TModel>();
        await _pgClient.From<TModel>().Insert(model);
    }

    public ISnapshotQuery Snapshots()
    {
        return new SnapshotQuery(_pgClient);
    }

    public IGraphQLQuery GraphQL(string query)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<GraphQLQueryBuilder>>();
        return new GraphQLQueryBuilder(_pgClient, logger, _auditService, query);
    }
}
