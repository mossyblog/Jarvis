# DataContext Usage Guide

## Introduction

The `DataContext` is your primary interface for working with entities and components in the Jarvis ECS framework. This guide covers practical usage patterns and real-world examples.

## Getting Started

### Basic Setup

```csharp
public class MyService
{
    private readonly IDataContext _dataContext;
    
    public MyService(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }
}
```

### Handler Resolution

The most common operation is resolving handlers to work with components:

```csharp
// Get a handler for a specific entity
var invoiceHandler = _dataContext.For<InvoiceHandler>(invoiceId);

// Work with the component through the handler
var invoice = await invoiceHandler.Get();
if (invoice != null)
{
    await invoiceHandler.UpdateStatus("Paid");
}
```

## Common Scenarios

### Creating New Entities

```csharp
public async Task<Guid> CreateOrder(decimal amount, string customerName)
{
    var orderId = Guid.NewGuid();
    
    // Create order component
    var order = new OrderComponent
    {
        Id = orderId,
        OwnerEntityId = orderId,
        Amount = amount,
        CustomerName = customerName,
        Status = "New",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    
    // Save through handler
    var handler = _dataContext.For<OrderHandler>(orderId);
    await handler.Save(order);
    
    return orderId;
}
```

### Working with Related Entities

```csharp
public async Task AddItemsToOrder(Guid orderId, List<OrderItem> items)
{
    foreach (var item in items)
    {
        // Create item entity
        var itemId = Guid.NewGuid();
        item.Id = itemId;
        item.OwnerEntityId = itemId;
        
        // Save item
        await _dataContext.Commit(item);
        
        // Link to order
        await _dataContext.LinkRelationship(
            parentId: orderId, 
            childId: itemId,
            parentType: "Order",
            childType: "OrderItem"
        );
    }
}
```

### Querying Across Components

```csharp
public async Task<List<Guid>> FindHighValuePaidOrders()
{
    return await _dataContext.Query()
        .WithAll<OrderComponent, PaymentComponent>()
        .Where<OrderComponent>(o => o.Amount > 1000)
        .Where<OrderComponent>(o => o.Status == "Paid")
        .Where<PaymentComponent>(p => p.PaymentDate != null)
        .ToEntityIds();
}
```

### Handling Concurrency

```csharp
public async Task<bool> UpdateInvoiceAmount(Guid invoiceId, decimal newAmount)
{
    var handler = _dataContext.For<InvoiceHandler>(invoiceId);
    var invoice = await handler.Require();
    
    invoice.Amount = newAmount;
    invoice.UpdatedAt = DateTime.UtcNow;
    
    // Use TryCommit for user-initiated updates
    if (!await _dataContext.TryCommit(invoice))
    {
        // Another user modified the record
        return false;
    }
    
    return true;
}
```

## Advanced Patterns

### Batch Operations

```csharp
public async Task ProcessMonthlyInvoices()
{
    // Find all invoices due this month
    var dueInvoices = await _dataContext.Query()
        .WithAll<InvoiceComponent>()
        .Where<InvoiceComponent>(i => 
            i.DueDate >= DateTime.UtcNow.StartOfMonth() &&
            i.DueDate <= DateTime.UtcNow.EndOfMonth() &&
            i.Status == "Pending")
        .ToEntityIds();
    
    // Process in batches
    foreach (var batch in dueInvoices.Chunk(100))
    {
        var tasks = batch.Select(async invoiceId =>
        {
            var handler = _dataContext.For<InvoiceHandler>(invoiceId);
            await handler.SendReminder();
        });
        
        await Task.WhenAll(tasks);
    }
}
```

### Working with Hierarchies

```csharp
public async Task<decimal> CalculateOrderTotal(Guid orderId)
{
    // Get all child items
    var itemIds = await _dataContext.Children(orderId);
    
    decimal total = 0;
    foreach (var itemId in itemIds)
    {
        var itemHandler = _dataContext.For<OrderItemHandler>(itemId);
        var item = await itemHandler.Get();
        if (item != null)
        {
            total += item.Quantity * item.UnitPrice;
        }
    }
    
    return total;
}
```

### Transaction Patterns

```csharp
public async Task TransferInvoice(Guid invoiceId, Guid fromCustomer, Guid toCustomer)
{
    // Note: This shows the pattern, but true transactions aren't implemented yet
    // Each operation is still atomic
    
    try
    {
        // Update invoice
        var invoiceHandler = _dataContext.For<InvoiceHandler>(invoiceId);
        var invoice = await invoiceHandler.Require();
        invoice.CustomerId = toCustomer;
        await _dataContext.Commit(invoice);
        
        // Update relationships
        await _dataContext.UnlinkRelationship(fromCustomer, invoiceId);
        await _dataContext.LinkRelationship(toCustomer, invoiceId, "Customer", "Invoice");
        
        // Create audit record
        var audit = new AuditRecord
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = invoiceId,
            Action = "Transfer",
            FromCustomer = fromCustomer,
            ToCustomer = toCustomer,
            Timestamp = DateTime.UtcNow
        };
        await _dataContext.Commit(audit);
    }
    catch (Exception ex)
    {
        // Handle rollback logic
        throw;
    }
}
```

### Historical Queries

```csharp
public async Task<List<InvoiceSnapshot>> GetInvoiceHistory(Guid invoiceId)
{
    var snapshots = await _dataContext.Snapshots()
        .ForEntity(invoiceId)
        .ForComponent<InvoiceComponent>()
        .OrderByDescending(s => s.Timestamp)
        .Execute();
    
    return snapshots.Select(s => new InvoiceSnapshot
    {
        Version = s.Version,
        Timestamp = s.Timestamp,
        Amount = s.Data.GetProperty("Amount").GetDecimal(),
        Status = s.Data.GetProperty("Status").GetString()
    }).ToList();
}
```

## Performance Considerations

### Efficient Querying

```csharp
// Good: Single query with filters
var results = await _dataContext.Query()
    .WithAll<InvoiceComponent, PaymentComponent>()
    .Where<InvoiceComponent>(i => i.Amount > 1000)
    .ToEntityIds();

// Avoid: Multiple individual queries
var allInvoices = await GetAllInvoices();
var filtered = new List<Guid>();
foreach (var id in allInvoices)
{
    var invoice = await GetInvoice(id); // N+1 problem!
    if (invoice.Amount > 1000)
        filtered.Add(id);
}
```

### Batch Loading

```csharp
public async Task<Dictionary<Guid, InvoiceComponent>> LoadInvoices(List<Guid> ids)
{
    // Handler implementation should batch load
    var tasks = ids.Select(async id =>
    {
        var handler = _dataContext.For<InvoiceHandler>(id);
        var invoice = await handler.Get();
        return (id, invoice);
    });
    
    var results = await Task.WhenAll(tasks);
    return results.Where(r => r.invoice != null)
                  .ToDictionary(r => r.id, r => r.invoice!);
}
```

## Error Handling

### Component Not Found

```csharp
try
{
    var handler = _dataContext.For<InvoiceHandler>(invoiceId);
    var invoice = await handler.Require(); // Throws if not found
}
catch (ComponentNotFoundException ex)
{
    // Handle missing component
    _logger.LogWarning($"Invoice {invoiceId} not found");
    return NotFound();
}
```

### Concurrency Conflicts

```csharp
public async Task<IActionResult> UpdateInvoice(Guid id, UpdateInvoiceDto dto)
{
    var handler = _dataContext.For<InvoiceHandler>(id);
    var invoice = await handler.Get();
    
    if (invoice == null)
        return NotFound();
    
    // Map DTO to component
    invoice.Amount = dto.Amount;
    invoice.DueDate = dto.DueDate;
    
    if (!await _dataContext.TryCommit(invoice))
    {
        return Conflict("The invoice was modified by another user. Please refresh and try again.");
    }
    
    return Ok();
}
```

## Testing with DataContext

### Integration Tests

```csharp
public class InvoiceServiceTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateInvoice_Should_LinkToCustomer()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new CustomerComponent
        {
            Id = customerId,
            OwnerEntityId = customerId,
            Name = "Test Customer"
        };
        await TestDataContext().Commit(customer);
        
        // Act
        var invoiceId = await CreateInvoice(customerId, 100m);
        
        // Assert
        var parent = await TestDataContext().Parent(invoiceId);
        parent.ShouldBe(customerId);
        
        var children = await TestDataContext().Children(customerId);
        children.ShouldContain(invoiceId);
    }
}
```

## Best Practices Summary

1. **Always use handlers** for business logic
2. **Use TryCommit** for user-initiated updates
3. **Batch operations** when possible
4. **Handle concurrency** gracefully
5. **Use relationships** for entity hierarchies
6. **Query efficiently** with IEntityQuery
7. **Test with real database** operations

## Common Pitfalls to Avoid

1. **Don't bypass handlers** for business operations
2. **Don't ignore concurrency** - use TryCommit
3. **Don't create N+1 queries** - use batch operations
4. **Don't forget to set OwnerEntityId** on new components
5. **Don't mix component IDs and entity IDs**

## Next Steps

- Learn about [Handler Development](handler-development.md)
- Explore [Query API](../api-reference/query-api.md)
- Understand [Snapshot Architecture](../architecture/snapshot-architecture.md)