# Database Guide

PostgreSQL data access in Jarvis uses the DataContext pattern. Direct database access is prohibited.

## DataContext Overview

DataContext is the single entry point for all data operations:
- Entity and component CRUD operations
- Query building with type-safe filters
- Entity relationships (parent/child hierarchies)
- Transaction management

```csharp
public class OrderSystem
{
    private readonly IDataContext _dataContext;

    public OrderSystem(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task ProcessOrder(Guid orderId)
    {
        var handler = _dataContext.For<OrderHandler>(orderId);
        await handler.ConfirmOrder();
    }
}
```

### Why DataContext Only

All database access goes through DataContext because:
1. JWT-based Row Level Security requires proper authentication context
2. Optimistic concurrency prevents lost updates
3. Transaction management ensures atomic operations

## Querying Entities

Use the fluent query API to find entities by component criteria.

### Basic Queries

```csharp
var entities = await _dataContext.Query()
    .WithAll<OrderComponent>(o => o.Status == "PENDING")
    .ToList();

foreach (var entity in entities)
{
    var order = await entity.Get<OrderComponent>();
    var customer = await entity.Get<CustomerComponent>();
}
```

### Query Methods

| Method | Description |
|--------|-------------|
| `WithAll<T>(predicate)` | AND filter - entities must match all criteria |
| `WithAll<T>()` | AND filter - entities must have this component |
| `WithAny<T>(predicate)` | OR filter - entities matching any criteria |
| `WithNone<T>(predicate)` | Exclusion - exclude entities matching criteria |
| `Include<T>()` | Eager load component without filtering |

### Sorting Results

```csharp
var entities = await _dataContext.Query()
    .WithAll<OrderComponent>(o => o.Status == "ACTIVE")
    .OrderBy<OrderComponent>(o => o.Priority)
    .ThenByDescending<OrderComponent>(o => o.TotalAmount)
    .ToList();
```

### Query Execution

| Method | Returns |
|--------|---------|
| `ToList()` | `List<Entity>` - Entities with DataContext binding |
| `ToEntityIds()` | `List<Guid>` - Just the entity IDs |
| `Single()` | `Entity` - Exactly one result (throws if 0 or >1) |

## Entity Relationships

Jarvis supports hierarchical entity relationships through parent/child links.

### Creating Relationships

```csharp
// Link child to parent
await _dataContext.LinkRelationship(parentId, childId);

// With optional type validation
await _dataContext.LinkRelationship(parentId, childId, "Organization", "Department");

// Remove relationship
await _dataContext.UnlinkRelationship(parentId, childId);
```

### Querying Relationships

```csharp
var parentId = await _dataContext.Parent(entityId);
var childIds = await _dataContext.Children(entityId);
bool isChild = await _dataContext.ChildOf(childId, parentId);

// Hierarchy traversal
var ancestors = await _dataContext.Ancestors(entityId);
var descendants = await _dataContext.Descendants(entityId);
```

## Row Level Security (RLS)

PostgreSQL RLS enforces data isolation at the database level. JWT claims are passed to the database as session variables.

### How RLS Works

1. User authenticates and receives a JWT token
2. PgClient parses JWT and extracts claims
3. Claims are set as PostgreSQL session variables (`app.*`)
4. RLS policies use these variables to filter rows

```csharp
pgClient.JWT(authToken);

// Claims become session variables accessible in SQL
// CREATE POLICY user_data ON orders
//     USING (user_id = current_setting('app.user_id', true)::uuid);
```

### Security Guarantees

- JWT signature is validated before claims are used
- RLS policies are enforced at the database level
- Even if application code has bugs, unauthorized data access is blocked

## Connection Pooling

Connection pooling is handled automatically by NpgsqlDataSource. Configure via `ConnectionPoolingOptions`:

```csharp
services.Configure<ConnectionPoolingOptions>(options =>
{
    options.Enabled = true;
    options.MaxPoolSize = 20;
    options.MinPoolSize = 5;
    options.ConnectionLifetimeMinutes = 5;
});
```

| Option | Default | Description |
|--------|---------|-------------|
| `Enabled` | false | Enable/disable pooling |
| `MaxPoolSize` | 20 | Maximum connections in pool |
| `MinPoolSize` | 5 | Minimum maintained connections |
| `ConnectionLifetimeMinutes` | 5 | Max connection age |

## Optimistic Concurrency

Jarvis uses optimistic concurrency to prevent lost updates. Components can implement `IVersionedComponent`:

```csharp
public record OrderComponent : IVersionedComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }  // Incremented on each update

    public string Status { get; set; }
}
```

### How It Works

1. Component is read with its current `Version`
2. Changes are made to the component
3. On `TryCommit`, the version is checked against the database
4. If versions match, save succeeds and version increments
5. If versions differ, `TryCommit` returns `false`

```csharp
var handler = _dataContext.For<OrderHandler>(orderId);
var order = await handler.Get();

order.Status = "CONFIRMED";

bool success = await _dataContext.TryCommit(order);
if (!success)
{
    // Handle conflict - re-read and retry
}
```

## Common Operations Reference

### Creating Entities

```csharp
var entity = _dataContext.NewEntity();
var component = new OrderComponent
{
    Id = Guid.NewGuid(),
    OwnerEntityId = entity.Id,
    Status = "DRAFT"
};
await _dataContext.Commit(component);
```

### Reading Components

```csharp
// Via handler
var handler = _dataContext.For<OrderHandler>(entityId);
var order = await handler.Get();

// Via entity from query
var entities = await _dataContext.Query()
    .WithAll<OrderComponent>()
    .ToList();
var order = await entities.First().Get<OrderComponent>();
```

### Updating Components

```csharp
var handler = _dataContext.For<OrderHandler>(entityId);
var order = await handler.Get();
order.Status = "CONFIRMED";
await handler.TryCommit(order);
```

### Deleting Components

```csharp
await _dataContext.Remove<OrderComponent>(entityId);
```

### Transactions

```csharp
await _dataContext.ExecuteInTransaction(async () =>
{
    var orderHandler = _dataContext.For<OrderHandler>(orderId);
    await orderHandler.ConfirmOrder();

    var invoiceHandler = _dataContext.For<InvoiceHandler>(orderId);
    await invoiceHandler.GenerateInvoice();
});
```

## Database Conventions

| C# Property | PostgreSQL Column |
|-------------|-------------------|
| `Id` | `id` |
| `OwnerEntityId` | `owner_entity_id` |
| `LastUpdated` | `last_updated` |
| `CustomerName` | `customer_name` |

Properties are automatically mapped to snake_case.
