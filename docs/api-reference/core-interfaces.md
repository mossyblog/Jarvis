# Core Interfaces

## DataContext

### Overview

The `DataContext` class is the concrete implementation of `IDataContext` and serves as the primary entry point for all entity and component operations in the Jarvis ECS framework. It provides a unified interface for:

- Handler resolution and initialization
- Component persistence with versioning and concurrency control
- Entity relationship management (parent-child hierarchies)
- Query building for cross-component searches
- Snapshot management for audit trails
- GraphQL query execution

### Why Use DataContext?

The `DataContext` acts as the orchestration layer that:

1. **Decouples business logic from storage**: Handlers contain business rules while DataContext manages persistence
2. **Provides consistency**: All operations go through the same validation, auditing, and error handling
3. **Enables plugin architecture**: Handlers are resolved through dependency injection, allowing modular design
4. **Manages complexity**: Handles versioning, concurrency, relationships, and snapshots transparently

### Core Functionality

#### Handler Resolution

```csharp
// Create a new entity with unique ID (new method)
var entity = dataContext.Entity();

// Resolve a handler by interface type
var invoiceHandler = dataContext.For<IInvoiceHandler>(entity.Id);

// Resolve by component type (less common)
var handler = dataContext.For(typeof(InvoiceComponent), entity.Id);
```

Handlers are:
- Resolved from the DI container
- Initialized with the entity ID
- Responsible for all business logic related to their component

#### Component Persistence

```csharp
// Save a component (insert or update)
await dataContext.Commit(component);

// Try to save with concurrency checking
bool success = await dataContext.TryCommit(component);

// Remove components for an entity
await dataContext.Remove<InvoiceComponent>(entityId);
```

Persistence features:
- **Optional versioning** for `IVersionedComponent` interface
- Optimistic concurrency control (version-based for versioned components, timestamp-based otherwise)  
- Automatic audit trail creation with pre-change state capture
- Enhanced snapshot system for versioned components
- Automatic table creation and schema validation via `ITableManager`
- Different handling strategies for versioned vs non-versioned components

#### Entity Querying

```csharp
// Create complex queries across components
var activeInvoices = await dataContext.Query()
    .WithAll<InvoiceComponent, PaymentComponent>()
    .Where<InvoiceComponent>(i => i.Status == "Active")
    .Where<PaymentComponent>(p => p.Amount > 1000)
    .ToEntityIds();
```

#### Relationship Management

```csharp
// Link parent-child relationships
await dataContext.LinkRelationship(parentId, childId, "Order", "Invoice");

// Query relationships
var parent = await dataContext.Parent(childId);
var children = await dataContext.Children(parentId);
var ancestors = await dataContext.Ancestors(entityId);
var descendants = await dataContext.Descendants(entityId);

// Check relationships
bool isChild = await dataContext.ChildOf(childId, parentId);
```

#### Snapshot Management

```csharp
// Query historical snapshots
var snapshots = await dataContext.Snapshots()
    .ForEntity(entityId)
    .ForComponent<InvoiceComponent>()
    .InRange(startDate, endDate)
    .Execute();
```

#### GraphQL Queries

```csharp
// Execute GraphQL queries with authentication
var result = await dataContext.GraphQL(@"
    query GetInvoice($id: UUID!) {
        invoice(id: $id) {
            id
            amount
            status
        }
    }")
    .WithAuth(jwt)
    .WithVariables(new { id = invoiceId })
    .ExecuteAsync<InvoiceResult>();
```

### Implementation Details

#### Concurrency Control

The DataContext implements two types of concurrency control:

1. **Version-based** (for `IVersionedComponent`):
   - Increments version number on each update
   - Rejects updates if version doesn't match
   
2. **Timestamp-based** (fallback):
   - Uses `LastUpdated` field
   - Allows 10ms tolerance for clock drift

#### Audit Trail

All operations are automatically audited:
- Component creation, updates, and deletions
- Relationship changes
- Query operations (for security monitoring)
- Errors and exceptions

#### Error Handling

Consistent error handling with:
- Detailed error context
- Audit trail for failures
- Typed exceptions (`ComponentNotFoundException`, `ConcurrencyException`, etc.)

### Best Practices

1. **Always use handlers**: Don't bypass handlers for business operations
   ```csharp
   // Good
   var handler = dataContext.For<InvoiceHandler>(id);
   await handler.UpdateStatus("Paid");
   
   // Bad - bypasses business logic
   var invoice = await GetInvoice();
   invoice.Status = "Paid";
   await dataContext.Commit(invoice);
   ```

2. **Use TryCommit for user operations**: Provides better concurrency handling
   ```csharp
   if (!await dataContext.TryCommit(component))
   {
       // Handle concurrency conflict
       return Conflict("The record was modified by another user");
   }
   ```

3. **Leverage relationships**: Use the built-in hierarchy support
   ```csharp
   // Link order items to order
   foreach (var itemId in orderItemIds)
   {
       await dataContext.LinkRelationship(orderId, itemId, "Order", "OrderItem");
   }
   ```

4. **Batch operations in handlers**: Reduce round trips
   ```csharp
   public async Task<List<InvoiceComponent>> GetActiveInvoices()
   {
       var entityIds = await _dataContext.Query()
           .WithAll<InvoiceComponent>()
           .Where<InvoiceComponent>(i => i.Status == "Active")
           .ToEntityIds();
           
       // Batch fetch all invoices
       return await FetchInvoices(entityIds);
   }
   ```

### Common Patterns

#### Parent-Child Aggregates
```csharp
// Create order with items
var orderId = Guid.NewGuid();
var order = new OrderComponent { Id = orderId, /* ... */ };
await dataContext.Commit(order);

foreach (var item in items)
{
    item.OwnerEntityId = Guid.NewGuid();
    await dataContext.Commit(item);
    await dataContext.LinkRelationship(orderId, item.OwnerEntityId);
}
```

#### Cross-Component Queries
```csharp
// Find all paid invoices with recent payments
var paidInvoices = await dataContext.Query()
    .WithAll<InvoiceComponent, PaymentComponent>()
    .Where<InvoiceComponent>(i => i.Status == "Paid")
    .Where<PaymentComponent>(p => p.PaymentDate > DateTime.UtcNow.AddDays(-30))
    .ToEntityIds();
```

#### Version History
```csharp
// Get component history
var history = await dataContext.Snapshots()
    .ForEntity(entityId)
    .ForComponent<InvoiceComponent>()
    .Execute();
    
foreach (var snapshot in history)
{
    Console.WriteLine($"Version {snapshot.Version}: {snapshot.Timestamp}");
}
```

## IDataContext

Interface defining the contract for DataContext. Main operations:

- `Entity()`: Create new entity with unique ID
- `For<THandler>(Guid entityId)`: Resolve handler for entity
- `Query()`: Create entity query builder
- `Commit<T>(T component)`: Save component changes
- `TryCommit<T>(T component)`: Try save with concurrency check
- `Remove<T>(Guid entityId)`: Remove components
- `Parent(Guid entityId)`: Get parent entity
- `Children(Guid entityId)`: Get child entities
- `LinkRelationship(...)`: Create parent-child link
- `Snapshots()`: Query component history
- `GraphQL(string query)`: Execute GraphQL query

## IComponentHandler

Base interface for all component handlers. Handlers encapsulate:

- Business logic and validation
- Component-specific operations
- Complex workflows
- Integration with other handlers

```csharp
public interface IComponentHandler
{
    void InitializeContext(Guid entityId);
}

public interface IComponentHandler<TComponent> : IComponentHandler
    where TComponent : class, IComponent
{
    Task<TComponent?> Get();
    Task<TComponent> Require();
    Task Save(TComponent component);
    Task Delete();
}
```

## IEntityQuery

Fluent interface for querying entities across components:

```csharp
public interface IEntityQuery
{
    IEntityQuery WithAll<T1>() where T1 : IComponent;
    IEntityQuery WithAll<T1, T2>() where T1 : IComponent where T2 : IComponent;
    IEntityQuery WithAny<T1>() where T1 : IComponent;
    IEntityQuery WithNone<T1>() where T1 : IComponent;
    IEntityQuery Where<T>(Expression<Func<T, bool>> predicate) where T : IComponent;
    Task<List<Guid>> ToEntityIds();
    Task<List<(Guid EntityId, List<IComponent> Components)>> ToEntityComponents();
}
```

Key features:
- Type-safe component filtering
- Composable query building
- Efficient batching
- No N+1 query problems 