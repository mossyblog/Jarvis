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
    
    public DataContext(
        IServiceProvider serviceProvider,
        IComponentQueryHandlerRegistry queryRegistry,
        IPgClient pgClient,
        ILogger<DataContext> logger,
        IAuditService auditService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _queryRegistry = queryRegistry ?? throw new ArgumentNullException(nameof(queryRegistry));
        _pgClient = pgClient ?? throw new ArgumentNullException(nameof(pgClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
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

    public async Task Commit<TComponent>(TComponent component) 
        where TComponent : class, IComponent, new()
    {
        try
        {
            component.UpdatedAt = DateTime.UtcNow;
            
            // Try to get existing component to determine if this is an insert or update
            TComponent? existing = null;
            try
            {
                existing = await _pgClient.From<TComponent>()
                    .Filter("id", "eq", component.Id)
                    .Single();
            }
            catch
            {
                // Component doesn't exist yet - this is an insert
            }
            
            // Check if component supports versioning
            if (component is IVersionedComponent versionedComponent)
            {
                if (existing != null)
                {
                    // This is an update - capture snapshot of existing state BEFORE any changes
                    await Snapshot(existing, "UPDATE");
                    
                    // For version-based concurrency, always increment from the existing version
                    // This ensures we don't accidentally skip versions
                    if (existing is IVersionedComponent existingVersioned)
                    {
                        versionedComponent.Version = (existingVersioned.Version ?? 0) + 1;
                    }
                    else
                    {
                        versionedComponent.Version = (versionedComponent.Version ?? 0) + 1;
                    }
                }
                else
                {
                    // This is an insert - set initial version
                    versionedComponent.Version = 1;
                }
            }
            
            await _pgClient.From<TComponent>().Upsert(component);
            
            // Audit the operation
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
            
            // For new inserts with versioning, capture initial snapshot after save
            if (existing == null && component is IVersionedComponent)
            {
                await Snapshot(component, "CREATE");
            }
        }
        catch (Exception ex)
        {
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                component.OwnerEntityId,
                ex,
                new { Operation = "Commit", ComponentType = typeof(TComponent).Name, ComponentId = component.Id });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to save component {typeof(TComponent).Name} for entity {component.OwnerEntityId}",
                new { ComponentType = typeof(TComponent).Name, EntityId = component.OwnerEntityId, ComponentId = component.Id });
        }
    }
    
    
    /// <inheritdoc/>
    public async Task<bool> TryCommit<TComponent>(TComponent component) 
        where TComponent : class, IComponent, new()
    {
        try
        {
            var originalUpdatedAt = component.UpdatedAt;
            component.UpdatedAt = DateTime.UtcNow;

            // Try to get existing record by ID
            TComponent? existing = null;
            try
            {
                existing = await _pgClient.From<TComponent>()
                    .Filter("id", "eq", component.Id)
                    .Single();
            }
            catch
            {
                // Record doesn't exist, which is fine for inserts
            }

            if (existing == null)
            {
                // New record - check if versioning is supported
                if (component is IVersionedComponent versionedComponent)
                {
                    versionedComponent.Version = 1;
                }
                
                // Use Upsert to handle both insert and update cases
                await _pgClient.From<TComponent>().Upsert(component);
                
                // Audit the new component creation
                await _auditService.LogEvent(
                    AuditEventTypes.ForComponent(typeof(TComponent).Name, "CREATED"),
                    component.OwnerEntityId,
                    new { ComponentId = component.Id, ComponentType = typeof(TComponent).Name });
                
                // Capture initial snapshot after insert
                if (component is IVersionedComponent)
                {
                    await Snapshot(component, "CREATE");
                }
                
                return true;
            }

            // For versioned components, use version-based concurrency control
            if (component is IVersionedComponent versionedComp && existing is IVersionedComponent existingVersioned)
            {
                // Check if version matches - if not, it's a concurrency conflict
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
                
                // Snapshot the existing state before update
                await Snapshot(existing, "UPDATE");
                
                // Increment version for update
                versionedComp.Version = (existingVersioned.Version ?? 0) + 1;
            }
            else
            {
                // For non-versioned components, fall back to timestamp-based concurrency
                var timeDiff = Math.Abs((existing.UpdatedAt - originalUpdatedAt).TotalMilliseconds);
                
                // Debug logging removed - timestamp concurrency check performed
                
                // If timestamps differ by more than 10ms, it's a concurrency conflict
                if (timeDiff > 10)
                {
                    await _auditService.LogEvent(
                        AuditEventTypes.ComponentConcurrencyConflict,
                        component.OwnerEntityId,
                        new { 
                            ComponentType = typeof(TComponent).Name, 
                            ComponentId = component.Id, 
                            ExpectedTimestamp = originalUpdatedAt, 
                            ActualTimestamp = existing.UpdatedAt, 
                            DifferenceMs = timeDiff 
                        });
                        
                    ErrorHandlingPolicy.LogExpectedError(
                        $"Timestamp concurrency conflict for {typeof(TComponent).Name} ID {component.Id}. Expected: {originalUpdatedAt:O}, Actual: {existing.UpdatedAt:O}, Diff: {timeDiff}ms",
                        new { ComponentType = typeof(TComponent).Name, ComponentId = component.Id, ExpectedTimestamp = originalUpdatedAt, ActualTimestamp = existing.UpdatedAt, DifferenceMs = timeDiff });
                    return false;
                }
            }
            
            // Safe to update
            await _pgClient.From<TComponent>().Upsert(component);
            
            // Audit the successful update
            await _auditService.LogChange(
                AuditEventTypes.ForComponent(typeof(TComponent).Name, "UPDATED"),
                component.OwnerEntityId,
                existing,
                component,
                new { ComponentId = component.Id, ComponentType = typeof(TComponent).Name });
            
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
            return false; // This line will never be reached but satisfies the compiler
        }
    }

    /// <inheritdoc/>
    public async Task Remove<TComponent>(Guid entityId) 
        where TComponent : class, IComponent, new()
    {
            
        try
        {
            // Audit the deletion attempt
            await _auditService.LogEvent(
                AuditEventTypes.ForComponent(typeof(TComponent).Name, "DELETED"),
                entityId,
                new { ComponentType = typeof(TComponent).Name, Operation = "Remove" });
            
            // Remove by owner_entity_id
            await _pgClient.From<TComponent>()
                .Filter("owner_entity_id", "eq", entityId)
                .Delete();
            
            await _pgClient.From<TComponent>()
                .Filter("id", "eq", entityId)
                .Delete();
        
        }
        catch (Exception ex)
        {
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                entityId,
                ex,
                new { Operation = "Remove", ComponentType = typeof(TComponent).Name });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to remove {typeof(TComponent).Name} components for entity {entityId}",
                new { ComponentType = typeof(TComponent).Name, EntityId = entityId });
        }
    }

    

    /// <inheritdoc/>
    public async Task Insert<TModel>(TModel model) 
        where TModel : class, new()
    {
        try
        {
            await _pgClient.From<TModel>().Insert(model);
            
            // Audit the successful insertion
            Guid entityId = Guid.Empty;
            if (model is IComponent component)
            {
                entityId = component.OwnerEntityId;
            }
            
            await _auditService.LogEvent(
                AuditEventTypes.ForComponent(typeof(TModel).Name, "INSERTED"),
                entityId,
                new { ModelType = typeof(TModel).Name, Operation = "Insert" });
        }
        catch (Exception ex)
        {
            // Audit the error
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                Guid.Empty,
                ex,
                new { Operation = "Insert", ModelType = typeof(TModel).Name });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to insert {typeof(TModel).Name}",
                new { ModelType = typeof(TModel).Name, ModelData = model });
        }
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
                existing.UpdatedAt = DateTime.UtcNow;
                
                await _pgClient.From<ComponentSnapshots>().Update(existing);
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
                    UpdatedAt = DateTime.UtcNow
                };
                
                await _pgClient.From<ComponentSnapshots>().Insert(record);
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
        try
        {
            // Audit the relationship query
            await _auditService.LogEvent(
                AuditEventTypes.RelationshipQueried,
                entityId,
                new { EntityId = entityId, Operation = "Parent", QueryType = "GetParent" });
            
            var relationship = await _pgClient.From<EntityRelationship>()
                .Filter("owner_entity_id", "eq", entityId)
                .SingleOrDefault();
            
            return relationship?.ParentId;
        }
        catch (InvalidOperationException)
        {
            // No relationship found
            return null;
        }
        catch (Exception ex)
        {
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                entityId,
                ex,
                new { Operation = "Parent", EntityId = entityId });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to get parent for entity {entityId}",
                new { EntityId = entityId });
            return null; // This line will never be reached but satisfies the compiler
        }
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> Children(Guid entityId)
    {
        try
        {
            // Audit the relationship query
            await _auditService.LogEvent(
                AuditEventTypes.RelationshipQueried,
                entityId,
                new { EntityId = entityId, Operation = "Children", QueryType = "GetChildren" });
            
            var relationship = await _pgClient.From<EntityRelationship>()
                .Filter("owner_entity_id", "eq", entityId)
                .SingleOrDefault();
            
            return relationship?.ChildrenIds?.ToList() ?? new List<Guid>();
        }
        catch (InvalidOperationException)
        {
            // No relationship found
            return new List<Guid>();
        }
        catch (Exception ex)
        {
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                entityId,
                ex,
                new { Operation = "Children", EntityId = entityId });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to get children for entity {entityId}",
                new { EntityId = entityId });
            return new List<Guid>(); // This line will never be reached but satisfies the compiler
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ChildOf(Guid childId, Guid parentId)
    {
        try
        {
            // Audit the relationship query
            await _auditService.LogEvent(
                AuditEventTypes.RelationshipQueried,
                childId,
                new { ChildId = childId, ParentId = parentId, Operation = "ChildOf", QueryType = "CheckParentChild" });
            
            var childRelationship = await _pgClient.From<EntityRelationship>()
                .Filter("owner_entity_id", "eq", childId)
                .SingleOrDefault();
            
            return childRelationship?.ParentId == parentId;
        }
        catch (InvalidOperationException)
        {
            // No relationship found
            return false;
        }
        catch (Exception ex)
        {
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                childId,
                ex,
                new { Operation = "ChildOf", ChildId = childId, ParentId = parentId });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to check relationship between {childId} and {parentId}",
                new { ChildId = childId, ParentId = parentId });
            return false; // This line will never be reached but satisfies the compiler
        }
    }

    /// <inheritdoc/>
    public async Task LinkRelationship(Guid parentId, Guid childId, string? parentType = null, string? childType = null)
    {
        try
        {
            // Audit the relationship creation attempt
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
                // Create new relationship for parent
                parentRelationship = new EntityRelationship
                {
                    Id = Guid.NewGuid(),
                    OwnerEntityId = parentId,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            
            // Add child if not already present
            if (!parentRelationship.ChildrenIds.Contains(childId))
            {
                // Convert array to list, add, and convert back
                var childrenList = parentRelationship.ChildrenIds.ToList();
                childrenList.Add(childId);
                parentRelationship.ChildrenIds = childrenList.ToArray();
                
                if (childType != null)
                {
                    // Deserialize, update, and re-serialize the child types
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
                // Create new relationship for child
                childRelationship = new EntityRelationship
                {
                    Id = Guid.NewGuid(),
                    OwnerEntityId = childId,
                    ParentId = parentId,
                    ParentType = parentType,
                    UpdatedAt = DateTime.UtcNow
                };
                
                await Commit(childRelationship);
            }
            else if (childRelationship.ParentId != parentId)
            {
                // Update existing relationship
                childRelationship.ParentId = parentId;
                childRelationship.ParentType = parentType;
                
                await Commit(childRelationship);
            }
        }
        catch (Exception ex)
        {
            // Audit the error
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                parentId,
                ex,
                new { 
                    Operation = "LinkRelationship", 
                    ParentId = parentId, 
                    ChildId = childId,
                    ParentType = parentType,
                    ChildType = childType
                });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to add relationship between parent {parentId} and child {childId}",
                new { ParentId = parentId, ChildId = childId, ParentType = parentType, ChildType = childType });
        }
    }

    /// <inheritdoc/>
    public async Task UnlinkRelationship(Guid parentId, Guid childId)
    {
        try
        {
            // Audit the relationship removal attempt
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
                // Convert array to list, remove, and convert back
                var childrenList = parentRelationship.ChildrenIds.ToList();
                childrenList.Remove(childId);
                parentRelationship.ChildrenIds = childrenList.ToArray();
                
                // Deserialize, update, and re-serialize the child types
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
        }
        catch (Exception ex)
        {
            // Audit the error
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                parentId,
                ex,
                new { 
                    Operation = "UnlinkRelationship", 
                    ParentId = parentId, 
                    ChildId = childId
                });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to remove relationship between parent {parentId} and child {childId}",
                new { ParentId = parentId, ChildId = childId });
        }
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> Ancestors(Guid entityId)
    {
        try
        {
            // Audit the hierarchy query
            await _auditService.LogEvent(
                AuditEventTypes.RelationshipQueried,
                entityId,
                new { EntityId = entityId, Operation = "Ancestors", QueryType = "GetAncestors" });
            
            var ancestors = new List<Guid>();
            var currentId = entityId;
            
            // Traverse up the hierarchy
            while (true)
            {
                var parentId = await Parent(currentId);
                if (parentId == null)
                    break;
                    
                ancestors.Add(parentId.Value);
                currentId = parentId.Value;
                
                // Prevent infinite loops
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
        }
        catch (Exception ex)
        {
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                entityId,
                ex,
                new { Operation = "Ancestors", EntityId = entityId });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to get ancestors for entity {entityId}",
                new { EntityId = entityId });
            return new List<Guid>(); // This line will never be reached but satisfies the compiler
        }
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> Descendants(Guid entityId)
    {
        try
        {
            // Audit the hierarchy query
            await _auditService.LogEvent(
                AuditEventTypes.RelationshipQueried,
                entityId,
                new { EntityId = entityId, Operation = "Descendants", QueryType = "GetDescendants" });
            
            var descendants = new List<Guid>();
            var toProcess = new Queue<Guid>();
            toProcess.Enqueue(entityId);
            
            // Breadth-first traversal
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
                
                // Prevent infinite loops
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
        }
        catch (Exception ex)
        {
            await _auditService.LogError(
                AuditEventTypes.DatabaseError,
                entityId,
                ex,
                new { Operation = "Descendants", EntityId = entityId });
                
            ErrorHandlingPolicy.LogAndRethrow(
                ex,
                $"Failed to get descendants for entity {entityId}",
                new { EntityId = entityId });
            return new List<Guid>(); // This line will never be reached but satisfies the compiler
        }
    }
}