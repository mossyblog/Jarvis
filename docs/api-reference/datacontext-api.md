# DataContext API Reference

The `DataContext` class is the primary interface for all data operations in the Jarvis ECS framework, providing handler resolution, component management, querying, and relationship tracking.

## Overview

`DataContext` is the high-level data access layer that:
- Resolves and manages component handlers
- Provides entity querying across components  
- Manages component persistence with versioning and concurrency control
- Tracks entity relationships (parent-child hierarchies)
- Handles audit logging and event emission
- Creates automatic snapshots for versioned components

## Class Definition

```csharp
namespace core.jarvis.Data
{
    public class DataContext : IDataContext
    {
        public DataContext(
            IServiceProvider serviceProvider,
            IComponentQueryHandlerRegistry queryRegistry,
            IPgClient pgClient,
            ILogger<DataContext> logger,
            IAuditService auditService,
            Events.IEventEmitter eventEmitter)
    }
}
```

## Constructor

The DataContext is typically resolved through dependency injection rather than instantiated directly.

### Parameters
- **serviceProvider**: DI container for resolving handlers
- **queryRegistry**: Registry for component query handlers
- **pgClient**: PostgreSQL client for database operations
- **logger**: Logger for DataContext operations
- **auditService**: Service for audit trail logging
- **eventEmitter**: Service for domain event emission

## Handler Resolution Methods

### For\<THandler\>(Guid entityId)

Resolves a strongly-typed handler for a specific entity.

```csharp
public THandler For<THandler>(Guid entityId) where THandler : class, IComponentHandler
```

**Parameters:**
- **entityId**: The entity ID the handler will operate on

**Returns:** An initialized handler instance

**Usage:**
```csharp
// Get a handler for a specific order
var orderHandler = dataContext.For<OrderHandler>(orderId);
var order = await orderHandler.Get();
await orderHandler.ConfirmOrder();

// Chain operations
var accountHandler = dataContext.For<AccountHandler>(userId);
var account = await accountHandler.Get();
await accountHandler.UpdateProfile(newData);
```

### For(Type componentType, Guid entityId)

Runtime handler resolution when type is not known at compile time.

```csharp
public IComponentHandler For(Type componentType, Guid entityId)
```

**Usage:**
```csharp
Type handlerType = typeof(OrderHandler);
var handler = dataContext.For(handlerType, entityId);
```

## Query Methods

### Query()

Creates a query builder for finding entities across components.

```csharp
public IEntityQuery Query()
```

**Returns:** `IEntityQuery` - A fluent query builder

**Usage Examples:**

```csharp
// Find all orders for a customer
var customerOrders = await dataContext.Query()
    .WithAll<OrderComponent>(o => o.CustomerId == customerId)
    .ToEntityComponents();

// Find confirmed orders with payments
var paidOrders = await dataContext.Query()
    .WithAll<OrderComponent>(o => o.Status == "CONFIRMED")
    .WithAll<PaymentComponent>(p => p.Status == "COMPLETED")
    .ToEntityComponents();

// Find orders without invoices
var unbilledOrders = await dataContext.Query()
    .WithAll<OrderComponent>(o => true)
    .WithNone<InvoiceComponent>(i => true)
    .ToEntityIds();

// Complex query with multiple components
var activeUserSessions = await dataContext.Query()
    .WithAll<Account>(a => a.IsActive)
    .WithAll<AuthToken>(t => !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
    .WithAll<SecurityProfile>(p => p.TwoFactorEnabled)
    .ToEntityComponents();
```

## Component Persistence Methods

### Commit\<TComponent\>(TComponent component)

Saves a component with automatic versioning and snapshot creation.

```csharp
public async Task Commit<TComponent>(TComponent component) 
    where TComponent : class, IComponent, new()
```

**Features:**
- Automatically increments version for `IVersionedComponent`
- Creates snapshots for versioned components
- Full audit logging
- Throws on concurrency conflicts

**Usage:**
```csharp
// Create new component
var order = new OrderComponent
{
    Id = Guid.NewGuid(),
    OwnerEntityId = entityId,
    OrderNumber = "ORD-001",
    Status = "PENDING"
};
await dataContext.Commit(order);

// Update existing component
order.Status = "CONFIRMED";
order.UpdatedAt = DateTime.UtcNow;
await dataContext.Commit(order);
```

### TryCommit\<TComponent\>(TComponent component)

Attempts to save with optimistic concurrency control.

```csharp
public async Task<bool> TryCommit<TComponent>(TComponent component) 
    where TComponent : class, IComponent, new()
```

**Returns:** 
- `true` if successful
- `false` if concurrency conflict detected

**Concurrency Handling:**
- For `IVersionedComponent`: Uses version-based concurrency
- For regular components: Uses timestamp-based concurrency (10ms tolerance)

**Usage:**
```csharp
// Handle concurrent updates gracefully
var success = await dataContext.TryCommit(order);
if (!success)
{
    // Reload and retry
    var handler = dataContext.For<OrderHandler>(order.OwnerEntityId);
    var current = await handler.Get();
    current.Status = newStatus;
    success = await dataContext.TryCommit(current);
}
```

### Remove\<TComponent\>(Guid entityId)

Removes component(s) by entity ID.

```csharp
public async Task Remove<TComponent>(Guid entityId) 
    where TComponent : class, IComponent, new()
```

**Usage:**
```csharp
// Remove all components for an entity
await dataContext.Remove<OrderComponent>(orderId);
await dataContext.Remove<PaymentComponent>(orderId);
```

### Insert\<TModel\>(TModel model)

Direct insertion for models (typically audit events).

```csharp
public async Task Insert<TModel>(TModel model) where TModel : class, new()
```

## Relationship Management Methods

### LinkRelationship(Guid parentId, Guid childId, string? parentType = null, string? childType = null)

Creates a parent-child relationship between entities.

```csharp
public async Task LinkRelationship(
    Guid parentId, 
    Guid childId, 
    string? parentType = null, 
    string? childType = null)
```

**Features:**
- Bidirectional relationship tracking
- Optional type information for clarity
- Full audit logging

**Usage:**
```csharp
// Link work order to invoices
await dataContext.LinkRelationship(workOrderId, invoiceId, "WorkOrder", "Invoice");

// Link project hierarchy
await dataContext.LinkRelationship(projectId, phaseId, "Project", "Phase");
await dataContext.LinkRelationship(phaseId, taskId, "Phase", "Task");
```

### UnlinkRelationship(Guid parentId, Guid childId)

Removes a parent-child relationship.

```csharp
public async Task UnlinkRelationship(Guid parentId, Guid childId)
```

### Parent(Guid entityId)

Gets the parent entity ID.

```csharp
public async Task<Guid?> Parent(Guid entityId)
```

**Returns:** Parent entity ID or null if no parent

### Children(Guid entityId)

Gets all direct child entity IDs.

```csharp
public async Task<List<Guid>> Children(Guid entityId)
```

### ChildOf(Guid childId, Guid parentId)

Checks if an entity is a child of another.

```csharp
public async Task<bool> ChildOf(Guid childId, Guid parentId)
```

### Ancestors(Guid entityId)

Gets all ancestor entity IDs (with loop protection).

```csharp
public async Task<List<Guid>> Ancestors(Guid entityId)
```

**Note:** Limited to 100 ancestors for safety

### Descendants(Guid entityId)

Gets all descendant entity IDs (with loop protection).

```csharp
public async Task<List<Guid>> Descendants(Guid entityId)
```

**Note:** Limited to 1000 descendants for safety

**Usage Examples:**

```csharp
// Build a project hierarchy
await dataContext.LinkRelationship(companyId, projectId, "Company", "Project");
await dataContext.LinkRelationship(projectId, workOrderId, "Project", "WorkOrder");
await dataContext.LinkRelationship(workOrderId, taskId, "WorkOrder", "Task");

// Query the hierarchy
var projectTasks = await dataContext.Descendants(projectId); // Gets all tasks under project
var taskCompany = await dataContext.Ancestors(taskId); // Gets project and company

// Check relationships
var isProjectTask = await dataContext.ChildOf(taskId, projectId); // true
```

## Event Emission Methods

### Emit\<TEvent\>(TEvent event)

Emits a single domain event.

```csharp
public async Task Emit<TEvent>(TEvent @event) where TEvent : Events.IEvent
```

### EmitBatch\<TEvent\>(IEnumerable\<TEvent\> events)

Emits multiple events as a batch.

```csharp
public async Task EmitBatch<TEvent>(IEnumerable<TEvent> events) where TEvent : Events.IEvent
```

**Usage:**
```csharp
// Emit order confirmed event
await dataContext.Emit(new OrderConfirmedEvent
{
    OrderId = orderId,
    ConfirmedAt = DateTime.UtcNow,
    CustomerId = customerId
});

// Emit batch of events
var events = orders.Select(o => new OrderShippedEvent 
{ 
    OrderId = o.Id, 
    ShippedAt = DateTime.UtcNow 
});
await dataContext.EmitBatch(events);
```

## Snapshot Methods

### Snapshots()

Creates a query builder for component snapshots.

```csharp
public ISnapshotQuery Snapshots()
```

**Usage:**
```csharp
// Get invoice history
var invoiceHistory = await dataContext.Snapshots()
    .ForComponent<InvoiceComponent>(invoiceId)
    .ToListAsync();

// Get snapshot at specific time
var snapshot = await dataContext.Snapshots()
    .ForComponent<OrderComponent>(orderId)
    .AtTime(DateTime.UtcNow.AddDays(-7))
    .SingleOrDefaultAsync();
```

## GraphQL Support

### GraphQL(string query)

Creates a GraphQL query builder.

```csharp
public IGraphQLQuery GraphQL(string query)
```

**Usage:**
```csharp
var result = await dataContext.GraphQL(@"
    query GetOrder($id: UUID!) {
        orders(id: $id) {
            id
            orderNumber
            status
            customer {
                name
                email
            }
        }
    }")
    .WithVariable("id", orderId)
    .ExecuteAsync();
```

## Common Patterns

### Pattern 1: Handler-Based Business Logic

```csharp
public class OrderService
{
    private readonly IDataContext _dataContext;
    
    public async Task<OrderDto> ProcessOrder(Guid orderId)
    {
        // Get handler for specific entity
        var handler = _dataContext.For<OrderHandler>(orderId);
        
        // Execute business logic through handler
        await handler.ValidateOrder();
        await handler.CalculateTotals();
        await handler.ConfirmOrder();
        
        // Return result
        var order = await handler.Get();
        return MapToDto(order);
    }
}
```

### Pattern 2: Cross-Component Queries

```csharp
public async Task<List<CustomerOrderSummary>> GetCustomerOrders(Guid customerId)
{
    // Find all orders with payments for a customer
    var orderEntities = await _dataContext.Query()
        .WithAll<OrderComponent>(o => o.CustomerId == customerId)
        .WithAll<PaymentComponent>(p => p.Status == "COMPLETED")
        .ToEntityComponents();
    
    var summaries = new List<CustomerOrderSummary>();
    foreach (var (entityId, components) in orderEntities)
    {
        var order = components.Get<OrderComponent>();
        var payment = components.Get<PaymentComponent>();
        
        summaries.Add(new CustomerOrderSummary
        {
            OrderId = entityId,
            OrderNumber = order.OrderNumber,
            Total = order.TotalAmount,
            PaymentDate = payment.CompletedAt
        });
    }
    
    return summaries;
}
```

### Pattern 3: Hierarchical Data Management

```csharp
public async Task<ProjectHierarchy> GetProjectStructure(Guid projectId)
{
    var hierarchy = new ProjectHierarchy { ProjectId = projectId };
    
    // Get all work orders under project
    var workOrderIds = await _dataContext.Children(projectId);
    
    foreach (var workOrderId in workOrderIds)
    {
        var woHandler = _dataContext.For<WorkOrderHandler>(workOrderId);
        var workOrder = await woHandler.Get();
        
        // Get tasks under each work order
        var taskIds = await _dataContext.Children(workOrderId);
        var tasks = new List<TaskInfo>();
        
        foreach (var taskId in taskIds)
        {
            var taskHandler = _dataContext.For<TaskHandler>(taskId);
            var task = await taskHandler.Get();
            tasks.Add(new TaskInfo(task));
        }
        
        hierarchy.WorkOrders.Add(new WorkOrderInfo
        {
            WorkOrder = workOrder,
            Tasks = tasks
        });
    }
    
    return hierarchy;
}
```

### Pattern 4: Concurrency Handling

```csharp
public async Task<bool> UpdateOrderStatus(Guid orderId, string newStatus, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        var handler = _dataContext.For<OrderHandler>(orderId);
        var order = await handler.Get();
        
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        
        if (await _dataContext.TryCommit(order))
        {
            await _dataContext.Emit(new OrderStatusChangedEvent 
            { 
                OrderId = orderId, 
                NewStatus = newStatus 
            });
            return true;
        }
        
        // Wait before retry
        await Task.Delay(100 * (i + 1));
    }
    
    return false; // Failed after retries
}
```

## Dependency Injection Setup

DataContext and handlers must be registered properly:

```csharp
// In Startup.cs or Program.cs
services.RegisterJarvis(LogLevel.Information, Configuration);

// Register your handlers
services.AddScoped<IComponentHandler, OrderHandler>();
services.AddScoped<OrderHandler>(); // Also register concrete type!

services.AddScoped<IComponentHandler, PaymentHandler>();
services.AddScoped<PaymentHandler>();
```

## Error Handling

DataContext operations wrap exceptions in domain-specific types:

- **EntityNotFoundException**: Component or entity not found
- **ConcurrencyException**: Version conflict in TryCommit
- **ComponentOperationException**: General component operation failures
- **EventEmissionException**: Event emission failures

Example:
```csharp
try
{
    var handler = dataContext.For<OrderHandler>(orderId);
    var order = await handler.Get();
}
catch (EntityNotFoundException ex)
{
    // Handle missing entity
    logger.LogWarning("Order {OrderId} not found", orderId);
    return NotFound();
}
catch (ComponentOperationException ex)
{
    // Handle operation failure
    logger.LogError(ex, "Failed to retrieve order");
    throw;
}
```

## Best Practices

1. **Always Use Handlers**: Encapsulate business logic in handlers
   ```csharp
   // Good
   var handler = dataContext.For<OrderHandler>(orderId);
   await handler.ProcessOrder();
   
   // Avoid direct component manipulation
   ```

2. **Handle Concurrency**: Use TryCommit for user-initiated updates
   ```csharp
   if (!await dataContext.TryCommit(component))
   {
       // Handle conflict
   }
   ```

3. **Batch Related Operations**: Use entity relationships
   ```csharp
   // Link all related entities
   await dataContext.LinkRelationship(orderId, paymentId, "Order", "Payment");
   await dataContext.LinkRelationship(orderId, shippingId, "Order", "Shipping");
   ```

4. **Query Efficiently**: Use appropriate query methods
   ```csharp
   // Get just IDs when that's all you need
   var ids = await dataContext.Query()
       .WithAll<OrderComponent>(o => o.Status == "PENDING")
       .ToEntityIds();
   ```

5. **Audit Important Operations**: DataContext automatically logs, but add context
   ```csharp
   await dataContext.Emit(new CustomerActionEvent
   {
       CustomerId = customerId,
       Action = "OrderCancelled",
       Reason = cancellationReason
   });
   ```

## See Also

- [PgClient API Reference](pgclient-api.md) - Low-level database access
- [Handler Development Guide](../guides/handler-development.md) - Creating handlers
- [Query API Reference](query-api.md) - Entity query details
- [Snapshot API Reference](snapshot-api.md) - Version history access