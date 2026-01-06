# Building Handlers

Handlers are the heart of Jarvis. Every piece of business logic lives in a handler.

## 1. Handler Anatomy

All handlers inherit from `ComponentHandler<T>`:

```csharp
public abstract class ComponentHandler<TComponent> : IComponentHandler<TComponent>
    where TComponent : class, IComponent, new()
{
    public Guid OwnerEntityId { get; }           // Entity this handler operates on
    protected IDataContext DataContext { get; }  // Database access
    protected ILogger Logger { get; }            // Logging

    public virtual async Task<TComponent> Get();              // Retrieve component
    protected async Task<TComponent?> GetOrDefault();         // Get or null
    protected async Task<bool> TryCommit(TComponent c);       // Save changes
    protected void Ensure(bool condition, string message);    // Validate rules
}
```

### Key Methods

| Method | Purpose | Throws |
|--------|---------|--------|
| `Get()` | Retrieve component for `OwnerEntityId` | `EntityNotFoundException` |
| `GetOrDefault()` | Retrieve or return null | Never |
| `TryCommit(component)` | Save to database | `ConcurrencyException` |
| `Ensure(condition, message)` | Assert business rule | `BusinessRuleException` |

## 2. Creating a Handler

### Step 1: Define the Component

```csharp
public record OrderComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }

    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public int TotalAmountCents { get; set; }
    public bool IsPaid { get; set; }
}
```

### Step 2: Create the Handler

```csharp
public class OrderHandler : ComponentHandler<OrderComponent>
{
    public OrderHandler(IDataContext dataContext, ILogger<OrderHandler> logger)
        : base(dataContext, logger) { }

    // Business operations go here
}
```

### Step 3: Register in DI

```csharp
services.AddScoped<OrderHandler>();
```

### Step 4: Use the Handler

```csharp
var handler = dataContext.For<OrderHandler>(orderId);
await handler.ConfirmOrder();
```

## 3. Validation with Ensure()

`Ensure()` throws `BusinessRuleException` when condition is false.

### Basic Validation

```csharp
public async Task ConfirmOrder()
{
    var order = await Get();
    Ensure(order.Status == "PENDING", "Only pending orders can be confirmed");

    order.Status = "CONFIRMED";
    await TryCommit(order);
}
```

### Validation Chains

```csharp
public async Task ShipOrder(string trackingNumber)
{
    var order = await Get();

    Ensure(order.Status == "CONFIRMED", "Order must be confirmed before shipping");
    Ensure(order.IsPaid, "Cannot ship unpaid orders");
    Ensure(!string.IsNullOrWhiteSpace(trackingNumber), "Tracking number required");

    order.Status = "SHIPPED";
    order.TrackingNumber = trackingNumber;
    await TryCommit(order);
}
```

### Conditional Validation

```csharp
public async Task CancelOrder(string reason)
{
    var order = await Get();

    Ensure(order.Status != "SHIPPED", "Cannot cancel shipped orders");
    Ensure(order.Status != "CANCELLED", "Order already cancelled");

    if (order.IsPaid)
    {
        Ensure(reason.Length >= 10, "Paid orders require detailed cancellation reason");
    }

    order.Status = "CANCELLED";
    await TryCommit(order);
}
```

### Input Validation with Guard

For input validation, use `Guard` before business rules:

```csharp
public async Task<OrderComponent> CreateOrder(string orderNumber, string customerId, decimal amount)
{
    // Input validation - throws ValidationException
    Guard.AgainstEmpty(orderNumber, nameof(orderNumber));
    Guard.AgainstEmpty(customerId, nameof(customerId));
    Guard.AgainstOutOfRange(amount, 0.01m, 1000000m, nameof(amount));

    // Business rule validation - throws BusinessRuleException
    var existing = await GetOrDefault();
    Ensure(existing == null, "Order already exists for this entity");

    var order = new OrderComponent
    {
        OwnerEntityId = OwnerEntityId,
        OrderNumber = orderNumber,
        CustomerId = customerId,
        TotalAmountCents = (int)(amount * 100)
    };

    await TryCommit(order);
    return order;
}
```

## 4. Business Operations

### Create

```csharp
public async Task<OrderComponent> CreateOrder(string orderNumber, string customerId)
{
    var existing = await GetOrDefault();
    Ensure(existing == null, "Order already exists");

    var order = new OrderComponent
    {
        OwnerEntityId = OwnerEntityId,
        OrderNumber = orderNumber,
        CustomerId = customerId,
        Status = "PENDING"
    };

    await TryCommit(order);
    return order;
}
```

### Update

```csharp
public async Task<OrderComponent> UpdateShippingAddress(string address)
{
    var order = await Get();
    Ensure(order.Status == "PENDING", "Can only update address on pending orders");

    order.ShippingAddress = address;
    order.LastUpdated = DateTime.UtcNow;

    await TryCommit(order);
    return order;
}
```

### State Transitions

```csharp
public async Task<OrderComponent> ConfirmOrder()
{
    var order = await Get();
    Ensure(order.Status == "PENDING", "Only pending orders can be confirmed");

    order.Status = "CONFIRMED";
    order.LastUpdated = DateTime.UtcNow;

    await TryCommit(order);
    return order;
}
```

## 5. Common Patterns

### State Machine

```csharp
public class WorkOrderHandler : ComponentHandler<WorkOrderComponent>
{
    private static readonly Dictionary<string, string[]> ValidTransitions = new()
    {
        ["DRAFT"] = new[] { "SUBMITTED", "CANCELLED" },
        ["SUBMITTED"] = new[] { "APPROVED", "REJECTED" },
        ["APPROVED"] = new[] { "IN_PROGRESS", "CANCELLED" },
        ["IN_PROGRESS"] = new[] { "COMPLETED", "CANCELLED" },
        ["COMPLETED"] = Array.Empty<string>(),
        ["CANCELLED"] = Array.Empty<string>()
    };

    private async Task TransitionTo(string newStatus)
    {
        var workOrder = await Get();

        var allowed = ValidTransitions.GetValueOrDefault(workOrder.Status, Array.Empty<string>());
        Ensure(allowed.Contains(newStatus), $"Cannot transition from {workOrder.Status} to {newStatus}");

        workOrder.Status = newStatus;
        await TryCommit(workOrder);
    }

    public Task Submit() => TransitionTo("SUBMITTED");
    public Task Approve() => TransitionTo("APPROVED");
    public Task Complete() => TransitionTo("COMPLETED");
    public Task Cancel() => TransitionTo("CANCELLED");
}
```

### Record Immutability

```csharp
public async Task<AccountComponent> Activate()
{
    var account = await Get();

    if (account.IsActive) return account;

    var updated = account with
    {
        IsActive = true,
        LastUpdated = DateTime.UtcNow
    };

    await TryCommit(updated);
    return updated;
}
```

## 6. Anti-Patterns

### Anemic Handlers

```csharp
// BAD: No business logic
public class OrderHandler
{
    public async Task<OrderComponent> GetOrder() => await Get();
    public async Task SaveOrder(OrderComponent order) => await TryCommit(order);
}

// GOOD: Business logic in handler
public class OrderHandler
{
    public async Task<OrderComponent> ConfirmOrder()
    {
        var order = await Get();
        Ensure(order.Status == "PENDING", "Only pending orders can be confirmed");
        order.Status = "CONFIRMED";
        await TryCommit(order);
        return order;
    }
}
```

### Try-Catch in Handlers

```csharp
// BAD: Swallowing exceptions
public async Task<bool> ConfirmOrder()
{
    try
    {
        var order = await Get();
        order.Status = "CONFIRMED";
        await TryCommit(order);
        return true;
    }
    catch (Exception) { return false; }  // Hides errors!
}

// GOOD: Let exceptions bubble
public async Task ConfirmOrder()
{
    var order = await Get();
    Ensure(order.Status == "PENDING", "Only pending orders can be confirmed");
    order.Status = "CONFIRMED";
    await TryCommit(order);
}
```

### Business Logic in Wrong Layer

```csharp
// BAD: Logic in system
public class OrderSystem
{
    public async Task ProcessOrder(Guid orderId)
    {
        var order = await _dataContext.Get<OrderComponent>(orderId);
        if (order.Status == "PENDING")  // Business logic in system!
        {
            order.Status = "CONFIRMED";
        }
    }
}

// GOOD: Handler decides
public class OrderSystem
{
    public async Task ProcessOrder(Guid orderId)
    {
        var handler = _dataContext.For<OrderHandler>(orderId);
        await handler.ConfirmOrder();
    }
}
```

### Validation in Components

```csharp
// BAD: Validation in component
public record OrderComponent : IComponent
{
    private string _status = "PENDING";
    public string Status
    {
        get => _status;
        set
        {
            if (value != "PENDING" && value != "CONFIRMED")
                throw new Exception("Invalid status");  // NO!
            _status = value;
        }
    }
}

// GOOD: Validation in handler
public async Task SetStatus(string status)
{
    Ensure(ValidStatuses.Contains(status), $"Invalid status: {status}");
    var order = await Get();
    order.Status = status;
    await TryCommit(order);
}
```

## Summary

| Do | Do Not |
|----|--------|
| Put ALL business logic in handlers | Put logic in systems or components |
| Use `Ensure()` for business rules | Use try-catch to handle errors |
| Use `Guard` for input validation | Validate inputs in components |
| Use `TryCommit()` to save | Bypass with direct commits |
| Let exceptions bubble up | Return success/failure booleans |

**Next:** [04-systems.md](04-systems.md) - Orchestrating handlers with systems
