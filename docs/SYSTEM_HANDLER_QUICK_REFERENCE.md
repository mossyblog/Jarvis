# System + Handler Quick Reference

## At a Glance

| Component | Purpose | Returns | Accepts |
|-----------|---------|---------|---------|
| **System** | Orchestrates workflows | `List<IComponent>` or `IComponent` | Primitives, request objects, JSON |
| **Handler** | CRUD for one component | `TComponent` | `IComponent` or `Guid` |
| **Function** | HTTP adapter | `HttpResponseData` | `HttpRequestData` |
| **Component** | Data structure | N/A | N/A |

## Do's and Don'ts

### ✅ DO
- Systems call handlers directly via `_dataContext.For<THandler>(entityId)`
- Handlers accept complete component objects as parameters
- Systems return `List<IComponent>` for multi-component operations
- Functions delegate all logic to Systems
- Use `record` types for components

### ❌ DON'T
- Have Handlers do orchestration
- Use ExecuteHandler or similar double-orchestration patterns
- Have Handlers accept individual field parameters
- Create custom result objects (use IComponent collections)
- Put business logic in Functions
- Have Handlers call other Handlers

## Quick Examples

### Creating a System
```csharp
public class InvoiceSystem
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<InvoiceSystem> _logger;

    public InvoiceSystem(IDataContext dataContext, ILogger<InvoiceSystem> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    public async Task<List<IComponent>> CreateInvoice(InvoiceRequest request)
    {
        // Validate
        if (request.Amount <= 0)
            throw new ValidationException("Amount must be positive");

        // Create via handler
        var handler = _dataContext.For<InvoiceHandler>(request.CustomerId);
        var invoice = await handler.CreateInvoice(new Invoice
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            CreatedAt = DateTime.UtcNow
        });

        return new List<IComponent> { invoice };
    }
}
```

### Creating a Handler
```csharp
public class InvoiceHandler : ComponentHandler<Invoice>
{
    public InvoiceHandler(IDataContext dataContext, ILogger<InvoiceHandler> logger)
        : base(dataContext, logger)
    {
    }

    public async Task<Invoice> CreateInvoice(Invoice newInvoice)
    {
        var invoice = newInvoice with { OwnerEntityId = OwnerEntityId };
        await DataContext.Commit(invoice);
        return invoice;
    }

    public async Task<Invoice> UpdateAmount(decimal newAmount)
    {
        var invoice = await Get() ?? throw new InvalidOperationException("Invoice not found");
        var updated = invoice with 
        { 
            Amount = newAmount, 
            UpdatedAt = DateTime.UtcNow 
        };
        await DataContext.Commit(updated);
        return updated;
    }
}
```

### Creating a Function
```csharp
public class InvoiceFunction
{
    private readonly InvoiceSystem _invoiceSystem;

    [Function("CreateInvoice")]
    public async Task<HttpResponseData> CreateInvoice(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var requestBody = await req.ReadAsStringAsync();
        var components = await _invoiceSystem.CreateInvoice(requestBody);
        
        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(components);
        return response;
    }
}
```

### Creating a Component
```csharp
public record Invoice : BaseComponent
{
    public Guid CustomerId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public InvoiceStatus Status { get; init; }
    public DateTime DueDate { get; init; }
}
```

## Common Patterns

### Pattern: Validation in System
```csharp
public async Task<List<IComponent>> CreateUser(UserRequest request)
{
    // Validate in System
    if (string.IsNullOrEmpty(request.Email))
        throw new ValidationException("Email required");
        
    if (!IsValidEmail(request.Email))
        throw new ValidationException("Invalid email format");
        
    // Check business rules in System
    var existing = await _dataContext.Query()
        .WithAll<User>(u => u.Email == request.Email)
        .Any();
        
    if (existing)
        throw new BusinessRuleException("EMAIL_EXISTS", "Email already registered");
        
    // Create via Handler
    var handler = _dataContext.For<UserHandler>(Guid.NewGuid());
    var user = await handler.CreateUser(new User { Email = request.Email });
    
    return new List<IComponent> { user };
}
```

### Pattern: Multi-Component Operation
```csharp
public async Task<List<IComponent>> CreateOrderWithItems(OrderRequest request)
{
    var components = new List<IComponent>();
    
    // Create order
    var orderHandler = _dataContext.For<OrderHandler>(request.CustomerId);
    var order = await orderHandler.CreateOrder(new Order
    {
        Id = Guid.NewGuid(),
        CustomerId = request.CustomerId,
        Status = OrderStatus.Pending
    });
    components.Add(order);
    
    // Create items
    foreach (var item in request.Items)
    {
        var itemHandler = _dataContext.For<OrderItemHandler>(order.Id);
        var orderItem = await itemHandler.CreateItem(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = item.ProductId,
            Quantity = item.Quantity
        });
        components.Add(orderItem);
    }
    
    return components;
}
```

### Pattern: Update Operation
```csharp
public async Task<IComponent> UpdateUserProfile(Guid userId, ProfileUpdate update)
{
    var handler = _dataContext.For<ProfileHandler>(userId);
    var profile = await handler.Get() 
        ?? throw new BusinessRuleException("NOT_FOUND", "Profile not found");
    
    var updated = profile with
    {
        Name = update.Name ?? profile.Name,
        Bio = update.Bio ?? profile.Bio,
        UpdatedAt = DateTime.UtcNow
    };
    
    await _dataContext.Commit(updated);
    return updated;
}
```

## Service Registration

```csharp
// In ServiceCollectionExtensions.cs
services.AddScoped<InvoiceSystem>();
services.AddScoped<OrderSystem>();
services.AddScoped<UserSystem>();

// Handlers registered as both interface and concrete
services.AddScoped<IComponentHandler, InvoiceHandler>();
services.AddScoped<InvoiceHandler>();
```

## Testing Quick Reference

### Test System
```csharp
[Fact]
public async Task System_Should_Create_Components()
{
    // Arrange
    var system = _serviceProvider.GetRequiredService<InvoiceSystem>();
    var request = new InvoiceRequest { CustomerId = Guid.NewGuid(), Amount = 100 };
    
    // Act
    var components = await system.CreateInvoice(request);
    
    // Assert
    components.Count.ShouldBe(1);
    var invoice = components[0] as Invoice;
    invoice.ShouldNotBeNull();
    invoice.Amount.ShouldBe(100);
    
    // Cleanup
    TrackEntity(invoice.OwnerEntityId);
}
```

### Test Handler
```csharp
[Fact]
public async Task Handler_Should_Create_Component()
{
    // Arrange
    var entityId = Guid.NewGuid();
    var handler = TestDataContext().For<InvoiceHandler>(entityId);
    
    // Act
    var invoice = await handler.CreateInvoice(new Invoice
    {
        Id = Guid.NewGuid(),
        Amount = 100
    });
    
    // Assert
    invoice.OwnerEntityId.ShouldBe(entityId);
    
    // Cleanup
    TrackEntity(entityId);
}
```

## Debugging Tips

1. **Component not saving?**
   - Check `OwnerEntityId` is set
   - Verify `Id` is not empty Guid
   - Ensure `await DataContext.Commit()` is called

2. **Handler not found?**
   - Verify handler is registered in DI
   - Check both interface and concrete registration

3. **Validation errors?**
   - Validation should be in System, not Handler
   - Use `ValidationException` for input errors
   - Use `BusinessRuleException` for business rules

4. **Test cleanup failing?**
   - Always use `TrackEntity(entityId)`
   - Track the OwnerEntityId, not component Id

## Migration Checklist

- [ ] Move orchestration from Handlers to Systems
- [ ] Remove ExecuteHandler methods
- [ ] Change Handler methods to accept IComponent parameters
- [ ] Replace custom result objects with List<IComponent>
- [ ] Move validation from Handlers to Systems
- [ ] Update Functions to call Systems directly
- [ ] Update tests to use new patterns
- [ ] Register Systems in DI container