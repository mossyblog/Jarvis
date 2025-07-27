# Jarvis ECS Examples

Practical, runnable examples demonstrating common patterns and best practices.

## Example Categories

### Basic Examples

#### 1. Order Management System
Location: `core.jarvis.tests/Examples/OrderHandler.cs`

Demonstrates:
- Creating handlers with validation
- State transitions (pending → confirmed → shipped)
- Audit trail integration
- Error handling patterns

```csharp
// Create and confirm an order
var handler = dataContext.For<OrderHandler>(entityId);
await handler.CreateOrder("ORD-001", "CUST-123", 99.99m, "123 Main St");
await handler.ConfirmOrder();
```

#### 2. Blog Content System
Location: `core.jarvis.tests/Examples/Blog/`

Demonstrates:
- Complex handler interactions
- Multi-component entities
- Content generation patterns
- Hierarchical data (blog → posts)

```csharp
// Generate blog posts
var blog = await dataContext.For<BlogHandler>(blogId);
var post = await blog.GeneratePost(new BlogPostGenerationRequest 
{ 
    Topic = "ECS Architecture" 
});
```

### Advanced Examples

#### 3. Work Order & Invoice System
Location: `core.jarvis.tests/Examples/WorkOrderInvoiceExample.cs`

Demonstrates:
- Entity relationships (parent-child)
- Cross-entity queries
- Business workflow implementation
- Transaction boundaries

```csharp
// Link work order to invoices
await dataContext.LinkRelationship(workOrderId, invoiceId, "WorkOrder", "Invoice");
var invoices = await dataContext.Children(workOrderId);
```

#### 4. Multi-Tenant Security
Location: `core.jarvis.data.tests/Tables/RowLevelSecurityTests.cs`

Demonstrates:
- JWT-based row level security
- Tenant isolation
- Role-based access control
- Security policy implementation

## Common Patterns

### Handler Registration Pattern
```csharp
// Startup.cs
services.RegisterJarvis(LogLevel.Information, Configuration);

// Register handlers manually
services.AddScoped<IComponentHandler, OrderHandler>();
services.AddScoped<OrderHandler>();

// Or register all handlers from assembly
services.RegisterAllComponentHandlersAndQueriesFromAssembly(
    typeof(OrderHandler).Assembly);
```

### Query Pattern Examples
```csharp
// Find entities with specific components
var results = await dataContext.Query()
    .WithAll<OrderComponent, PaymentComponent>()
    .WithNone<RefundComponent>()
    .ToEntityComponents();

// Filter by component properties
var shippedOrders = results
    .Where(kvp => kvp.Value.Get<OrderComponent>()?.Status == "SHIPPED")
    .Select(kvp => kvp.Value.Get<OrderComponent>())
    .ToList();

// Get specific entity with all its components
var entityComponents = await dataContext.Query()
    .WithEntity(entityId)
    .ToEntityComponents();
```

### Relationship Pattern
```csharp
// Create parent-child relationships
await dataContext.LinkRelationship(orderId, invoiceId, "Order", "Invoice");
await dataContext.LinkRelationship(orderId, paymentId, "Order", "Payment");

// Query relationships
var invoices = await dataContext.Children(orderId);
var order = await dataContext.Parent(invoiceId);

// Check full hierarchy
var allRelated = await dataContext.Descendants(orderId);
```

### Error Handling Pattern
```csharp
public async Task<bool> ProcessPayment(decimal amount)
{
    try
    {
        var order = await GetRequired();
        
        // Business validation
        Ensure(order.Status == "CONFIRMED", "Order must be confirmed");
        Ensure(amount >= order.TotalAmount, "Payment insufficient");
        
        // Process payment
        await DataContext.Commit(new PaymentComponent
        {
            OrderId = OwnerEntityId,
            Amount = amount,
            ProcessedAt = DateTime.UtcNow
        });
        
        return true;
    }
    catch (BusinessRuleException ex)
    {
        Logger.LogWarning("Payment failed: {Reason}", ex.Message);
        return false;
    }
}
```

## Running the Examples

### Prerequisites
1. PostgreSQL running (use `docker-compose up -d`)
2. Database initialized (run tests once)
3. .NET 8 SDK installed

### Run Examples
```bash
# Run all example tests
dotnet test --filter "FullyQualifiedName~Examples"

# Run specific example
dotnet test --filter "WorkOrderInvoiceExample"

# With logging
dotnet test --logger "console;verbosity=detailed"
```

## Creating Your Own Examples

Template for new examples:

```csharp
public class MyExampleTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: What this example demonstrates
    /// PURPOSE: Why this pattern is useful
    /// BUSINESS CONTEXT: Real-world scenario
    /// </summary>
    [Fact]
    public async Task Example_DescriptiveName()
    {
        // Arrange - Set up test data
        var entityId = Guid.NewGuid();
        
        // Act - Perform operations
        var handler = TestDataContext.For<MyHandler>(entityId);
        var result = await handler.DoSomething();
        
        // Assert - Verify results
        result.ShouldNotBeNull();
        result.Status.ShouldBe("Expected");
    }
}
```

## Additional Resources

- [Handler Development Guide](../../01_CurrentState/Components/handler-development.md)
- [Testing Strategies](../../07_Projects/testing-strategies.md)
- [Full Test Suite](../../../core.jarvis.tests/)
- [API Examples](../../../core.jarvis.api/test-api.ps1)

## Tips

1. **Start Simple**: Begin with OrderHandler example
2. **Use IntelliSense**: Handler methods are discoverable
3. **Check Tests**: Examples include comprehensive tests
4. **Debug Friendly**: All examples can be debugged step-by-step
5. **Real Patterns**: Examples reflect production usage