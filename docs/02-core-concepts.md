# Core Concepts: The EHS Architecture

Jarvis uses the Entity-Handler-System (EHS) pattern - a three-layer architecture that enforces clean separation between data, business logic, and orchestration.

## The Three Layers

```
+------------------------------------------------------------------+
|                          API LAYER                                |
|              (Azure Functions - HTTP parsing only)                |
+------------------------------------------------------------------+
                               |
                               v
+------------------------------------------------------------------+
|                           SYSTEMS                                 |
|           Orchestration layer - coordinates handlers              |
|           NO business logic, just workflow coordination           |
+------------------------------------------------------------------+
                               |
                               v
+------------------------------------------------------------------+
|                          HANDLERS                                 |
|         ALL business logic lives here - one per component         |
|         Validation, state transitions, business rules             |
+------------------------------------------------------------------+
                               |
                               v
+------------------------------------------------------------------+
|                   ENTITIES & COMPONENTS                           |
|              Pure data - no behavior, no logic                    |
|              Components are C# records with IComponent            |
+------------------------------------------------------------------+
                               |
                               v
+------------------------------------------------------------------+
|                        POSTGRESQL                                 |
|                 Row-Level Security via JWT                        |
+------------------------------------------------------------------+
```

**Data flows down. Exceptions bubble up.**

## 1. Entities and Components (Data Layer)

Entities are containers. Components are the actual data. Neither contains business logic.

### Entity

An entity is just a GUID with optional hierarchy:

```csharp
public class Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid ParentId { get; set; }
    public Guid[] ChildrenIds { get; set; }
}
```

### Component

A component implements `IComponent` and holds domain data:

```csharp
public record OrderComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }

    // Domain data - no methods, no logic
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public int TotalAmountCents { get; set; }
    public bool IsPaid { get; set; }
}
```

**Key rules:**
- Components are C# records (immutable by default)
- No methods that modify state
- No validation logic
- No business rules
- Just data

## 2. Handlers (Business Logic Layer)

Handlers own ALL business logic for a single component type.

```csharp
public class OrderHandler : ComponentHandler<OrderComponent>
{
    public OrderHandler(IDataContext dataContext, ILogger<OrderHandler> logger)
        : base(dataContext, logger) { }

    public async Task<OrderComponent> ConfirmOrder()
    {
        var order = await Get();

        // Business rule validation - throws if violated
        Ensure(order.Status == "PENDING", "Only pending orders can be confirmed");

        order.Status = "CONFIRMED";
        order.LastUpdated = DateTime.UtcNow;

        await TryCommit(order);
        return order;
    }

    public async Task CancelOrder(string reason)
    {
        var order = await Get();

        Ensure(order.Status != "SHIPPED", "Cannot cancel shipped orders");
        Ensure(order.Status != "CANCELLED", "Order already cancelled");

        order.Status = "CANCELLED";
        await TryCommit(order);
    }
}
```

**Key rules:**
- One handler per component type
- All validation happens here
- All state transitions happen here
- NO try-catch blocks (exceptions bubble up)
- Use `Ensure()` for business rule validation
- Use `Get()` to retrieve the component
- Use `TryCommit()` to save changes

## 3. Systems (Orchestration Layer)

Systems coordinate multiple handlers. They contain ZERO business logic.

```csharp
public class OrderFulfillmentSystem
{
    private readonly IDataContext _dataContext;

    public OrderFulfillmentSystem(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task FulfillOrder(Guid orderId)
    {
        // Orchestrate handlers - no business logic here
        var orderHandler = _dataContext.For<OrderHandler>(orderId);
        await orderHandler.ConfirmOrder();

        var invoiceHandler = _dataContext.For<InvoiceHandler>(orderId);
        await invoiceHandler.GenerateInvoice();

        var inventoryHandler = _dataContext.For<InventoryHandler>(orderId);
        await inventoryHandler.ReserveItems();
    }
}
```

**Key rules:**
- Coordinate multiple handlers
- Handle transactions when needed
- NO business logic (no if/else on business conditions)
- NO validation (handlers do that)
- Let exceptions bubble up

## 4. Why This Architecture?

### Testability Without Mocks

Traditional MVC forces you to mock everything:

```csharp
// BAD: Traditional MVC - requires mocking
public class OrderService
{
    private readonly IOrderRepository _repo;
    private readonly IEmailService _email;
    // Test requires mocking all dependencies
}
```

EHS handlers work with real data:

```csharp
// GOOD: EHS - test with real database
var handler = dataContext.For<OrderHandler>(orderId);
await handler.ConfirmOrder();

var order = await dataContext.Get<OrderComponent>(orderId);
Assert.Equal("CONFIRMED", order.Status);
```

### Clear Ownership

- Order validation lives in `OrderHandler`
- Invoice generation lives in `InvoiceHandler`
- Fulfillment workflow lives in `FulfillmentSystem`

### Exception Bubbling

Errors propagate naturally to the API layer:

```
Handler throws BusinessRuleException
    -> System doesn't catch it
        -> API middleware converts to 400 Bad Request
```

## 5. Critical Rules

### No Try-Catch in Handlers

```csharp
// BAD - catching exceptions hides errors
public async Task<bool> ConfirmOrder()
{
    try { ... }
    catch (Exception) { return false; }  // NO!
}

// GOOD - let exceptions bubble
public async Task ConfirmOrder()
{
    var order = await Get();
    Ensure(order.Status == "PENDING", "Only pending orders can be confirmed");
    order.Status = "CONFIRMED";
    await TryCommit(order);
}
```

### No Mocks

```csharp
// BAD - mocking hides real behavior
var mockRepo = new Mock<IOrderRepository>();

// GOOD - test against real database
var handler = dataContext.For<OrderHandler>(orderId);
await handler.ConfirmOrder();
```

### Handlers Own ALL Business Logic

```csharp
// BAD - logic in system
public class OrderSystem
{
    public async Task ProcessOrder(Guid orderId)
    {
        var order = await _dataContext.Get<OrderComponent>(orderId);
        if (order.Status == "PENDING")  // NO! Business logic in system
        {
            order.Status = "CONFIRMED";
        }
    }
}

// GOOD - system orchestrates, handler decides
public class OrderSystem
{
    public async Task ProcessOrder(Guid orderId)
    {
        var handler = _dataContext.For<OrderHandler>(orderId);
        await handler.ConfirmOrder();
    }
}
```

## Summary

| Layer | Contains | Does NOT Contain |
|-------|----------|------------------|
| **Components** | Data fields | Methods, validation, logic |
| **Handlers** | Business logic, validation | Try-catch, orchestration |
| **Systems** | Workflow coordination | Business logic, validation |
| **API** | HTTP parsing | Business logic |

**Next:** [03-handlers.md](03-handlers.md) - Deep dive into handler patterns
