# Systems: Workflow Orchestration

Systems coordinate multiple handlers to complete workflows. They contain zero business logic.

## 1. What Systems Do

Systems are the orchestration layer:
- Coordinate multiple handlers in sequence
- Manage transaction boundaries for atomic operations
- Let exceptions bubble up (no try-catch)
- Operate across multiple entities when needed

Systems do NOT:
- Contain business logic (no if/else on business conditions)
- Validate data (handlers do that)
- Catch and swallow exceptions

## 2. Creating a System

```csharp
public class OrderFulfillmentSystem
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<OrderFulfillmentSystem> _logger;

    public OrderFulfillmentSystem(IDataContext dataContext, ILogger<OrderFulfillmentSystem> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    public async Task FulfillOrder(Guid orderId)
    {
        var orderHandler = _dataContext.For<OrderHandler>(orderId);
        await orderHandler.ConfirmOrder();

        var inventoryHandler = _dataContext.For<InventoryHandler>(orderId);
        await inventoryHandler.ReserveItems();

        var paymentHandler = _dataContext.For<PaymentHandler>(orderId);
        await paymentHandler.ChargeCustomer();
    }
}
```

Register in DI: `services.AddScoped<OrderFulfillmentSystem>();`

## 3. Real Example: OrderSystem

```csharp
public class OrderSystem
{
    private readonly IDataContext _dataContext;

    public OrderSystem(IDataContext dataContext) => _dataContext = dataContext;

    public async Task PlaceOrder(Guid orderId, Guid customerId)
    {
        // OrderHandler validates: order exists, status is DRAFT, has line items
        var orderHandler = _dataContext.For<OrderHandler>(orderId);
        var order = await orderHandler.SubmitOrder();

        // InventoryHandler validates: items in stock, not already reserved
        var inventoryHandler = _dataContext.For<InventoryHandler>(orderId);
        await inventoryHandler.ReserveItems();

        // PaymentHandler validates: customer has payment method, credit limit
        var paymentHandler = _dataContext.For<PaymentHandler>(customerId);
        await paymentHandler.AuthorizePayment(order.TotalAmountCents);
    }

    public async Task CancelOrder(Guid orderId, string reason)
    {
        var orderHandler = _dataContext.For<OrderHandler>(orderId);
        await orderHandler.CancelOrder(reason);

        var inventoryHandler = _dataContext.For<InventoryHandler>(orderId);
        await inventoryHandler.ReleaseReservation();

        var paymentHandler = _dataContext.For<PaymentHandler>(orderId);
        await paymentHandler.RefundIfCharged();
    }
}
```

## 4. Transaction Boundaries

Use `ExecuteInTransaction` when operations must succeed or fail together:

```csharp
public async Task<List<IComponent>> RegisterUser(string email, string password)
{
    var userEntityId = Guid.NewGuid();

    return await _dataContext.ExecuteInTransaction(async () =>
    {
        var accountHandler = _dataContext.For<AccountHandler>(userEntityId);
        var account = await accountHandler.CreateAccount(email, password);

        var profileHandler = _dataContext.For<ProfileHandler>(userEntityId);
        var profile = await profileHandler.CreateWithDefaults(email);

        return new List<IComponent> { account, profile };
    });
}
```

| Scenario | Use Transaction? |
|----------|------------------|
| Related components that must exist together | Yes |
| Financial operations (payment + record) | Yes |
| Operations that can be retried independently | No |

## 5. When to Use Systems

**Use a System when:** workflow involves 2+ handlers, needs transactions, or requires cross-entity coordination.

**Use Handler directly when:** single component operation or API maps 1:1 to handler method.

```csharp
// Handler directly - single operation
var handler = _dataContext.For<OrderHandler>(orderId);
await handler.UpdateNotes(notes);

// System - coordinated workflow
await _orderSystem.PlaceOrder(orderId, customerId);
```

Decision flow:
```
Single-component operation? --> Yes --> Use handler directly
                           --> No  --> Need atomic updates? --> Yes --> System + Transaction
                                                            --> No  --> System without transaction
```

## 6. Anti-Patterns

### Business Logic in Systems

```csharp
// BAD: System making business decisions
public async Task ProcessOrder(Guid orderId)
{
    var order = await _dataContext.Query().WithAll<OrderComponent>().FirstOrDefault();
    if (order.TotalAmountCents > 10000)  // Business logic - belongs in handler!
        order.RequiresApproval = true;
}

// GOOD: Handler contains the rules
public async Task ProcessOrder(Guid orderId)
{
    var handler = _dataContext.For<OrderHandler>(orderId);
    await handler.ProcessOrder();
}
```

### Catching Exceptions

```csharp
// BAD: Swallowing exceptions
public async Task<bool> PlaceOrder(Guid orderId)
{
    try {
        await _dataContext.For<OrderHandler>(orderId).SubmitOrder();
        return true;
    }
    catch { return false; }  // Error details lost!
}

// GOOD: Let exceptions bubble
public async Task PlaceOrder(Guid orderId)
{
    await _dataContext.For<OrderHandler>(orderId).SubmitOrder();
    // BusinessRuleException -> 400, EntityNotFoundException -> 404
}
```

### Validation in Systems

```csharp
// BAD: Validation in system
if (quantity <= 0) throw new ValidationException("...");  // Belongs in handler!

// GOOD: Handler validates
await handler.AddItem(quantity);  // Handler validates quantity
```

## Summary

| Aspect | Systems | Handlers |
|--------|---------|----------|
| Purpose | Orchestration | Business logic |
| Contains | Handler coordination | Validation, state transitions |
| Exceptions | Let bubble | Throw domain exceptions |
| Transactions | Manages boundaries | Participates in transactions |

**Next:** [05-testing.md](05-testing.md) - Testing without mocks
