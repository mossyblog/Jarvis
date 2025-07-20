# DataContext in Jarvis: Technical Whitepaper

## Abstract

This whitepaper provides a comprehensive technical analysis of the DataContext implementation in the Jarvis Entity Component System (ECS) framework. DataContext serves as the primary abstraction layer for all data operations, orchestrating component handlers, managing transactions, enforcing audit trails, and providing a unified interface for entity-component interactions. We examine its architecture, design patterns, performance characteristics, and role in enabling a plugin-based, handler-driven development model.

## Table of Contents

1. [Introduction](#introduction)
2. [Architectural Philosophy](#architectural-philosophy)
3. [Core Components](#core-components)
4. [Operation Flow](#operation-flow)
5. [Handler System Integration](#handler-system-integration)
6. [Transaction Management](#transaction-management)
7. [Audit and Compliance](#audit-and-compliance)
8. [Query System](#query-system)
9. [Performance Considerations](#performance-considerations)
10. [Testing and Isolation](#testing-and-isolation)
11. [Future Evolution](#future-evolution)
12. [Conclusion](#conclusion)

## Introduction

The DataContext is the heart of the Jarvis framework, providing a high-level abstraction over database operations while maintaining the flexibility and power needed for complex business logic. It embodies the Entity Component System pattern where data (Components) is separated from behavior (Handlers), enabling a highly modular and testable architecture.

### Key Design Principles

1. **Handler-Centric Operations**: All business logic flows through handlers
2. **Immutable Components**: Components are records with init-only properties
3. **Automatic Audit Trails**: Every operation is tracked for compliance
4. **Transaction Safety**: ACID guarantees with explicit transaction boundaries
5. **Type Safety**: Generic constraints ensure compile-time correctness

## Architectural Philosophy

### Entity Component System (ECS) Pattern

The DataContext implements a pure ECS pattern:

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│   Entity    │────▶│  Component   │◀────│   Handler   │
│   (GUID)    │     │    (Data)    │     │  (Logic)    │
└─────────────┘     └──────────────┘     └─────────────┘
       │                    │                     │
       └────────────────────┴─────────────────────┘
                           │
                    ┌──────▼──────┐
                    │ DataContext │
                    └─────────────┘
```

### Separation of Concerns

```csharp
// Component: Pure Data
public record InvoiceComponent : BaseComponent, IComponent
{
    public string InvoiceNumber { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; }
}

// Handler: Business Logic
public class InvoiceHandler : ComponentHandler<InvoiceComponent>
{
    public async Task<InvoiceComponent> ProcessPayment(decimal amount)
    {
        var invoice = await Ensure();
        return await Update(invoice with 
        { 
            Status = "Paid",
            Amount = invoice.Amount - amount 
        });
    }
}

// DataContext: Orchestration
var handler = dataContext.For<InvoiceHandler>(entityId);
await handler.ProcessPayment(100.00m);
```

## Core Components

### IDataContext Interface

The primary interface defines all available operations:

```csharp
public interface IDataContext
{
    // Handler Resolution
    THandler For<THandler>(Guid entityId) where THandler : IComponentHandler;
    THandler For<THandler, TComponent>(Guid entityId) 
        where THandler : IComponentHandler<TComponent> 
        where TComponent : IComponent;
    
    // Component Operations
    Task Commit<T>(T component) where T : IComponent;
    Task<bool> TryCommit<T>(T component) where T : IComponent, IVersionedComponent;
    Task<bool> Update<T>(Guid entityId, Func<T?, T?> updateFunc) where T : class, IComponent;
    Task Remove<T>(Guid entityId) where T : IComponent;
    
    // Relationship Management
    Task LinkRelationship(Guid parentEntityId, Guid childEntityId, 
        string parentRole = "Parent", string childRole = "Child");
    Task UnlinkRelationship(Guid parentEntityId, Guid childEntityId);
    
    // Query Operations
    IEntityQuery CreateQuery();
    Task<List<Guid>> Query<T>(Expression<Func<T, bool>>? filter = null) where T : IComponent;
    
    // Transaction Support
    Task<TResult> InTransaction<TResult>(Func<IDataContext, Task<TResult>> operation);
    
    // Hierarchy Navigation
    Task<Guid?> Parent(Guid childEntityId);
    Task<List<Guid>> Children(Guid parentEntityId);
    Task<List<Guid>> Ancestors(Guid entityId);
    Task<List<Guid>> Descendants(Guid entityId);
}
```

### DataContext Implementation

The concrete implementation orchestrates all operations:

```csharp
public class DataContext : IDataContext
{
    private readonly IPgClient _pgClient;
    private readonly IComponentHandlerRegistry _handlerRegistry;
    private readonly IAuditService _auditService;
    private readonly ILogger<DataContext> _logger;
    private readonly EventSubscriptionManager _eventManager;
    
    public DataContext(
        IPgClient pgClient,
        IComponentHandlerRegistry handlerRegistry,
        IAuditService auditService,
        ILogger<DataContext> logger,
        EventSubscriptionManager eventManager)
    {
        _pgClient = pgClient;
        _handlerRegistry = handlerRegistry;
        _auditService = auditService;
        _logger = logger;
        _eventManager = eventManager;
    }
}
```

## Operation Flow

### Component Commit Flow

The commit operation demonstrates the full DataContext flow:

```csharp
public async Task Commit<T>(T component) where T : IComponent
{
    // 1. Validation
    ArgumentNullException.ThrowIfNull(component);
    ValidateComponent(component);
    
    // 2. Audit - Log intent
    await _auditService.LogComponentOperation(
        component.OwnerEntityId,
        typeof(T).Name,
        "COMMIT_INITIATED",
        component);
    
    // 3. Check for existing component
    var existing = await _pgClient.From<T>()
        .Filter("owner_entity_id", "eq", component.OwnerEntityId)
        .SingleOrDefault();
    
    // 4. Perform operation
    if (existing == null)
    {
        // 4a. Insert new component
        await _pgClient.From<T>().Insert(component);
        
        // 5a. Audit creation
        await _auditService.LogComponentCreated(
            component.OwnerEntityId,
            component);
    }
    else
    {
        // 4b. Update existing component
        await _auditService.LogChange(
            component.OwnerEntityId,
            typeof(T).Name,
            existing,
            component);
        
        await _pgClient.From<T>()
            .Match(existing)
            .Update(component);
        
        // 5b. Audit update
        await _auditService.LogComponentUpdated(
            component.OwnerEntityId,
            existing,
            component);
    }
    
    // 6. Event emission
    await _eventManager.EmitAsync(new ComponentCommittedEvent<T>(component));
    
    // 7. Create snapshot for versioned components
    if (component is IVersionedComponent versioned)
    {
        await CreateSnapshot(versioned);
    }
}
```

### Handler Resolution Flow

Handler resolution is central to the DataContext pattern:

```csharp
public THandler For<THandler>(Guid entityId) where THandler : IComponentHandler
{
    // 1. Validate entity ID
    if (entityId == Guid.Empty)
        throw new ArgumentException("Entity ID cannot be empty", nameof(entityId));
    
    // 2. Resolve handler from registry
    var handler = _handlerRegistry.Resolve<THandler>();
    
    // 3. Initialize handler with context
    handler.Initialize(entityId, this, _pgClient);
    
    // 4. Audit handler access
    _auditService.LogHandlerAccess(entityId, typeof(THandler).Name);
    
    // 5. Return initialized handler
    return handler;
}
```

## Handler System Integration

### Handler Lifecycle

DataContext manages the complete handler lifecycle:

```
┌──────────────┐
│   Request    │
└──────┬───────┘
       │
┌──────▼───────────────┐
│ DataContext.For<T>() │
└──────┬───────────────┘
       │
┌──────▼────────────────┐     ┌─────────────────┐
│ Handler Resolution    │────▶│ DI Container    │
└──────┬────────────────┘     └─────────────────┘
       │
┌──────▼────────────────┐
│ Handler Initialization│
│ - Set EntityId        │
│ - Inject DataContext  │
│ - Inject PgClient     │
└──────┬────────────────┘
       │
┌──────▼────────────────┐
│ Handler Ready for Use │
└───────────────────────┘
```

### Handler Base Classes

DataContext works with handler base classes that provide common functionality:

```csharp
public abstract class ComponentHandler<TComponent> : IComponentHandler<TComponent>
    where TComponent : class, IComponent, new()
{
    protected Guid EntityId { get; private set; }
    protected IDataContext DataContext { get; private set; }
    protected IPgClient PgClient { get; private set; }
    
    public void Initialize(Guid entityId, IDataContext dataContext, IPgClient pgClient)
    {
        EntityId = entityId;
        DataContext = dataContext;
        PgClient = pgClient;
    }
    
    protected async Task<TComponent?> Get()
    {
        return await PgClient.From<TComponent>()
            .Filter("owner_entity_id", "eq", EntityId)
            .SingleOrDefault();
    }
    
    protected async Task<TComponent> Ensure()
    {
        var component = await Get();
        if (component == null)
        {
            component = new TComponent { OwnerEntityId = EntityId };
            await DataContext.Commit(component);
        }
        return component;
    }
    
    protected async Task<TComponent> Update(TComponent component)
    {
        await DataContext.Commit(component);
        return component;
    }
}
```

## Transaction Management

### Transaction Boundaries

DataContext provides explicit transaction management:

```csharp
public async Task<TResult> InTransaction<TResult>(
    Func<IDataContext, Task<TResult>> operation)
{
    // 1. Create transaction scope
    await using var transaction = await _pgClient.BeginTransactionAsync();
    
    try
    {
        // 2. Create transactional DataContext
        var transactionalContext = new TransactionalDataContext(
            this, 
            transaction,
            _pgClient,
            _handlerRegistry,
            _auditService,
            _logger,
            _eventManager);
        
        // 3. Execute operation
        var result = await operation(transactionalContext);
        
        // 4. Commit transaction
        await transaction.CommitAsync();
        
        // 5. Emit transaction completed event
        await _eventManager.EmitAsync(
            new TransactionCompletedEvent(result));
        
        return result;
    }
    catch (Exception ex)
    {
        // 6. Rollback on error
        await transaction.RollbackAsync();
        
        // 7. Audit failure
        await _auditService.LogTransactionFailure(ex);
        
        throw;
    }
}
```

### Transactional DataContext

A special implementation ensures all operations within a transaction use the same connection:

```csharp
internal class TransactionalDataContext : IDataContext
{
    private readonly IDataContext _parent;
    private readonly IDbTransaction _transaction;
    
    // All operations delegated to parent with transaction context
    public async Task Commit<T>(T component) where T : IComponent
    {
        // Uses transaction's connection
        await _parent.CommitWithTransaction(component, _transaction);
    }
}
```

## Audit and Compliance

### Comprehensive Audit Trail

Every DataContext operation generates audit events:

```csharp
public enum AuditEventTypes
{
    // Component lifecycle
    ComponentCreated = "COMPONENT_CREATED",
    ComponentUpdated = "COMPONENT_UPDATED", 
    ComponentDeleted = "COMPONENT_DELETED",
    
    // Relationships
    RelationshipCreated = "RELATIONSHIP_CREATED",
    RelationshipRemoved = "RELATIONSHIP_REMOVED",
    RelationshipQueried = "RELATIONSHIP_QUERIED",
    
    // Transactions
    TransactionStarted = "TRANSACTION_STARTED",
    TransactionCommitted = "TRANSACTION_COMMITTED",
    TransactionRolledBack = "TRANSACTION_ROLLED_BACK",
    
    // Errors
    ValidationError = "VALIDATION_ERROR",
    DatabaseError = "DATABASE_ERROR",
    ConcurrencyConflict = "CONCURRENCY_CONFLICT"
}
```

### Audit Integration Points

```csharp
private async Task AuditComponentOperation<T>(
    string operation, 
    Guid entityId, 
    T? oldValue, 
    T? newValue) where T : IComponent
{
    var auditEvent = new AuditEvent
    {
        Id = Guid.NewGuid(),
        EventType = $"{typeof(T).Name.ToUpper()}_{operation}",
        OwnerEntityId = entityId,
        EntityType = typeof(T).Name,
        OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
        NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
        UserId = _currentUser?.Id ?? "SYSTEM",
        Timestamp = DateTime.UtcNow,
        Metadata = JsonSerializer.Serialize(new
        {
            Operation = operation,
            ComponentType = typeof(T).FullName,
            Version = (oldValue as IVersionedComponent)?.Version,
            CorrelationId = Activity.Current?.Id
        })
    };
    
    await _auditService.LogEvent(auditEvent);
}
```

## Query System

### Entity Query Builder

DataContext provides a fluent query interface:

```csharp
public IEntityQuery CreateQuery()
{
    return new EntityQuery(_pgClient, _auditService);
}

// Usage example
var activeOrders = await dataContext.CreateQuery()
    .WithAll<OrderComponent, InventoryComponent>()
    .WithAny<PriorityFlag, ExpressShipping>()
    .WithNone<CancelledFlag, RefundedFlag>()
    .Where<OrderComponent>(o => o.Status == "Active")
    .Where<OrderComponent>(o => o.Total > 100)
    .OrderBy<OrderComponent>(o => o.CreatedDate)
    .Take(50)
    .ExecuteAsync();
```

### Query Optimization

The query system generates optimized SQL:

```sql
-- Generated SQL for above query
WITH entity_matches AS (
    SELECT DISTINCT e.id
    FROM entities e
    WHERE EXISTS (
        SELECT 1 FROM order_component oc 
        WHERE oc.owner_entity_id = e.id 
        AND oc.status = 'Active' 
        AND oc.total > 100
    )
    AND EXISTS (
        SELECT 1 FROM inventory_component ic 
        WHERE ic.owner_entity_id = e.id
    )
    AND (
        EXISTS (SELECT 1 FROM priority_flag pf WHERE pf.owner_entity_id = e.id)
        OR EXISTS (SELECT 1 FROM express_shipping es WHERE es.owner_entity_id = e.id)
    )
    AND NOT EXISTS (
        SELECT 1 FROM cancelled_flag cf WHERE cf.owner_entity_id = e.id
    )
    AND NOT EXISTS (
        SELECT 1 FROM refunded_flag rf WHERE rf.owner_entity_id = e.id
    )
)
SELECT em.id, oc.created_date
FROM entity_matches em
JOIN order_component oc ON oc.owner_entity_id = em.id
ORDER BY oc.created_date
LIMIT 50;
```

## Performance Considerations

### Caching Strategy

DataContext implements intelligent caching:

```csharp
public class CachedDataContext : IDataContext
{
    private readonly IDataContext _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    
    public async Task<T?> GetComponent<T>(Guid entityId) where T : class, IComponent
    {
        var cacheKey = $"{typeof(T).Name}:{entityId}";
        
        if (_cache.TryGetValue<T>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache hit for {ComponentType}:{EntityId}", 
                typeof(T).Name, entityId);
            return cached;
        }
        
        var component = await _inner.GetComponent<T>(entityId);
        
        if (component != null)
        {
            _cache.Set(cacheKey, component, _cacheExpiration);
        }
        
        return component;
    }
}
```

### Batch Operations

DataContext supports efficient batch operations:

```csharp
public async Task CommitBatch<T>(IEnumerable<T> components) where T : IComponent
{
    // Group by operation type
    var grouped = components.GroupBy(c => /* existing check */);
    
    foreach (var group in grouped)
    {
        if (group.Key == OperationType.Insert)
        {
            await _pgClient.From<T>().Insert(group.ToList());
        }
        else
        {
            // Batch update using UNNEST
            await _pgClient.ExecuteBatchUpdate(group.ToList());
        }
    }
}
```

### Query Performance Metrics

DataContext tracks query performance:

```csharp
private async Task<T> MeasureOperation<T>(
    string operationName,
    Func<Task<T>> operation)
{
    using var activity = Activity.StartActivity(operationName);
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var result = await operation();
        
        _metrics.RecordOperationDuration(
            operationName, 
            stopwatch.ElapsedMilliseconds);
        
        return result;
    }
    catch (Exception ex)
    {
        _metrics.RecordOperationFailure(operationName);
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
}
```

## Testing and Isolation

### TestDataContext

A specialized implementation for testing:

```csharp
public class TestDataContext : DataContext
{
    private readonly List<Guid> _trackedEntities = new();
    
    public TestDataContext(/* dependencies */) : base(/* dependencies */)
    {
        // Override audit service with test implementation
        _auditService = new InMemoryAuditService();
    }
    
    public void TrackEntity(Guid entityId)
    {
        _trackedEntities.Add(entityId);
    }
    
    public async Task CleanupTrackedEntities()
    {
        foreach (var entityId in _trackedEntities)
        {
            // Remove all components for entity
            await RemoveAllComponents(entityId);
        }
    }
}
```

### Integration Test Support

DataContext provides test helpers:

```csharp
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IDataContext TestDataContext() => _dataContext;
    
    public async Task InitializeAsync()
    {
        // Setup test database
        // Register handlers
        // Create DataContext
        _dataContext = new TestDataContext(/* ... */);
    }
    
    public async Task DisposeAsync()
    {
        // Cleanup tracked entities
        await (_dataContext as TestDataContext)?.CleanupTrackedEntities();
    }
}
```

## Future Evolution

### 1. GraphQL Integration

Future versions will provide GraphQL schema generation:

```csharp
public interface IGraphQLDataContext : IDataContext
{
    IQueryable<T> QueryableFor<T>() where T : IComponent;
    Task<IResolverContext> CreateResolverContext();
}
```

### 2. Event Sourcing

Support for event-sourced components:

```csharp
public interface IEventSourcedDataContext : IDataContext
{
    Task<T> ReplayEvents<T>(Guid entityId, DateTime? asOf = null) 
        where T : IEventSourcedComponent;
    Task AppendEvent(Guid entityId, IComponentEvent @event);
}
```

### 3. Multi-Tenancy

Built-in multi-tenant support:

```csharp
public interface ITenantDataContext : IDataContext
{
    Guid CurrentTenantId { get; }
    IDataContext ForTenant(Guid tenantId);
}
```

### 4. Distributed Tracing

Enhanced observability:

```csharp
public interface ITracedDataContext : IDataContext
{
    Activity? CurrentActivity { get; }
    IDataContext WithBaggage(string key, string value);
}
```

## Conclusion

The DataContext is the cornerstone of the Jarvis framework, providing a powerful yet intuitive abstraction for data operations. Its design embodies several key architectural principles:

### Key Achievements

1. **Clean Separation**: Complete separation of data, logic, and orchestration
2. **Handler Pattern**: Consistent, testable business logic encapsulation
3. **Audit Compliance**: Comprehensive tracking of all operations
4. **Transaction Safety**: ACID guarantees with explicit boundaries
5. **Performance**: Optimized queries and operation batching

### Design Benefits

1. **Testability**: Every operation can be tested in isolation
2. **Maintainability**: Clear boundaries between concerns
3. **Extensibility**: Plugin-based architecture for handlers
4. **Compliance**: Built-in audit trail for all operations
5. **Type Safety**: Compile-time guarantees through generics

### Best Practices

1. **Always Use Handlers**: Never bypass handlers for business logic
2. **Immutable Components**: Use record types with init properties
3. **Explicit Transactions**: Use InTransaction for multi-operation consistency
4. **Track Test Entities**: Always track entities in tests for cleanup
5. **Monitor Performance**: Use built-in metrics for optimization

The DataContext pattern has proven to be a robust foundation for building scalable, maintainable applications while providing the flexibility needed for complex business requirements.

---

*Document Version: 1.0*  
*Last Updated: January 2025*  
*Authors: Jarvis Development Team*