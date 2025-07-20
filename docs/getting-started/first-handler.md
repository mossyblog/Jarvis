# Your First Handler

This guide walks you through creating your first component and handler in Jarvis ECS.

## 1. Configure Dependency Injection

Add Jarvis ECS to your DI setup:

```csharp
// In Program.cs or Startup.cs
services.RegisterJarvis(LogLevel.Information, Configuration);

// Register your handlers
services.AddScoped<IComponentHandler, OrderHandler>();
services.AddScoped<OrderHandler>(); // Also register concrete type for DataContext.For<T>()
```

## 2. Create Your Component

Components are data structures that implement `IComponent`:

```csharp
public record OrderComponent : IComponent, IVersionedComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Your domain properties
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public int TotalAmountCents { get; set; }
    
    // IVersionedComponent for optimistic concurrency
    public int? Version { get; set; }
}
```

## 3. Create Your Handler

Handlers encapsulate all business logic for a component:

```csharp
public class OrderHandler : ComponentHandler<OrderComponent>
{
    private readonly IAuditService _auditService;

    public OrderHandler(
        IDataContext dataContext, 
        ILogger<OrderHandler> logger,
        IAuditService auditService)
        : base(dataContext, logger)
    {
        _auditService = auditService;
    }

    public async Task<OrderComponent> CreateOrder(
        string orderNumber, 
        string customerId, 
        decimal totalAmount)
    {
        // Validation
        Guard.AgainstEmpty(orderNumber, nameof(orderNumber));
        Guard.AgainstEmpty(customerId, nameof(customerId));
        
        // Create component
        var order = new OrderComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = OwnerEntityId,
            OrderNumber = orderNumber,
            CustomerId = customerId,
            TotalAmountCents = (int)(totalAmount * 100),
            Status = "PENDING",
            UpdatedAt = DateTime.UtcNow
        };
        
        // Save to database
        await DataContext.Commit(order);
        
        // Audit trail
        await _auditService.LogEvent("OrderCreated", OwnerEntityId, new
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            TotalAmount = totalAmount
        });
        
        return order;
    }

    public async Task<bool> ConfirmOrder()
    {
        var order = await Get();
        
        // Business rule validation
        Ensure(order.Status == "PENDING", "Only pending orders can be confirmed");
        
        // Update status
        order.Status = "CONFIRMED";
        order.UpdatedAt = DateTime.UtcNow;
        
        // Save with concurrency handling
        var success = await DataContext.TryCommit(order);
        if (!success)
        {
            throw new ConcurrencyException("Order confirmation failed due to concurrent modification");
        }
        
        return true;
    }
}
```

## 4. Use Your Handler

In your service or controller:

```csharp
public class OrderService
{
    private readonly IDataContext _dataContext;
    
    public OrderService(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task<OrderDto> CreateNewOrder(CreateOrderRequest request)
    {
        // Create a new entity ID
        var entityId = Guid.NewGuid();
        
        // Get handler for this entity
        var handler = _dataContext.For<OrderHandler>(entityId);
        
        // Create the order
        var order = await handler.CreateOrder(
            request.OrderNumber,
            request.CustomerId,
            request.TotalAmount
        );
        
        return new OrderDto
        {
            Id = order.OwnerEntityId,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            TotalAmount = order.TotalAmountCents / 100m
        };
    }
    
    public async Task<bool> ConfirmOrder(Guid orderId)
    {
        var handler = _dataContext.For<OrderHandler>(orderId);
        return await handler.ConfirmOrder();
    }
}
```

## 5. Query Across Components

Use DataContext to query entities with specific components:

```csharp
public async Task<List<OrderSummary>> GetPendingOrders()
{
    // Find all entities with pending orders
    var pendingOrders = await _dataContext.Query()
        .WithAll<OrderComponent>(o => o.Status == "PENDING")
        .ToEntityComponents();
    
    var summaries = new List<OrderSummary>();
    foreach (var (entityId, components) in pendingOrders)
    {
        var order = components.Get<OrderComponent>();
        summaries.Add(new OrderSummary
        {
            OrderId = entityId,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmountCents / 100m
        });
    }
    
    return summaries;
}
```

## 6. Handle Relationships

Link entities in parent-child relationships:

```csharp
public async Task CreateOrderWithItems(CreateOrderRequest request)
{
    // Create order
    var orderId = Guid.NewGuid();
    var orderHandler = _dataContext.For<OrderHandler>(orderId);
    await orderHandler.CreateOrder(request.OrderNumber, request.CustomerId, request.TotalAmount);
    
    // Create order items
    foreach (var item in request.Items)
    {
        var itemId = Guid.NewGuid();
        var itemHandler = _dataContext.For<OrderItemHandler>(itemId);
        await itemHandler.CreateItem(item.ProductId, item.Quantity, item.Price);
        
        // Link item to order
        await _dataContext.LinkRelationship(orderId, itemId, "Order", "OrderItem");
    }
    
    // Later, retrieve all items for an order
    var itemIds = await _dataContext.Children(orderId);
}
```

## Key Concepts

1. **Components** are pure data structures (records)
2. **Handlers** contain all business logic
3. **DataContext** manages persistence and queries
4. **Entities** are just IDs - components give them meaning
5. **Relationships** connect entities in hierarchies

## Next Steps

- [Handler Development Guide](../guides/handler-development.md) - Advanced handler patterns
- [Testing Strategies](../guides/testing-strategies.md) - Testing your handlers
- [API Reference](../api-reference/datacontext-api.md) - Complete DataContext API

## Common Patterns

### Validation Pattern
```csharp
// Use Guard for input validation
Guard.AgainstEmpty(value, nameof(value));
Guard.AgainstNull(obj, nameof(obj));

// Use Ensure for business rules
Ensure(order.Status == "PENDING", "Invalid order status");
```

### Concurrency Pattern
```csharp
// Use TryCommit for user-initiated updates
if (!await DataContext.TryCommit(component))
{
    // Reload and retry
    var current = await handler.Get();
    current.PropertyToUpdate = newValue;
    await DataContext.TryCommit(current);
}
```

### Audit Pattern
```csharp
// Log important operations
await _auditService.LogEvent("OperationName", entityId, new
{
    // Include relevant context
    UserId = currentUserId,
    OldValue = oldValue,
    NewValue = newValue,
    Timestamp = DateTime.UtcNow
});
```