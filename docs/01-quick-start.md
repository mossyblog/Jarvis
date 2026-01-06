# Quick Start Guide

Get a Jarvis EHS application running in 10 minutes.

## Prerequisites

- .NET 8.0 SDK
- Docker Desktop
- Azure Functions Core Tools v4

## Setup

```bash
git clone https://github.com/your-org/jarvis.git
cd jarvis
docker-compose up -d
dotnet build
dotnet test
```

## Your First Handler

Jarvis uses components for data and handlers for business logic.

### 1. Component (Data Model)

```csharp
public record ProductComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int PriceCents { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}
```

### 2. Handler (Business Logic)

```csharp
public class ProductHandler : DataContextComponentHandler<ProductComponent>
{
    public ProductHandler(IDataContext dataContext, ILogger<ProductHandler> logger)
        : base(dataContext, logger) { }

    public async Task<ProductComponent> Create(string name, string sku, decimal price)
    {
        Ensure(!string.IsNullOrEmpty(name), "Product name is required");
        Ensure(!string.IsNullOrEmpty(sku), "SKU is required");
        Ensure(price > 0, "Price must be positive");

        var product = new ProductComponent
        {
            Name = name,
            Sku = sku,
            PriceCents = (int)(price * 100)
        };

        await TryCommit(product);
        return product;
    }

    public async Task<ProductComponent> UpdateStock(int quantity)
    {
        var product = await Get();

        Ensure(product.IsActive, "Cannot update stock for inactive product");
        Ensure(quantity >= 0, "Stock quantity cannot be negative");

        product.StockQuantity = quantity;
        await TryCommit(product);

        return product;
    }
}
```

### 3. Registration

```csharp
services.RegisterJarvis();
services.AddScoped<ProductHandler>();
```

## Using the Handler

```csharp
public class ProductService
{
    private readonly IDataContext _dataContext;

    public ProductService(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<ProductComponent> CreateProduct(string name, string sku, decimal price)
    {
        var entityId = Guid.NewGuid();
        var handler = _dataContext.For<ProductHandler>(entityId);
        return await handler.Create(name, sku, price);
    }
}
```

## Verify It Works

```csharp
[Fact]
public async Task ProductHandler_Create_SetsCorrectValues()
{
    var entityId = Guid.NewGuid();
    var handler = _dataContext.For<ProductHandler>(entityId);

    var product = await handler.Create("Widget", "WGT-001", 29.99m);

    Assert.Equal("Widget", product.Name);
    Assert.Equal("WGT-001", product.Sku);
    Assert.Equal(2999, product.PriceCents);
}
```

Run: `dotnet test --filter "ProductHandler_Create"`

## Key Patterns

| Pattern | Purpose |
|---------|---------|
| `DataContextComponentHandler<T>` | Base class for handlers |
| `IDataContext.For<T>(entityId)` | Get handler bound to entity |
| `Ensure(condition, message)` | Business rule validation |
| `TryCommit(component)` | Save component |
| `Get()` | Retrieve component |

## Next Steps

Next: [02-core-concepts.md](02-core-concepts.md) - Understanding the EHS architecture
