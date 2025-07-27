# Jarvis - Entity Component System SDK for .NET

Build scalable, maintainable .NET applications with a battle-tested Entity Component System (ECS) architecture. Jarvis provides a handler-based approach to business logic with built-in security, auditing, and multi-tenant support.

## 🚀 Quick Start (5 minutes)

```csharp
// 1. Define your component (data)
public record OrderComponent : IComponent, IVersionedComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    public string OrderNumber { get; set; }
    public string CustomerId { get; set; }
    public string Status { get; set; }
    public int TotalAmountCents { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public string ShippingAddress { get; set; }
    public bool IsPaid { get; set; }
    public int? Version { get; set; }
}

// 2. Create a handler (business logic)
public class OrderHandler : ComponentHandler<OrderComponent>
{
    public async Task<OrderComponent> CreateOrder(
        string orderNumber, 
        string customerId, 
        decimal totalAmount, 
        string shippingAddress)
    {
        // Validation
        Guard.AgainstEmpty(orderNumber, nameof(orderNumber));
        Guard.AgainstEmpty(customerId, nameof(customerId));
        
        var order = new OrderComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = OwnerEntityId,
            OrderNumber = orderNumber,
            CustomerId = customerId,
            Status = "PENDING",
            TotalAmountCents = (int)(totalAmount * 100),
            ShippingAddress = shippingAddress,
            OrderDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.TryCommit(order);
        return order;
    }
}

// 3. Use it in your application
var handler = dataContext.For<OrderHandler>(entityId);
var order = await handler.CreateOrder("ORD-2024-001", "CUST-123", 150.00m, "123 Main St");
```

## 🎯 Why Jarvis?

### The Problem
Traditional architectures mix data models with business logic, leading to:
- Tightly coupled code that's hard to test
- Business logic scattered across services and controllers
- Difficult state management and entity relationships
- Security concerns with direct database access

### The Solution
Jarvis separates concerns using the Entity Component System pattern:
- **Entities**: Just IDs - the identity of things
- **Components**: Pure data structures (records)
- **Handlers**: All business logic in one place
- **Systems**: Orchestration between handlers

## 📦 What's Included

### Three Complementary SDKs

1. **`core.jarvis`** - The main ECS framework
   - Handler pattern for business logic
   - Entity querying and relationships
   - Transaction support
   - Audit trail integration

2. **`core.jarvis.data`** - Low-level data access
   - JWT-based Row Level Security
   - Type-safe PostgreSQL operations
   - Automatic PascalCase to snake_case mapping
   - Connection pooling

3. **`core.jarvis.api`** - REST API layer
   - Azure Functions integration
   - Authentication endpoints
   - OpenAPI/Swagger documentation
   - Security middleware

## 🏃 Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL (or Docker)
- Azure Functions Core Tools (for API layer)

### Installation

```bash
# Clone and build
git clone https://github.com/yourusername/jarvis.git
cd jarvis
dotnet build

# Start PostgreSQL
docker-compose up -d

# Run tests to verify
dotnet test
```

### Your First Handler

```csharp
// 1. Register handlers in DI
services.RegisterJarvis(LogLevel.Information, Configuration);
services.AddScoped<IComponentHandler, OrderHandler>();
services.AddScoped<OrderHandler>(); // Also register concrete type

// 2. Use in your controller/service
public class OrderService
{
    private readonly IDataContext _dataContext;
    
    public async Task<OrderDto> ProcessOrder(Guid orderId)
    {
        // Get handler for specific entity
        var handler = _dataContext.For<OrderHandler>(orderId);
        
        // Execute business logic
        await handler.ConfirmOrder();
        
        // Return result
        var order = await handler.Get();
        return MapToDto(order);
    }
}
```

## 🔥 Key Features

### 1. Handler Pattern
Encapsulate all business logic in testable handlers:
```csharp
public class InvoiceHandler : ComponentHandler<InvoiceTestComponent>
{
    public async Task<InvoiceTestComponent> GenerateFromOrder(Guid orderId)
    {
        // Complex business logic in one place
        var orderHandler = DataContext.For<OrderHandler>(orderId);
        var order = await orderHandler.Get();
        Ensure(order.Status == "CONFIRMED", "Can only invoice confirmed orders");
        
        var invoice = new InvoiceTestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = OwnerEntityId,
            WorkOrderId = orderId,
            Amount = order.TotalAmountCents,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = "PENDING",
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.Commit(invoice);
        
        return invoice;
    }
}
```

### 2. Entity Queries
Find entities across components with type-safe queries:
```csharp
// Find all confirmed orders with invoices
var invoicedOrders = await dataContext.Query()
    .WithAll<OrderComponent>(o => o.Status == "CONFIRMED")
    .WithAll<InvoiceTestComponent>(i => true)
    .ToEntityComponents();

// Find orders without invoices (need billing)
var unbilledOrders = await dataContext.Query()
    .WithAll<OrderComponent>(o => true)
    .WithNone<InvoiceTestComponent>(i => true)
    .ToEntityComponents();
```

### 3. Built-in Security
JWT-based Row Level Security at the SDK level:
```csharp
// Users only see their own data automatically
var client = new PgClient(connection);
client.JWT(userToken); // Sets user context

// This query is automatically filtered by user's tenant/permissions
var orders = await client.From<Order>()
    .Filter("status", "eq", "active")
    .Get();
```

### 4. Relationship Management
Track entity relationships and hierarchies:
```csharp
// Link entities (e.g., work order → invoices)
await dataContext.LinkRelationship(workOrderId, invoiceId, "WorkOrder", "Invoice");

// Query relationships
var invoices = await dataContext.Children(workOrderId);
var workOrder = await dataContext.Parent(invoiceId);

// Check relationships
var isChild = await dataContext.ChildOf(invoiceId, workOrderId);

// Get full hierarchy
var ancestors = await dataContext.Ancestors(invoiceId);
var descendants = await dataContext.Descendants(workOrderId);
```

## 📚 Documentation

- [Quickstart Guide](docs/getting-started/installation.md) - Get running in minutes
- [Architecture Overview](docs/architecture/ecs-principles.md) - Understand the patterns
- [Handler Development](docs/guides/handler-development.md) - Write effective handlers
- [API Reference](docs/api-reference/core-interfaces.md) - Complete API documentation
- [Examples](core.jarvis.tests/Examples/) - Real-world usage patterns

## 🧪 Testing

The framework is designed for testability:

```csharp
[Fact]
public async Task OrderHandler_ConfirmOrder_ChangesStatus()
{
    // Arrange
    var entityId = Guid.NewGuid();
    var handler = TestDataContext().For<OrderHandler>(entityId);
    await handler.CreateOrder("TEST-001", "CUST-123", 100m, "Test Address");
    
    // Act
    var result = await handler.ConfirmOrder();
    
    // Assert
    Assert.True(result);
    var order = await handler.Get();
    Assert.Equal("CONFIRMED", order.Status);
}
```

## 🏗️ Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Controllers   │────▶│   IDataContext   │────▶│    Handlers     │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                                 │                         │
                                 ▼                         ▼
                        ┌──────────────────┐     ┌─────────────────┐
                        │  Entity Query    │     │   Components    │
                        └──────────────────┘     └─────────────────┘
                                 │                         │
                                 ▼                         ▼
                        ┌──────────────────┐     ┌─────────────────┐
                        │    PgClient      │────▶│   PostgreSQL    │
                        └──────────────────┘     └─────────────────┘
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

## 🆘 Support

- [Documentation](docs/)
- [GitHub Issues](https://github.com/yourusername/jarvis/issues)
- [Discussions](https://github.com/yourusername/jarvis/discussions)