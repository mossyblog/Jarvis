using core.jarvis.Data.Components;
using core.jarvis.Data.GraphQL;
using core.jarvis.Data.Query;
using core.jarvis.Data.Schema;
using core.jarvis.ErrorHandling;
using core.jarvis.Events;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Refactored DataContext that uses composition and delegation.
/// Maintains the exact same public API while internally using specialized services.
/// </summary>
public class DataContext : IDataContext
{
    private readonly ILogger<DataContext> _logger;
    private readonly IComponentQueryHandlerRegistry _componentQueryHandlerRegistry;
    private readonly IDataContextCore _core;
    private readonly IAuditManager _audit;
    private readonly IRelationshipManager _relationships;
    private readonly ITransactionManager _transactions;
    private readonly IAuditService _auditService;
    private readonly Events.IEventEmitter _eventEmitter;

    public DataContext(
        IComponentQueryHandlerRegistry queryRegistry,
        IDataContextCore core,
        IAuditManager audit,
        IRelationshipManager relationships,
        ITransactionManager transactions,
        IAuditService auditService,
        Events.IEventEmitter eventEmitter,
        ILogger<DataContext> logger)
    {
        _componentQueryHandlerRegistry = queryRegistry ?? throw new ArgumentNullException(nameof(queryRegistry));
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _relationships = relationships ?? throw new ArgumentNullException(nameof(relationships));
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _eventEmitter = eventEmitter ?? throw new ArgumentNullException(nameof(eventEmitter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public IComponentHandler For(Type componentType, Guid entityId)
    {
        return _core.For(componentType, entityId);
    }

    /// <inheritdoc/>
    public THandler For<THandler>(Guid entityId) where THandler : class, IComponentHandler
    {
        return _core.For<THandler>(entityId);
    }

    /// <inheritdoc/>
    public IEntityQuery Query()
    {
        return new Query.EntityQuery(_componentQueryHandlerRegistry, this);
    }

    /// <inheritdoc/>
    public Entity NewEntity()
    {
        return _core.NewEntity();
    }

    /// <inheritdoc/>
    public async Task Commit<TComponent>(TComponent component)
        where TComponent : class, IComponent, new()
    {
        _logger.LogInformation("DataContext.Commit called for ComponentId={ComponentId}", component.Id);
        // Store variables for snapshot creation
        TComponent? existing = null;
        string operation = "UNKNOWN";

        // Execute the main commit transaction INCLUDING snapshot creation for atomic consistency
        try
        {
            _logger.LogInformation("DataContext.Commit: Starting transaction for ComponentId={ComponentId}", component.Id);
            await _transactions.ExecuteWithAudit(
                async () => {
                    // Ensure table exists before any operations
                    await _core.EnsureTableExists<TComponent>();

                    // Determine operation type inside the transaction context
                    existing = await _core.GetExistingComponent(component);
                    operation = existing == null ? "CREATE" : "UPDATE";

                    _logger.LogInformation("DataContext.Commit: Operation determined as {Operation} for ComponentId={ComponentId}", operation, component.Id);

                    // Execute the actual commit
                    await _core.Commit(component);

                    // Create snapshot INSIDE the transaction for atomic consistency
                    _logger.LogInformation("DataContext.Commit: Creating snapshot within transaction for ComponentId={ComponentId}, operation={Operation}", component.Id, operation);
                    await _core.CreateSnapshot(component, existing, operation);
                },
                component.OwnerEntityId,
                $"Commit<{typeof(TComponent).Name}>",
                new { ComponentType = typeof(TComponent).Name, ComponentId = component.Id });

            _logger.LogInformation("DataContext.Commit: Transaction completed for ComponentId={ComponentId}, operation={Operation}", component.Id, operation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DataContext.Commit: Transaction failed for ComponentId={ComponentId}", component.Id);
            throw;
        }

        // Log component changes for updates (after transaction succeeds)
        if (operation == "UPDATE" && existing != null)
        {
            try
            {
                await _audit.LogChange(
                    AuditEventTypes.ComponentUpdated,
                    component.OwnerEntityId,
                    existing,
                    component);
            }
            catch (Exception ex)
            {
                // Log audit failure but don't fail the operation
                _logger.LogError(ex,
                    "Component change audit failed for {ComponentType} {ComponentId}",
                    typeof(TComponent).Name, component.Id);
            }
        }

        // Log successful snapshot creation audit (moved outside of transaction)
        try
        {
            var snapshotVersion = component is IVersionedComponent versionedComponent ? versionedComponent.Version : null;
            await _audit.LogEvent(
                AuditEventTypes.SnapshotCreated,
                component.OwnerEntityId,
                new { 
                    ComponentType = typeof(TComponent).Name,
                    ComponentId = component.Id,
                    Operation = operation,
                    Version = snapshotVersion
                });
        }
        catch (Exception auditEx)
        {
            _logger.LogError(auditEx,
                "Snapshot creation audit failed for {ComponentType} {ComponentId}",
                typeof(TComponent).Name, component.Id);
        }
    }
    
    
    /// <inheritdoc/>
    public async Task<bool> TryCommit<TComponent>(TComponent component)
        where TComponent : class, IComponent, new()
    {
        // Store original state before commit for snapshot creation
        TComponent? existing = null;
        string operation = "UNKNOWN";

        var result = await _transactions.ExecuteWithAudit(
            async () => {
                // Ensure table exists before any operations
                await _core.EnsureTableExists<TComponent>();

                // Get existing state within transaction
                existing = await _core.GetExistingComponent(component);
                operation = existing == null ? "CREATE" : "UPDATE";

                // Execute TryCommit
                var commitResult = await _core.TryCommit(component);

                // Create snapshot INSIDE the transaction for atomic consistency (only if commit succeeded)
                if (commitResult)
                {
                    _logger.LogInformation("TryCommit: Creating snapshot within transaction for ComponentId={ComponentId}, operation={Operation}", component.Id, operation);
                    await _core.CreateSnapshot(component, existing, operation);
                }

                return commitResult;
            },
            component.OwnerEntityId,
            $"TryCommit<{typeof(TComponent).Name}>",
            new { ComponentType = typeof(TComponent).Name, ComponentId = component.Id, Operation = operation });

        // Log version conflict if TryCommit failed (likely due to version mismatch)
        if (!result && existing != null)
        {
            try
            {
                var auditData = new Dictionary<string, object>
                {
                    { "ComponentType", typeof(TComponent).Name },
                    { "ComponentId", component.Id },
                    { "Operation", "TryCommit" }
                };

                // Add version information if component implements IVersionedComponent
                if (existing is IVersionedComponent existingVersioned && component is IVersionedComponent componentVersioned)
                {
                    auditData["ExpectedVersion"] = existingVersioned.Version?.ToString() ?? "null";
                    auditData["ActualVersion"] = componentVersioned.Version?.ToString() ?? "null";
                }

                await _audit.LogEvent(
                    AuditEventTypes.ComponentVersionConflict,
                    component.OwnerEntityId,
                    auditData);
            }
            catch (Exception ex)
            {
                // Log audit failure but don't fail the operation
                _logger.LogError(ex,
                    "Version conflict audit failed for {ComponentType} {ComponentId}",
                    typeof(TComponent).Name, component.Id);
            }
        }

        // Log component changes for updates (after transaction succeeds, only if commit succeeded)
        if (result && operation == "UPDATE" && existing != null)
        {
            try
            {
                await _audit.LogChange(
                    AuditEventTypes.ComponentUpdated,
                    component.OwnerEntityId,
                    existing,
                    component);
            }
            catch (Exception ex)
            {
                // Log audit failure but don't fail the operation
                _logger.LogError(ex,
                    "Component change audit failed for {ComponentType} {ComponentId}",
                    typeof(TComponent).Name, component.Id);
            }
        }

        // Log successful snapshot creation audit (snapshot already created within transaction)
        if (result)
        {
            try
            {
                var snapshotVersion = component is IVersionedComponent versionedComponent ? versionedComponent.Version : null;
                await _audit.LogEvent(
                    AuditEventTypes.SnapshotCreated,
                    component.OwnerEntityId,
                    new {
                        ComponentType = typeof(TComponent).Name,
                        ComponentId = component.Id,
                        Operation = operation,
                        Version = snapshotVersion
                    });
            }
            catch (Exception ex)
            {
                // Log failure but don't fail the operation (snapshot was already created in transaction)
                _logger.LogError(ex,
                    "Snapshot audit logging failed for {ComponentType} {ComponentId}",
                    typeof(TComponent).Name, component.Id);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task Remove<TComponent>(Guid entityId)
        where TComponent : class, IComponent, new()
    {
        await _transactions.ExecuteWithAudit(
            () => _core.Remove<TComponent>(entityId),
            entityId,
            $"Remove<{typeof(TComponent).Name}>",
            new { ComponentType = typeof(TComponent).Name });

        // Log component deleted audit event
        try
        {
            await _audit.LogEvent(
                AuditEventTypes.ComponentDeleted,
                entityId,
                new {
                    ComponentType = typeof(TComponent).Name,
                    Operation = "Remove"
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log component deletion audit for {ComponentType}", typeof(TComponent).Name);
        }
    }

    

    /// <inheritdoc/>
    public async Task Insert<TModel>(TModel model)
        where TModel : class, IComponent, new()
    {
        var entityId = model is IComponent component ? component.OwnerEntityId : Guid.Empty;

        await _transactions.ExecuteWithAudit(
            () => _core.Insert(model),
            entityId,
            $"Insert<{typeof(TModel).Name}>",
            new { ModelType = typeof(TModel).Name });

        // Log data inserted audit event
        try
        {
            await _audit.LogEvent(
                AuditEventTypes.DataInserted,
                entityId,
                new {
                    ModelType = typeof(TModel).Name,
                    Operation = "Insert"
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log data insertion audit for {ModelType}", typeof(TModel).Name);
        }
    }

    /// <inheritdoc/>
    public Query.ISnapshotQuery Snapshots()
    {
        return _core.Snapshots();
    }

    /// <inheritdoc/>
    public GraphQL.IGraphQLQuery GraphQL(string query)
    {
        return _core.GraphQL(query);
    }

    /// <inheritdoc/>
    public async Task<Guid?> Parent(Guid entityId)
    {
        return await _relationships.Parent(entityId);
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> Children(Guid entityId)
    {
        return await _relationships.Children(entityId);
    }

    /// <inheritdoc/>
    public async Task<bool> ChildOf(Guid childId, Guid parentId)
    {
        return await _relationships.ChildOf(childId, parentId);
    }

    /// <inheritdoc/>
    public async Task LinkRelationship(Guid parentId, Guid childId, string? parentType = null, string? childType = null)
    {
        // Track the pending relationship for validation at commit time
        _transactions.TrackPendingRelationship(childId, parentId);

        await _relationships.LinkRelationship(parentId, childId, parentType, childType);
    }

    /// <inheritdoc/>
    public async Task UnlinkRelationship(Guid parentId, Guid childId)
    {
        await _relationships.UnlinkRelationship(parentId, childId);
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> Ancestors(Guid entityId)
    {
        return await _relationships.Ancestors(entityId);
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> Descendants(Guid entityId)
    {
        return await _relationships.Descendants(entityId);
    }

    /// <inheritdoc/>
    public async Task Emit<TEvent>(TEvent @event) where TEvent : Events.IEvent
    {
        try
        {
            AddEventMetadata(@event);
            await _eventEmitter.Emit(@event);

            await _auditService.LogEvent(
                AuditEventTypes.EventEmitted,
                Guid.Empty,
                new {
                    EventId = @event.Id,
                    EventType = @event.Type,
                    EventMetadata = @event.Metadata
                });
        }
        catch (Exception ex)
        {
            await _auditService.LogError(
                AuditEventTypes.EventEmissionFailed,
                Guid.Empty,
                ex,
                new { EventId = @event.Id, EventType = @event.Type });

            throw new EventEmissionException($"Failed to emit event {@event.Type}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task EmitBatch<TEvent>(IEnumerable<TEvent> events) where TEvent : Events.IEvent
    {
        try
        {
            var eventsList = events.ToList();
            foreach (var @event in eventsList)
            {
                AddEventMetadata(@event);
            }

            await _eventEmitter.EmitBatch(eventsList);

            await _auditService.LogEvent(
                AuditEventTypes.EventBatchEmitted,
                Guid.Empty,
                new {
                    EventCount = eventsList.Count,
                    EventTypes = eventsList.Select(e => e.Type).Distinct().ToList()
                });
        }
        catch (Exception ex)
        {
            await _auditService.LogError(
                AuditEventTypes.EventEmissionFailed,
                Guid.Empty,
                ex,
                new { EventCount = events.Count() });

            throw new EventEmissionException("Failed to emit event batch", ex);
        }
    }

    /// <inheritdoc/>
    public async Task EnsureEntityTableExists()
    {
        await _transactions.EnsureEntityTableExists();
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteInTransaction<T>(Func<Task<T>> operation)
    {
        return await _transactions.ExecuteInTransaction(operation);
    }

    /// <inheritdoc/>
    public async Task ExecuteInTransaction(Func<Task> operation)
    {
        await _transactions.ExecuteInTransaction(operation);
    }

    private void AddEventMetadata<TEvent>(TEvent @event) where TEvent : Events.IEvent
    {
        @event.Metadata["EmittedFrom"] = "DataContext";
        @event.Metadata["EmittedAt"] = DateTime.UtcNow.ToString("O");
    }
}
