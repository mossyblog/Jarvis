# Jarvis - Entity-Handler-System (EHS) Framework for .NET

**A Domain-Driven Architecture for Complex Business Applications**

Jarvis implements a unique Entity-Handler-System (EHS) architecture - a three-layer pattern that cleanly separates data, business logic, and orchestration. Unlike traditional Entity Component Systems (ECS) designed for games, Jarvis is optimized for business applications with rich domain logic, multi-tenancy, and Azure Functions deployment.

> **Note**: This is NOT a game engine ECS. Jarvis uses entity/component terminology but is architected for business domains, not real-time systems.

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
        // Validation - exceptions bubble up naturally
        Guard.AgainstEmpty(orderNumber, nameof(orderNumber));
        Guard.AgainstEmpty(customerId, nameof(customerId));
        
        // Business rule validation
        Ensure(totalAmount > 0, "Order amount must be positive");
        
        var order = new OrderComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = EntityId,
            OrderNumber = orderNumber,
            CustomerId = customerId,
            Status = "PENDING",
            TotalAmountCents = (int)(totalAmount * 100),
            ShippingAddress = shippingAddress,
            OrderDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        await TryCommit(order);
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

### The Solution: Three-Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Azure Functions                         │
│            (Thin HTTP adapters - NO business logic)         │
├─────────────────────────────────────────────────────────────┤
│                         Systems                             │
│        (Orchestration, transactions, cross-entity ops)      │
├─────────────────────────────────────────────────────────────┤
│                        Handlers                             │
│         (Business logic for single component types)         │
├─────────────────────────────────────────────────────────────┤
│                   Entities & Components                     │
│              (Data layer - pure data, no logic)            │
└─────────────────────────────────────────────────────────────┘
```

- **Entities**: Simple containers with just a Guid ID
- **Components**: Pure data structures (C# records) - no behavior
- **Handlers**: ALL business logic for a single component type (entity-scoped)
- **Systems**: Orchestrate multiple handlers, manage transactions
- **Functions**: Thin HTTP adapters only - NO business logic

## 📦 What's Included

### Three Complementary SDKs

1. **`core.jarvis`** - The main EHS framework
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

### 1. Clean Exception Bubbling
Exceptions flow naturally through layers with centralized handling:
```csharp
// In Handler - just throw, no try-catch needed
public async Task ConfirmOrder()
{
    var order = await Get();
    
    // Business rules throw domain exceptions
    if (order.Status != "PENDING")
        throw new BusinessRuleException("ORDER_INVALID_STATE", 
            "Can only confirm pending orders");
    
    if (!order.IsPaid)
        throw new BusinessRuleException("ORDER_NOT_PAID", 
            "Order must be paid before confirmation");
    
    // Update and save
    order.Status = "CONFIRMED";
    await TryCommit(order);
}

// In API - exceptions handled by middleware
public async Task<HttpResponseData> ConfirmOrder(HttpRequestData req, Guid orderId)
{
    // No try-catch needed - middleware handles all exceptions
    var handler = _dataContext.For<OrderHandler>(orderId);
    await handler.ConfirmOrder();
    
    return req.CreateResponse(HttpStatusCode.OK);
}
```

### 2. Handler Pattern
Encapsulate all business logic in testable handlers:
```csharp
public class InvoiceHandler : ComponentHandler<InvoiceTestComponent>
{
    public async Task<InvoiceTestComponent> GenerateFromOrder(Guid orderId)
    {
        // Complex business logic in one place
        var orderHandler = DataContext.For<OrderHandler>(orderId);
        var order = await orderHandler.Get();
        
        // Business rules throw exceptions - no error codes
        Ensure(order.Status == "CONFIRMED", "Can only invoice confirmed orders");
        
        var invoice = new InvoiceTestComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = EntityId,
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

### 3. Entity Queries with Component Access
Query entities and access their components directly:
```csharp
// Get entities as List<Entity> with component access
var entities = await dataContext.Query()
    .With<OrderComponent>(o => o.Status == "CONFIRMED")
    .ToList();

// Access components from entities
foreach (var entity in entities)
{
    var order = await entity.Get<OrderComponent>();
    var invoice = await entity.Get<InvoiceComponent>();
    
    if (invoice == null)
    {
        // Create invoice for unbilled order
        var handler = dataContext.For<InvoiceHandler>(entity.Id);
        await handler.GenerateFromOrder(entity.Id);
    }
}

// Find orders without invoices (need billing)
var unbilledOrders = await dataContext.Query()
    .WithAll<OrderComponent>(o => true)
    .WithNone<InvoiceTestComponent>(i => true)
    .ToList();

// Query with ordering by component properties
var prioritizedOrders = await dataContext.Query()
    .WithAll<OrderComponent>(o => o.Status == "PENDING")
    .OrderBy<OrderComponent>(o => o.Priority)              // Primary sort
    .ThenByDescending<OrderComponent>(o => o.TotalAmount)  // Secondary sort
    .ToList();
```

### 4. Built-in Security
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

### 5. Relationship Management
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
- [Architecture Overview](docs/architecture/jarvis-overview.md) - Understand the EHS patterns
- [System Pattern Guide](docs/architecture/system-pattern-technical-whitepaper.md) - Deep dive into handler orchestration
- [API Reference](docs/api-reference/) - Complete API documentation
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

## 🏗️ Architecture: Three-Layer EHS Pattern

```
┌─────────────────────────────────────────────────────────┐
│                   Azure Functions                      │
│                  (HTTP Endpoints)                      │
└─────────────────────┬───────────────────────────────────┘
                      │ Thin HTTP adapters only
                      ▼
┌─────────────────────────────────────────────────────────┐
│                     Systems                            │
│              (Orchestration Layer)                     │
│  - Coordinate handlers    - Manage transactions        │
│  - Business workflows     - Cross-cutting concerns     │
└─────────────────────┬───────────────────────────────────┘
                      │ Orchestrates business logic
                      ▼
┌─────────────────────────────────────────────────────────┐
│                    Handlers                            │
│                (Business Logic)                        │
│  - Single component focus  - Domain operations         │
│  - Clean exception flow    - CRUD operations           │
└─────────────────────┬───────────────────────────────────┘
                      │ Operates on pure data
                      ▼
┌─────────────────────────────────────────────────────────┐
│              Entities & Components                     │
│                  (Data Layer)                          │
│  - Entities: Guid IDs      - Components: Records       │
│  - PostgreSQL storage      - JWT-based security        │
└─────────────────────────────────────────────────────────┘
```

### Key Architectural Principles

1. **Clean Separation**: Each layer has a single, clear responsibility
2. **Exception Bubbling**: No try-catch blocks - exceptions flow up naturally
3. **Handler Focus**: One handler per component type, encapsulates all logic
4. **System Orchestration**: Systems coordinate multiple handlers for workflows
5. **Thin Functions**: Azure Functions only handle HTTP concerns

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

## 🆘 Support

- [Documentation](docs/)
- [GitHub Issues](https://github.com/yourusername/jarvis/issues)
- [Discussions](https://github.com/yourusername/jarvis/discussions)