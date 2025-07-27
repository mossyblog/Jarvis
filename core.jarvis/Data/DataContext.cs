using System.Text.Json;
using core.jarvis.Data.Components;
using core.jarvis.Data.GraphQL;
using core.jarvis.Data.Query;
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
    
    public DataContext(
        IServiceProvider serviceProvider,
        IComponentQueryHandlerRegistry queryRegistry,
        IPgClient pgClient,
        ILogger<DataContext> logger,
        IAuditService auditService,
        Events.IEventEmitter eventEmitter)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _queryRegistry = queryRegistry ?? throw new ArgumentNullException(nameof(queryRegistry));
        _pgClient = pgClient ?? throw new ArgumentNullException(nameof(pgClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _eventEmitter = eventEmitter ?? throw new ArgumentNullException(nameof(eventEmitter));
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
        }, "Commit", component.OwnerEntityId, new { ComponentType = typeof(TComponent).Name, ComponentId = component.Id });
    }
    
    
    /// <inheritdoc/>
    public async Task<bool> TryCommit<TComponent>(TComponent component) 
        where TComponent : class, IComponent, new()
    {
        try
        {
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
            return true;
        }
        catch (Exception ex)
        {
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
        }
        catch (Exception ex)
        {
            // Audit snapshot failure
            await _auditService.LogError(
                AuditEventTypes.SnapshotFailed,
                component.OwnerEntityId,
                ex,
                new { 
                    ComponentType = component.GetType().Name, 
                    ComponentId = component.Id, 
                    Operation = operation 
                });
                
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

    /// <inheritdoc/>
    public async Task LinkRelationship(Guid parentId, Guid childId, string? parentType = null, string? childType = null)
    {
        await ExecuteWithErrorHandling(async () =>
        {
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
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                entityId,
                ex,
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
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                entityId,
                ex,
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

    /// <inheritdoc/>
    public async Task EnsureEntityTableExists()
    {
        // TODO: Finish this...
        await Task.CompletedTask;
    }
}