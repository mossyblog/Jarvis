using System.Text.Json;
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
        // Resolve via interface (IComponentHandler<T>) not base class (ComponentHandler<T>)
        var handlerInterface = typeof(IComponentHandler<>).MakeGenericType(componentType);
        try
        {
            var handler = (IComponentHandler)_serviceProvider.GetRequiredService(handlerInterface);
            handler.InitializeContext(entityId);
            return handler;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No service for type"))
        {
            throw new Exceptions.ComponentNotFoundException(
                $"Handler for component type '{componentType.Name}' is not registered. Ensure it is registered in the service container.",
                ex);
        }
    }

    public THandler For<THandler>(Guid entityId) where THandler : class, IComponentHandler
    {
        try
        {
            var handler = _serviceProvider.GetRequiredService<THandler>();
            handler.InitializeContext(entityId);
            return handler;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No service for type"))
        {
            throw new Exceptions.ComponentNotFoundException(
                $"Handler of type '{typeof(THandler).Name}' is not registered. Ensure it is registered in the service container.",
                ex);
        }
    }

    public Entity NewEntity()
    {
        return new Entity { Id = Guid.NewGuid() };
    }

    public async Task EnsureTableExists<TComponent>() where TComponent : class, IComponent, new()
    {
        await _tableManager.EnsureTableExists<TComponent>();
    }

    public async Task<TComponent?> GetExistingComponent<TComponent>(TComponent component)
        where TComponent : class, IComponent, new()
    {
        // For components that allow multiple entries per entity (posts, comments, etc.),
        // we must filter by the component's unique Id to find the specific existing record.
        // Filtering only by owner_entity_id would return the wrong component.
        var results = await _pgClient.From<TComponent>()
            .Filter("id", "eq", component.Id)
            .Get();
        return results.FirstOrDefault();
    }

    public async Task Commit<TComponent>(TComponent component)
        where TComponent : class, IComponent, new()
    {
        await _tableManager.EnsureTableExists<TComponent>();

        if (component is IVersionedComponent versioned)
        {
            // For versioned components, check version before upsert and throw on conflict
            var existing = await GetExistingComponent(component);
            if (existing is IVersionedComponent existingVersioned)
            {
                if (existingVersioned.Version != versioned.Version)
                {
                    _logger.LogWarning("Version conflict in Commit: expected {Expected}, actual {Actual}",
                        versioned.Version, existingVersioned.Version);
                    throw new Exceptions.ConcurrencyException(
                        component.OwnerEntityId,
                        typeof(TComponent).Name,
                        versioned.Version,
                        existingVersioned.Version);
                }
            }
            // Increment version
            versioned.Version = (versioned.Version ?? 0) + 1;
        }

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

    public async Task CreateSnapshot<TComponent>(TComponent component, TComponent? existing, string operation)
        where TComponent : class, IComponent, new()
    {
        _logger.LogDebug("Creating snapshot for {ComponentType} with operation {Operation}",
            typeof(TComponent).Name, operation);

        // Ensure component_snapshots table exists using proper table manager
        await _tableManager.EnsureSnapshotsTableExists();

        var componentType = typeof(TComponent).Name;

        // For CREATE: Snapshot the new component being created
        // For UPDATE: Snapshot the existing (pre-update) state for audit trail
        var snapshotTarget = operation == "UPDATE" && existing != null ? existing : component;
        var version = (snapshotTarget as IVersionedComponent)?.Version ?? 1;
        var componentJson = JsonSerializer.Serialize(snapshotTarget);

        // Create new snapshot entry
        var newSnapshot = new
        {
            Version = version,
            DataJson = componentJson,
            Operation = operation,
            Timestamp = DateTime.UtcNow,
            CreatedBy = "system"
        };

        // Check if snapshot record exists for this component using typed query
        var existingRecords = await _pgClient.From<ComponentSnapshots>()
            .Filter("component_id", "eq", component.Id)
            .Filter("component_type", "eq", componentType)
            .Get();

        var record = existingRecords.FirstOrDefault();

        if (record == null)
        {
            // Create new snapshot record using typed insert
            var snapshotsJson = JsonSerializer.Serialize(new[] { newSnapshot });
            var newRecord = new ComponentSnapshots
            {
                Id = Guid.NewGuid(),
                EntityId = component.OwnerEntityId,
                ComponentType = componentType,
                ComponentId = component.Id,
                Snapshots = snapshotsJson,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            await _pgClient.From<ComponentSnapshots>().Insert(newRecord);
        }
        else
        {
            // Append to existing snapshots
            var existingSnapshots = record.GetSnapshots();
            var allSnapshots = existingSnapshots.Select(s => new
            {
                s.Version,
                s.DataJson,
                s.Operation,
                s.Timestamp,
                s.CreatedBy
            }).ToList();
            allSnapshots.Add(newSnapshot);

            // Update using typed operation
            record.Snapshots = JsonSerializer.Serialize(allSnapshots);
            record.LastUpdated = DateTime.UtcNow;
            await _pgClient.From<ComponentSnapshots>().Update(record);
        }
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
