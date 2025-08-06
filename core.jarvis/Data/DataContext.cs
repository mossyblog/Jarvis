using System.Text.Json;
using core.jarvis.Data.Components;
using core.jarvis.Data.GraphQL;
using core.jarvis.Data.Query;
using core.jarvis.Data.Schema;
using core.jarvis.ErrorHandling;
using core.jarvis.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Concrete implementation of IDataContext.
/// Delegates all component operations to registered handlers.
/// </summary>
public class DataContext : IDataContext
{
    private readonly IComponentQueryHandlerRegistry _queryRegistry;
    private readonly IPgClient _pgClient;
    private readonly ILogger<DataContext> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuditService _auditService;
    private readonly Events.IEventEmitter _eventEmitter;
    private readonly ITableManager _tableManager;
    
    // Transaction-aware tracking for entities and relationships
    private readonly HashSet<Guid> _pendingEntityIds = new();
    private readonly Dictionary<Guid, Guid> _pendingChildToParentMap = new();
    private readonly object _pendingLock = new();
    
    public DataContext(
        IServiceProvider serviceProvider,
        IComponentQueryHandlerRegistry queryRegistry,
        IPgClient pgClient,
        ILogger<DataContext> logger,
        IAuditService auditService,
        Events.IEventEmitter eventEmitter,
        ITableManager tableManager)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _queryRegistry = queryRegistry ?? throw new ArgumentNullException(nameof(queryRegistry));
        _pgClient = pgClient ?? throw new ArgumentNullException(nameof(pgClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _eventEmitter = eventEmitter ?? throw new ArgumentNullException(nameof(eventEmitter));
        _tableManager = tableManager ?? throw new ArgumentNullException(nameof(tableManager));
    }

    public IComponentHandler For(Type componentType, Guid entityId)
    {
        // Need to find the handler for this component type
        // First, try to find IComponentHandler<componentType>
        var componentHandlerInterface = typeof(IComponentHandler<>).MakeGenericType(componentType);
        var handler = _serviceProvider.GetService(componentHandlerInterface) as IComponentHandler;
        
        if (handler == null)
        {
            throw new ComponentNotFoundException($"No handler registered for component type {componentType.Name}. Ensure the handler is registered in DI.");
        }
        
        // Initialize the handler with the entity ID
        handler.InitializeContext(entityId);
        
        return handler;
    }

    /// <inheritdoc/>
    public THandler For<THandler>(Guid entityId) where THandler : class, IComponentHandler
    {
        var handler = _serviceProvider.GetService<THandler>() 
               ?? throw new ComponentNotFoundException($"No handler registered for handler type {typeof(THandler).Name}. Ensure the handler is registered in DI.");
        
        // Initialize the handler with the entity ID
        handler.InitializeContext(entityId);
        
        return handler;
    }

    /// <inheritdoc/>
    public IEntityQuery Query()
    {
        return new EntityQuery(_queryRegistry);
    }

    /// <summary>
    /// Creates a new Entity with a generated ID.
    /// </summary>
    /// <returns>A new Entity instance with a unique ID.</returns>
    public Entity NewEntity()
    {
        return new Entity(Guid.NewGuid());
    }

    public async Task Commit<TComponent>(TComponent component) 
        where TComponent : class, IComponent, new()
    {
        await ExecuteWithErrorHandling(async () =>
        {
            // Track this entity as pending before validation
            lock (_pendingLock)
            {
                _pendingEntityIds.Add(component.OwnerEntityId);
            }
            
            // Only validate relationships for non-EntityRelationship components
            // EntityRelationship is internal infrastructure and should not trigger validation
            if (typeof(TComponent) != typeof(EntityRelationship))
            {
                await ValidatePendingRelationships();
            }
            
            component.LastUpdated = DateTime.UtcNow;
            var existing = await GetExistingComponent(component);
            
            if (existing != null && component is IVersionedComponent)
            {
                await Snapshot(existing, "UPDATE");
            }
            
            HandleVersioning(component, existing);
            await ExecuteDatabaseOperation(component, existing == null);
            await AuditComponentOperation(component, existing, existing == null ? "CREATED" : "UPDATED");
            
            if (existing == null && component is IVersionedComponent)
            {
                await Snapshot(component, "CREATE");
            }
            
            // Clear this entity from pending after successful commit
            lock (_pendingLock)
            {
                _pendingEntityIds.Remove(component.OwnerEntityId);
                // Also remove any relationships where this entity is a child
                _pendingChildToParentMap.Remove(component.OwnerEntityId);
            }
        }, "Commit", component.OwnerEntityId, new { ComponentType = typeof(TComponent).Name, ComponentId = component.Id });
    }
    
    
    /// <inheritdoc/>
    public async Task<bool> TryCommit<TComponent>(TComponent component) 
        where TComponent : class, IComponent, new()
    {
        try
        {
            // Track this entity as pending before validation
            lock (_pendingLock)
            {
                _pendingEntityIds.Add(component.OwnerEntityId);
            }
            
            // Only validate relationships for non-EntityRelationship components
            // EntityRelationship is internal infrastructure and should not trigger validation
            if (typeof(TComponent) != typeof(EntityRelationship))
            {
                await ValidatePendingRelationships();
            }
            
            var originalUpdatedAt = component.LastUpdated;
            component.LastUpdated = DateTime.UtcNow;
            var existing = await GetExistingComponent(component);

            if (existing == null)
            {
                HandleVersioning(component, existing);
                await ExecuteDatabaseOperation(component, true);
                await AuditComponentOperation(component, existing, "CREATED");
                
                if (component is IVersionedComponent)
                {
                    await Snapshot(component, "CREATE");
                }
                
                // Clear this entity from pending after successful commit
                lock (_pendingLock)
                {
                    _pendingEntityIds.Remove(component.OwnerEntityId);
                    // Also remove any relationships where this entity is a child
                    _pendingChildToParentMap.Remove(component.OwnerEntityId);
                }
                
                return true;
            }

            // Check for concurrency conflicts
            if (component is IVersionedComponent versionedComp && existing is IVersionedComponent existingVersioned)
            {
                if (versionedComp.Version != existingVersioned.Version)
                {
                    await _auditService.LogEvent(
                        AuditEventTypes.ComponentVersionConflict,
                        component.OwnerEntityId,
                        new { 
                            ComponentType = typeof(TComponent).Name, 
                            ComponentId = component.Id, 
                            ExpectedVersion = versionedComp.Version, 
                            ActualVersion = existingVersioned.Version 
                        });
                        
                    ErrorHandlingPolicy.LogExpectedError(
                        $"Version mismatch for {typeof(TComponent).Name} ID {component.Id}. Expected version: {versionedComp.Version}, Actual version: {existingVersioned.Version}",
                        new { ComponentType = typeof(TComponent).Name, ComponentId = component.Id, ExpectedVersion = versionedComp.Version, ActualVersion = existingVersioned.Version });
                    
                    // Clear pending data on version conflict
                    lock (_pendingLock)
                    {
                        _pendingEntityIds.Remove(component.OwnerEntityId);
                    }
                    
                    return false;
                }
                
                await Snapshot(existing, "UPDATE");
                HandleVersioning(component, existing);
            }
            else
            {
                var timeDiff = Math.Abs((existing.LastUpdated - originalUpdatedAt).TotalMilliseconds);
                if (timeDiff > 10)
                {
                    await _auditService.LogEvent(
                        AuditEventTypes.ComponentConcurrencyConflict,
                        component.OwnerEntityId,
                        new { 
                            ComponentType = typeof(TComponent).Name, 
                            ComponentId = component.Id, 
                            ExpectedTimestamp = originalUpdatedAt, 
                            ActualTimestamp = existing.LastUpdated, 
                            DifferenceMs = timeDiff 
                        });
                        
                    ErrorHandlingPolicy.LogExpectedError(
                        $"Timestamp concurrency conflict for {typeof(TComponent).Name} ID {component.Id}. Expected: {originalUpdatedAt:O}, Actual: {existing.LastUpdated:O}, Diff: {timeDiff}ms",
                        new { ComponentType = typeof(TComponent).Name, ComponentId = component.Id, ExpectedTimestamp = originalUpdatedAt, ActualTimestamp = existing.LastUpdated, DifferenceMs = timeDiff });
                    return false;
                }
            }
            
            await ExecuteDatabaseOperation(component, false);
            await AuditComponentOperation(component, existing, "UPDATED");
            
            // Clear this entity from pending after successful commit
            lock (_pendingLock)
            {
                _pendingEntityIds.Remove(component.OwnerEntityId);
                // Also remove any relationships where this entity is a child
                _pendingChildToParentMap.Remove(component.OwnerEntityId);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            // Clear pending data on error
            lock (_pendingLock)
            {
                _pendingEntityIds.Remove(component.OwnerEntityId);
            }
            
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                component.OwnerEntityId,
                ex,
                new { Operation = "TryCommit", ComponentType = typeof(TComponent).Name, ComponentId = component.Id });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to save component {typeof(TComponent).Name} for entity {component.OwnerEntityId}",
                new { ComponentType = typeof(TComponent).Name, EntityId = component.OwnerEntityId, ComponentId = component.Id });
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task Remove<TComponent>(Guid entityId) 
        where TComponent : class, IComponent, new()
    {
        await ExecuteWithErrorHandling(async () =>
        {
            // Ensure table exists before attempting to delete
            await _tableManager.EnsureTableExists<TComponent>();
            
            await _auditService.LogEvent(
                AuditEventTypes.ForComponent(typeof(TComponent).Name, "DELETED"),
                entityId,
                new { ComponentType = typeof(TComponent).Name, Operation = "Remove" });
            
            await _pgClient.From<TComponent>()
                .Filter("owner_entity_id", "eq", entityId)
                .Delete();
        }, "Remove", entityId, new { ComponentType = typeof(TComponent).Name });
    }

    

    /// <inheritdoc/>
    public async Task Insert<TModel>(TModel model) 
        where TModel : class, new()
    {
        var entityId = model is IComponent component ? component.OwnerEntityId : Guid.Empty;
        
        await ExecuteWithErrorHandling(async () =>
        {
            // Ensure table exists before attempting to insert (only for IComponent types)
            if (model is IComponent && typeof(IComponent).IsAssignableFrom(typeof(TModel)))
            {
                // Use reflection to call EnsureTableExists<TModel> when TModel implements IComponent
                var method = _tableManager.GetType().GetMethod("EnsureTableExists");
                var genericMethod = method!.MakeGenericMethod(typeof(TModel));
                await (Task)genericMethod.Invoke(_tableManager, null)!;
            }
            
            await _pgClient.From<TModel>().Insert(model);
            
            await _auditService.LogEvent(
                AuditEventTypes.ForComponent(typeof(TModel).Name, "INSERTED"),
                entityId,
                new { ModelType = typeof(TModel).Name, Operation = "Insert" });
        }, "Insert", entityId, new { ModelType = typeof(TModel).Name, ModelData = model });
    }
    
    /// <inheritdoc/>
    public ISnapshotQuery Snapshots()
    {
        return new SnapshotQuery(_pgClient);
    }
    
    /// <inheritdoc/>
    public IGraphQLQuery GraphQL(string query)
    {
        var logger = _serviceProvider.GetService<ILogger<GraphQLQueryBuilder>>() 
            ?? _logger as ILogger<GraphQLQueryBuilder>
            ?? throw new InvalidOperationException("Unable to resolve logger for GraphQLQueryBuilder");
            
        var auditService = _serviceProvider.GetService<IAuditService>();
        
        return new GraphQLQueryBuilder(_pgClient, logger, auditService, query);
    }

    private async Task Snapshot<TComponent>(TComponent component, string operation = "UPDATE")
        where TComponent : IComponent
    {
        try
        {
            // Create a clean copy of the component for serialization
            var componentData = new Dictionary<string, object?>();
            
            // Use reflection to get all public properties
            var componentType = component.GetType();
            foreach (var prop in componentType.GetProperties())
            {
                // Skip properties from BaseComponent that cause serialization issues
                if (prop.DeclaringType == typeof(BaseComponent))
                    continue;
                
                // Skip any property that can't be easily serialized
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    continue;
                
                // Get the value
                var value = prop.GetValue(component);
                if (value != null)
                {
                    componentData[prop.Name] = value;
                }
            }
            
            var version = component is IVersionedComponent vers ? (vers.Version ?? 1) : 1;
            var snapshotEntry = new 
            {
                version = version,
                data = componentData,
                operation = operation,
                timestamp = DateTime.UtcNow,
                created_by = "system" // Could be enhanced to capture actual user
            };
            
            // Try to get existing snapshot record
            ComponentSnapshots? existing = null;
            try
            {
                existing = await _pgClient.From<ComponentSnapshots>()
                    .Filter("component_id", "eq", component.Id)
                    .Single();
            }
            catch (InvalidOperationException)
            {
                // No existing record, will create new one
            }
            catch (Exception ex)
            {
                // Log and return - don't fail the operation
                ErrorHandlingPolicy.LogAndContinue(
                    ex,
                    $"Failed to retrieve existing snapshot for component {component.Id}",
                    new { ComponentId = component.Id, ComponentType = component.GetType().Name });
                return;
            }
            
            if (existing != null)
            {
                // Get current snapshots array
                var snapshots = existing.GetSnapshots();
                
                // Create new array with appended snapshot
                var newSnapshotsArray = snapshots
                    .Select(s => new 
                    {
                        version = s.Version,
                        data = (object)JsonSerializer.Deserialize<object>(s.Data.RootElement.GetRawText())!,
                        operation = s.Operation,
                        timestamp = s.Timestamp,
                        created_by = s.CreatedBy
                    })
                    .Concat(new[] { new 
                    {
                        version = snapshotEntry.version,
                        data = (object)snapshotEntry.data,
                        operation = snapshotEntry.operation,
                        timestamp = snapshotEntry.timestamp,
                        created_by = snapshotEntry.created_by
                    }})
                    .ToArray();
                
                // Update the snapshots JSON
                existing.Snapshots = JsonSerializer.Serialize(newSnapshotsArray);
                existing.LastUpdated = DateTime.UtcNow;
                
                try
                {
                    await _pgClient.From<ComponentSnapshots>().Update(existing);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Failed to update snapshot record for component {ComponentId}", component.Id);
                    throw;
                }
            }
            else
            {
                // Create new record with first snapshot
                var record = new ComponentSnapshots
                {
                    Id = Guid.NewGuid(),
                    EntityId = component.OwnerEntityId,
                    ComponentType = component.GetType().Name,
                    ComponentId = component.Id,
                    Snapshots = JsonSerializer.Serialize(new[] { snapshotEntry }),
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                
                try
                {
                    await _pgClient.From<ComponentSnapshots>().Insert(record);
                }
                catch (Exception insertEx)
                {
                    _logger.LogError(insertEx, "Failed to create snapshot record for component {ComponentId}", component.Id);
                    throw;
                }
            }
            
            // Audit successful snapshot creation
            await _auditService.LogEvent(
                AuditEventTypes.SnapshotCreated,
                component.OwnerEntityId,
                new { 
                    ComponentType = component.GetType().Name, 
                    ComponentId = component.Id, 
                    Version = version,
                    Operation = operation 
                });
            
            _logger.LogInformation("Successfully created snapshot for {ComponentType} {ComponentId}, operation: {Operation}, version: {Version}",
                component.GetType().Name, component.Id, operation, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture snapshot for {ComponentType} {ComponentId}, operation: {Operation}",
                component.GetType().Name, component.Id, operation);
            
            // Audit snapshot failure
            try
            {
                await _auditService.LogError(
                    AuditEventTypes.SnapshotFailed,
                    component.OwnerEntityId,
                    ex,
                    new { 
                        ComponentType = component.GetType().Name, 
                        ComponentId = component.Id, 
                        Operation = operation 
                    });
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx, "Failed to audit snapshot failure for component {ComponentId}", component.Id);
            }
                
            // Log but don't fail the operation - fire and forget
            ErrorHandlingPolicy.LogAndContinue(
                ex,
                $"Failed to capture snapshot for {component.GetType().Name} {component.Id}",
                new { ComponentType = component.GetType().Name, ComponentId = component.Id, Operation = operation });
        }
    }

    /// <inheritdoc/>
    public async Task<Guid?> Parent(Guid entityId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            await AuditRelationshipQuery("Parent", "GetParent", entityId);
            
            var relationship = await _pgClient.From<EntityRelationship>()
                .Filter("owner_entity_id", "eq", entityId)
                .SingleOrDefault();
            
            return relationship?.ParentId;
        }, "Parent", entityId, defaultValue: (Guid?)null);
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> Children(Guid entityId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            await AuditRelationshipQuery("Children", "GetChildren", entityId);
            
            var relationship = await _pgClient.From<EntityRelationship>()
                .Filter("owner_entity_id", "eq", entityId)
                .SingleOrDefault();
            
            return relationship?.ChildrenIds?.ToList() ?? new List<Guid>();
        }, "Children", entityId, defaultValue: new List<Guid>());
    }

    /// <inheritdoc/>
    public async Task<bool> ChildOf(Guid childId, Guid parentId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            await AuditRelationshipQuery("ChildOf", "CheckParentChild", childId, new { ChildId = childId, ParentId = parentId });
            
            var childRelationship = await _pgClient.From<EntityRelationship>()
                .Filter("owner_entity_id", "eq", childId)
                .SingleOrDefault();
            
            return childRelationship?.ParentId == parentId;
        }, "ChildOf", childId, new { ChildId = childId, ParentId = parentId }, defaultValue: false);
    }

    /// <summary>
    /// Checks if an entity exists in the database.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <returns>True if the entity exists, false otherwise.</returns>
    private async Task<bool> EntityExists(Guid entityId)
    {
        try
        {
            var entity = await _pgClient.From<Entity>()
                .Filter("id", "eq", entityId)
                .SingleOrDefault();
            return entity != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking entity existence for ID {EntityId}", entityId);
            return false;
        }
    }
    
    /// <summary>
    /// Checks if an entity exists in the database or will be created in the current batch.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <returns>True if the entity exists or will exist, false otherwise.</returns>
    private async Task<bool> EntityExistsOrWillExist(Guid entityId)
    {
        // Check if entity is being created in current batch
        lock (_pendingLock)
        {
            if (_pendingEntityIds.Contains(entityId))
            {
                return true;
            }
        }
        
        // Check if entity already exists in database
        return await EntityExists(entityId);
    }
    
    /// <summary>
    /// Validates all pending relationships to ensure parent entities exist or will exist.
    /// </summary>
    private async Task ValidatePendingRelationships()
    {
        Dictionary<Guid, Guid> relationshipsToValidate;
        lock (_pendingLock)
        {
            relationshipsToValidate = new Dictionary<Guid, Guid>(_pendingChildToParentMap);
        }
        
        foreach (var (childId, parentId) in relationshipsToValidate)
        {
            if (!await EntityExistsOrWillExist(parentId))
            {
                throw new InvalidOperationException(
                    $"Cannot create relationship: Parent entity {parentId} does not exist and is not being created in the current batch. " +
                    $"Child entity {childId} requires this parent to exist.");
            }
        }
    }
    
    /// <summary>
    /// Clears all pending transaction data. Called after successful commit or on error.
    /// </summary>
    private void ClearPendingData()
    {
        lock (_pendingLock)
        {
            _pendingEntityIds.Clear();
            _pendingChildToParentMap.Clear();
        }
    }

    /// <inheritdoc/>
    public async Task LinkRelationship(Guid parentId, Guid childId, string? parentType = null, string? childType = null)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            // Track the pending relationship for validation at commit time
            lock (_pendingLock)
            {
                _pendingChildToParentMap[childId] = parentId;
            }

            await _auditService.LogEvent(
                AuditEventTypes.RelationshipCreated,
                parentId,
                new { 
                    ParentId = parentId, 
                    ChildId = childId, 
                    ParentType = parentType, 
                    ChildType = childType,
                    Operation = "LinkRelationship"
                });
            
            // Update parent's children list
            var parentRelationship = await _pgClient.From<EntityRelationship>()
                .Filter("owner_entity_id", "eq", parentId)
                .SingleOrDefault();
            
            if (parentRelationship == null)
            {
                parentRelationship = new EntityRelationship
                {
                    Id = Guid.NewGuid(),
                    OwnerEntityId = parentId,
                    LastUpdated = DateTime.UtcNow
                };
            }
            
            if (!parentRelationship.ChildrenIds.Contains(childId))
            {
                var childrenList = parentRelationship.ChildrenIds.ToList();
                childrenList.Add(childId);
                parentRelationship.ChildrenIds = childrenList.ToArray();
                
                if (childType != null)
                {
                    var childTypesDict = JsonSerializer.Deserialize<Dictionary<Guid, string>>(parentRelationship.ChildTypes) ?? new Dictionary<Guid, string>();
                    childTypesDict[childId] = childType;
                    parentRelationship.ChildTypes = JsonSerializer.Serialize(childTypesDict);
                }
                
                await Commit(parentRelationship);
            }
            
            // Update child's parent reference
            var childRelationship = await _pgClient.From<EntityRelationship>()
                .Filter("owner_entity_id", "eq", childId)
                .SingleOrDefault();
            
            if (childRelationship == null)
            {
                childRelationship = new EntityRelationship
                {
                    Id = Guid.NewGuid(),
                    OwnerEntityId = childId,
                    ParentId = parentId,
                    ParentType = parentType,
                    LastUpdated = DateTime.UtcNow
                };
                
                await Commit(childRelationship);
            }
            else if (childRelationship.ParentId != parentId)
            {
                childRelationship.ParentId = parentId;
                childRelationship.ParentType = parentType;
                
                await Commit(childRelationship);
            }
        }, "LinkRelationship", parentId, new { ParentId = parentId, ChildId = childId, ParentType = parentType, ChildType = childType });
    }

    /// <inheritdoc/>
    public async Task UnlinkRelationship(Guid parentId, Guid childId)
    {
        await ExecuteWithErrorHandling(async () =>
        {
            await _auditService.LogEvent(
                AuditEventTypes.RelationshipRemoved,
                parentId,
                new { 
                    ParentId = parentId, 
                    ChildId = childId,
                    Operation = "UnlinkRelationship"
                });
            
            // Update parent's children list
            var parentRelationship = await _pgClient.From<EntityRelationship>()
                .Filter("owner_entity_id", "eq", parentId)
                .SingleOrDefault();
            
            if (parentRelationship != null && parentRelationship.ChildrenIds.Contains(childId))
            {
                var childrenList = parentRelationship.ChildrenIds.ToList();
                childrenList.Remove(childId);
                parentRelationship.ChildrenIds = childrenList.ToArray();
                
                var childTypesDict = JsonSerializer.Deserialize<Dictionary<Guid, string>>(parentRelationship.ChildTypes) ?? new Dictionary<Guid, string>();
                childTypesDict.Remove(childId);
                parentRelationship.ChildTypes = JsonSerializer.Serialize(childTypesDict);
                
                await Commit(parentRelationship);
            }
            
            // Update child's parent reference
            var childRelationship = await _pgClient.From<EntityRelationship>()
                .Filter("owner_entity_id", "eq", childId)
                .SingleOrDefault();
            
            if (childRelationship != null && childRelationship.ParentId == parentId)
            {
                childRelationship.ParentId = null;
                childRelationship.ParentType = null;
                
                await Commit(childRelationship);
            }
        }, "UnlinkRelationship", parentId, new { ParentId = parentId, ChildId = childId });
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> Ancestors(Guid entityId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            await AuditRelationshipQuery("Ancestors", "GetAncestors", entityId);
            
            var ancestors = new List<Guid>();
            var currentId = entityId;
            
            while (true)
            {
                var parentId = await Parent(currentId);
                if (parentId == null)
                    break;
                    
                ancestors.Add(parentId.Value);
                currentId = parentId.Value;
                
                if (ancestors.Count > 100)
                {
                    await _auditService.LogEvent(
                        AuditEventTypes.DataValidationFailed,
                        entityId,
                        new { 
                            EntityId = entityId, 
                            AncestorCount = ancestors.Count,
                            Reason = "PossibleCircularReference",
                            Operation = "Ancestors"
                        });
                        
                    ErrorHandlingPolicy.LogExpectedError(
                        $"Possible circular reference detected while getting ancestors for entity {entityId}",
                        new { EntityId = entityId, AncestorCount = ancestors.Count });
                    break;
                }
            }
            
            return ancestors;
        }, "Ancestors", entityId, defaultValue: new List<Guid>());
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> Descendants(Guid entityId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            await AuditRelationshipQuery("Descendants", "GetDescendants", entityId);
            
            var descendants = new List<Guid>();
            var toProcess = new Queue<Guid>();
            toProcess.Enqueue(entityId);
            
            while (toProcess.Count > 0)
            {
                var currentId = toProcess.Dequeue();
                var children = await Children(currentId);
                
                foreach (var childId in children)
                {
                    if (!descendants.Contains(childId))
                    {
                        descendants.Add(childId);
                        toProcess.Enqueue(childId);
                    }
                }
                
                if (descendants.Count > 1000)
                {
                    await _auditService.LogEvent(
                        AuditEventTypes.DataValidationFailed,
                        entityId,
                        new { 
                            EntityId = entityId, 
                            DescendantCount = descendants.Count,
                            Reason = "LargeHierarchy",
                            Operation = "Descendants"
                        });
                        
                    ErrorHandlingPolicy.LogExpectedError(
                        $"Large hierarchy detected while getting descendants for entity {entityId}",
                        new { EntityId = entityId, DescendantCount = descendants.Count });
                    break;
                }
            }
            
            return descendants;
        }, "Descendants", entityId, defaultValue: new List<Guid>());
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

    private async Task<TComponent?> GetExistingComponent<TComponent>(TComponent component)
        where TComponent : class, IComponent, new()
    {
        if (component is not IVersionedComponent) return null;
        
        try
        {
            return await _pgClient.From<TComponent>()
                .Filter("id", "eq", component.Id)
                .Single();
        }
        catch
        {
            return null;
        }
    }

    private void HandleVersioning<TComponent>(TComponent component, TComponent? existing)
        where TComponent : class, IComponent, new()
    {
        if (component is not IVersionedComponent versionedComponent) return;

        if (existing != null)
        {
            var existingVersion = existing is IVersionedComponent existingVersioned 
                ? existingVersioned.Version ?? 0 
                : 0;
            versionedComponent.Version = existingVersion + 1;
        }
        else
        {
            versionedComponent.Version = 1;
        }
    }

    private async Task ExecuteDatabaseOperation<TComponent>(TComponent component, bool isInsert)
        where TComponent : class, IComponent, new()
    {
        // Ensure table exists before attempting any database operations
        await _tableManager.EnsureTableExists<TComponent>();
        
        if (component is IVersionedComponent)
        {
            await _pgClient.From<TComponent>().Upsert(component);
        }
        else if (isInsert)
        {
            try
            {
                await _pgClient.From<TComponent>().Insert(component);
            }
            catch
            {
                await _pgClient.From<TComponent>().Update(component);
            }
        }
        else
        {
            await _pgClient.From<TComponent>().Update(component);
        }
    }

    private async Task<T> ExecuteWithErrorHandling<T>(
        Func<Task<T>> operation,
        string operationName,
        Guid entityId,
        object? context = null,
        T? defaultValue = default)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            await SafeAuditError(AuditEventTypes.DatabaseError, entityId, ex, 
                new { Operation = operationName, Context = context });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to execute {operationName} for entity {entityId}",
                new { EntityId = entityId, Context = context });
            return defaultValue!;
        }
    }

    private async Task ExecuteWithErrorHandling(
        Func<Task> operation,
        string operationName,
        Guid entityId,
        object? context = null)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            await SafeAuditError(AuditEventTypes.DatabaseError, entityId, ex,
                new { Operation = operationName, Context = context });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to execute {operationName} for entity {entityId}",
                new { EntityId = entityId, Context = context });
        }
    }

    private async Task AuditRelationshipQuery(string operation, string queryType, Guid entityId, object? additionalData = null)
    {
        var auditData = new Dictionary<string, object>
        {
            { "EntityId", entityId },
            { "Operation", operation },
            { "QueryType", queryType }
        };

        if (additionalData != null)
        {
            foreach (var prop in additionalData.GetType().GetProperties())
            {
                auditData[prop.Name] = prop.GetValue(additionalData) ?? "";
            }
        }

        await _auditService.LogEvent(AuditEventTypes.RelationshipQueried, entityId, auditData);
    }

    private async Task AuditComponentOperation<TComponent>(TComponent component, TComponent? existing, string operation)
        where TComponent : class, IComponent
    {
        if (existing == null)
        {
            await _auditService.LogEvent(
                AuditEventTypes.ForComponent(typeof(TComponent).Name, "CREATED"),
                component.OwnerEntityId,
                new { ComponentId = component.Id, ComponentType = typeof(TComponent).Name });
        }
        else
        {
            await _auditService.LogChange(
                AuditEventTypes.ForComponent(typeof(TComponent).Name, "UPDATED"),
                component.OwnerEntityId,
                existing,
                component,
                new { ComponentId = component.Id, ComponentType = typeof(TComponent).Name });
        }
    }

    private void AddEventMetadata<TEvent>(TEvent @event) where TEvent : Events.IEvent
    {
        @event.Metadata["EmittedFrom"] = "DataContext";
        @event.Metadata["EmittedAt"] = DateTime.UtcNow.ToString("O");
    }

    /// <summary>
    /// Ensures that the core entity table exists in the database with the correct schema.
    /// This method validates the Entity table structure and creates or updates it as needed.
    /// - If table doesn't exist: Creates the table with proper schema
    /// - If table exists but missing columns: Adds missing columns
    /// - If table exists but has incompatible column types: Throws SchemaValidationException
    /// </summary>
    /// <exception cref="SchemaValidationException">Thrown when existing columns have incompatible data types requiring manual migration</exception>
    /// <exception cref="InvalidOperationException">Thrown when table management fails due to system errors</exception>
    public async Task EnsureEntityTableExists()
    {
        await ExecuteWithErrorHandling(async () =>
        {
            _logger.LogDebug("Ensuring Entity table exists in database");
            
            await _auditService.LogEvent(
                "TABLE_VALIDATION_STARTED",
                Guid.Empty,
                new { TableName = "entity", Operation = "EnsureEntityTableExists" });

            try
            {
                // Use the table manager to ensure the Entity table exists
                // Entity class serves as the IComponent-like structure for table management
                await EnsureEntityTableExistsInternal();
                
                _logger.LogInformation("Successfully validated Entity table schema");
                
                await _auditService.LogEvent(
                    "TABLE_VALIDATION_COMPLETED",
                    Guid.Empty,
                    new { TableName = "entity", Operation = "EnsureEntityTableExists", Status = "Success" });
            }
            catch (SchemaValidationException schemaEx)
            {
                _logger.LogError(schemaEx, "Entity table schema validation failed: {ErrorMessage}", schemaEx.Message);
                
                await _auditService.LogError(
                    "TABLE_SCHEMA_VALIDATION_FAILED",
                    Guid.Empty,
                    schemaEx,
                    new { 
                        TableName = "entity", 
                        Operation = "EnsureEntityTableExists",
                        ExpectedType = schemaEx.ExpectedType,
                        ActualType = schemaEx.ActualType,
                        FieldName = schemaEx.FieldName
                    });
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure Entity table exists: {ErrorMessage}", ex.Message);
                
                await _auditService.LogError(
                    AuditEventTypes.DatabaseError,
                    Guid.Empty,
                    ex,
                    new { TableName = "entity", Operation = "EnsureEntityTableExists" });
                throw;
            }
        }, "EnsureEntityTableExists", Guid.Empty, new { TableName = "entity" });
    }
    
    private async Task EnsureEntityTableExistsInternal()
    {
        // Since Entity doesn't implement IComponent, we need to handle it specially
        // Check if the entity table exists manually using PostgreSQL system tables
        var tableExists = await CheckEntityTableExists();
        
        if (!tableExists)
        {
            _logger.LogInformation("Creating entity table");
            await CreateEntityTable();
            return;
        }
        
        // Table exists - validate schema manually
        _logger.LogDebug("Entity table exists, validating schema");
        await ValidateEntityTableSchema();
    }
    
    private async Task<bool> CheckEntityTableExists()
    {
        var query = @"
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name = 'entity'
            )";
        
        try
        {
            var result = await _pgClient.ExecuteScalar<bool>(query);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if entity table exists");
            throw;
        }
    }
    
    private async Task CreateEntityTable()
    {
        var createTableSql = @"
            CREATE TABLE entity (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name TEXT NOT NULL DEFAULT '',
                parent_id UUID DEFAULT '00000000-0000-0000-0000-000000000000'::uuid,
                children_ids UUID[] DEFAULT '{}'
            )";
        
        try
        {
            await _pgClient.Execute(createTableSql);
            _logger.LogInformation("Successfully created entity table");
            
            // Create indexes for better performance
            await CreateEntityTableIndexes();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create entity table");
            throw;
        }
    }
    
    private async Task CreateEntityTableIndexes()
    {
        var indexes = new[]
        {
            "CREATE INDEX IF NOT EXISTS idx_entity_parent_id ON entity(parent_id)",
            "CREATE INDEX IF NOT EXISTS idx_entity_name ON entity(name)",
            "CREATE INDEX IF NOT EXISTS idx_entity_children_ids ON entity USING GIN(children_ids)"
        };
        
        foreach (var indexSql in indexes)
        {
            try
            {
                await _pgClient.Execute(indexSql);
                _logger.LogDebug("Created entity table index");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create entity table index, but continuing");
                // Don't fail on index creation errors
            }
        }
    }
    
    private async Task ValidateEntityTableSchema()
    {
        var query = @"
            SELECT 
                c.column_name,
                c.data_type,
                c.is_nullable = 'YES' as is_nullable,
                c.column_default
            FROM information_schema.columns c
            WHERE c.table_name = 'entity'
            ORDER BY c.ordinal_position";
        
        try
        {
            var actualColumns = await _pgClient.Query<dynamic>(query);
            var columnsList = actualColumns.ToList();
            
            // Define expected columns for Entity table
            var expectedColumns = new Dictionary<string, string>
            {
                { "id", "uuid" },
                { "name", "text" },
                { "parent_id", "uuid" },
                { "children_ids", "ARRAY" }
            };
            
            var missingColumns = new List<string>();
            
            foreach (var expected in expectedColumns)
            {
                var actualColumn = columnsList.FirstOrDefault(c => 
                    ((string)c.column_name).Equals(expected.Key, StringComparison.OrdinalIgnoreCase));
                
                if (actualColumn == null)
                {
                    _logger.LogDebug("Column {ColumnName} not found in entity table, will add it", expected.Key);
                    missingColumns.Add(expected.Key);
                }
                else
                {
                    // Basic type validation - could be enhanced for more strict checking
                    var actualType = ((string)actualColumn.data_type).ToLowerInvariant();
                    var expectedType = expected.Value.ToLowerInvariant();
                    
                    if (expectedType == "array" && !actualType.Contains("array"))
                    {
                        throw new SchemaValidationException(
                            "entity", 
                            expected.Key, 
                            "UUID[]", 
                            actualType);
                    }
                    else if (expectedType != "array" && !actualType.Contains(expectedType))
                    {
                        throw new SchemaValidationException(
                            "entity", 
                            expected.Key, 
                            expectedType.ToUpperInvariant(), 
                            actualType.ToUpperInvariant());
                    }
                }
            }
            
            // Add missing columns
            if (missingColumns.Any())
            {
                await AddMissingEntityColumns(missingColumns);
            }
        }
        catch (SchemaValidationException)
        {
            throw; // Re-throw schema validation exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate entity table schema");
            throw;
        }
    }
    
    private async Task AddMissingEntityColumns(List<string> missingColumns)
    {
        _logger.LogInformation("Adding {Count} missing columns to entity table", missingColumns.Count);
        
        var columnDefinitions = new Dictionary<string, string>
        {
            { "id", "id UUID PRIMARY KEY DEFAULT gen_random_uuid()" },
            { "name", "name TEXT NOT NULL DEFAULT ''" },
            { "parent_id", "parent_id UUID DEFAULT '00000000-0000-0000-0000-000000000000'::uuid" },
            { "children_ids", "children_ids UUID[] DEFAULT '{}'" }
        };
        
        foreach (var columnName in missingColumns)
        {
            if (columnDefinitions.TryGetValue(columnName, out var columnDef))
            {
                var alterSql = $@"
                    DO $$ 
                    BEGIN 
                        BEGIN
                            ALTER TABLE entity ADD COLUMN {columnDef};
                        EXCEPTION
                            WHEN duplicate_column THEN RAISE NOTICE 'column {columnName} already exists in entity.';
                        END;
                    END;
                    $$";
                
                try
                {
                    await _pgClient.Execute(alterSql);
                    _logger.LogDebug("Ensured column {ColumnName} exists in entity table", columnName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ensure column {ColumnName} exists in entity table", columnName);
                    throw;
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteInTransaction<T>(Func<Task<T>> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        return await ExecuteTransactionWithRecovery(async () =>
        {
            var connection = await _pgClient.GetConnectionAsync();
            
            await SafeAuditEvent(
                AuditEventTypes.TransactionStarted,
                Guid.Empty,
                new { Operation = "ExecuteInTransaction", OperationType = typeof(T).Name });

            using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                
                await SafeAuditEvent(
                    AuditEventTypes.TransactionCommitted,
                    Guid.Empty,
                    new { Operation = "ExecuteInTransaction", OperationType = typeof(T).Name });
                
                _logger.LogDebug("Transaction committed successfully for operation returning {OperationType}", typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                await SafeRollbackTransaction(transaction, ex);
                
                await SafeAuditError(
                    AuditEventTypes.TransactionRolledBack,
                    Guid.Empty,
                    ex,
                    new { Operation = "ExecuteInTransaction", OperationType = typeof(T).Name });
                
                _logger.LogError(ex, "Transaction rolled back due to error in operation returning {OperationType}", typeof(T).Name);
                throw;
            }
        }, typeof(T).Name);
    }

    /// <inheritdoc/>
    public async Task ExecuteInTransaction(Func<Task> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        await ExecuteTransactionWithRecovery(async () =>
        {
            var connection = await _pgClient.GetConnectionAsync();
            
            await SafeAuditEvent(
                AuditEventTypes.TransactionStarted,
                Guid.Empty,
                new { Operation = "ExecuteInTransaction", OperationType = "void" });

            using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                await operation();
                await transaction.CommitAsync();
                
                await SafeAuditEvent(
                    AuditEventTypes.TransactionCommitted,
                    Guid.Empty,
                    new { Operation = "ExecuteInTransaction", OperationType = "void" });
                
                _logger.LogDebug("Transaction committed successfully for void operation");
                return true; // Dummy return for consistency
            }
            catch (Exception ex)
            {
                await SafeRollbackTransaction(transaction, ex);
                
                await SafeAuditError(
                    AuditEventTypes.TransactionRolledBack,
                    Guid.Empty,
                    ex,
                    new { Operation = "ExecuteInTransaction", OperationType = "void" });
                
                _logger.LogError(ex, "Transaction rolled back due to error in void operation");
                throw;
            }
        }, "void");
    }

    /// <summary>
    /// Executes a transaction with PostgreSQL-specific error recovery handling.
    /// This method handles the "current transaction is aborted" error by properly recovering the connection.
    /// </summary>
    private async Task<T> ExecuteTransactionWithRecovery<T>(Func<Task<T>> transactionOperation, object operationType)
    {
        try
        {
            return await transactionOperation();
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "25P02") // Current transaction is aborted
        {
            _logger.LogWarning(pgEx, "PostgreSQL transaction aborted, attempting recovery. Operation: {OperationType}", operationType);
            
            // Log the recovery attempt
            await SafeAuditEvent(
                "TRANSACTION_RECOVERY_ATTEMPTED", 
                Guid.Empty,
                new { 
                    OperationType = operationType,
                    PostgreSQLErrorCode = pgEx.SqlState,
                    ErrorMessage = pgEx.Message 
                });

            // Force connection recovery by getting a fresh connection
            try
            {
                // Force a fresh connection by calling GetConnectionAsync which should handle recovery
                var newConnection = await _pgClient.GetConnectionAsync();
                _logger.LogInformation("PostgreSQL connection recovered successfully for operation: {OperationType}", operationType);
                
                // Retry the operation once with fresh connection
                return await transactionOperation();
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "Transaction recovery failed for operation: {OperationType}", operationType);
                
                await SafeAuditError(
                    AuditEventTypes.DatabaseError,
                    Guid.Empty,
                    retryEx,
                    new { 
                        OperationType = operationType,
                        RecoveryAttempted = true,
                        OriginalError = pgEx.Message,
                        RetryError = retryEx.Message
                    });
                
                throw;
            }
        }
        catch (Exception ex)
        {
            await SafeAuditError(
                AuditEventTypes.DatabaseError,
                Guid.Empty,
                ex,
                new { OperationType = operationType });

            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to execute operation in transaction: {operationType}",
                new { OperationType = operationType });
            throw;
        }
    }

    /// <summary>
    /// Safely performs a transaction rollback, handling cases where the transaction is already aborted.
    /// </summary>
    private async Task SafeRollbackTransaction(System.Data.Common.DbTransaction transaction, Exception originalException)
    {
        try
        {
            await transaction.RollbackAsync();
            _logger.LogDebug("Transaction rolled back successfully");
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "25P02")
        {
            // Transaction is already aborted, no need to rollback
            _logger.LogDebug("Transaction was already aborted, skipping explicit rollback");
        }
        catch (Exception rollbackEx)
        {
            _logger.LogError(rollbackEx, "Failed to rollback transaction. Original error: {OriginalError}", originalException.Message);
            // Don't throw rollback exceptions - let the original exception propagate
        }
    }

    /// <summary>
    /// Safely performs audit operations without allowing audit failures to break business operations.
    /// This method ensures that audit operations are isolated from the main transaction flow.
    /// </summary>
    private async Task SafeAuditError(string eventType, Guid entityId, Exception exception, object? context = null)
    {
        try
        {
            // Use fire-and-forget approach for audit operations
            await _auditService.LogError(eventType, entityId, exception, context);
        }
        catch (Exception auditEx)
        {
            // Never let audit failures break business operations
            // Just log the audit failure and continue
            _logger.LogError(auditEx, 
                "Audit operation failed for event type {EventType}, entity {EntityId}. Original exception: {OriginalException}", 
                eventType, entityId, exception.Message);
        }
    }

    /// <summary>
    /// Safely performs audit events without allowing audit failures to break business operations.
    /// </summary>
    private async Task SafeAuditEvent(string eventType, Guid entityId, object? eventData = null)
    {
        try
        {
            await _auditService.LogEvent(eventType, entityId, eventData);
        }
        catch (Exception auditEx)
        {
            // Never let audit failures break business operations
            _logger.LogError(auditEx, 
                "Audit event logging failed for event type {EventType}, entity {EntityId}", 
                eventType, entityId);
        }
    }
}