# Table Manager Guide

## Overview

The Table Manager is a core component of the Jarvis ECS framework that provides automatic database schema management for components. It ensures your PostgreSQL database schema stays synchronized with your C# component definitions without requiring manual DDL scripts or migrations for most changes.

### Key Features

- **Automatic Table Creation**: Creates tables for new components on first use
- **Schema Evolution**: Safely adds new columns as components evolve
- **Type Validation**: Detects incompatible type changes and prevents data corruption
- **Versioning Support**: Handles special requirements for `IVersionedComponent` implementations
- **Index Management**: Creates commonly needed indexes automatically
- **Safe Operations**: Uses PostgreSQL's native features to prevent race conditions

## How It Works

The Table Manager operates on a simple principle: your C# component definitions are the source of truth for database schema. When a component is used, the Table Manager:

1. **Checks Table Existence**: Queries PostgreSQL's information schema
2. **Creates Missing Tables**: Generates and executes CREATE TABLE statements
3. **Validates Existing Tables**: Compares expected vs actual schema
4. **Updates Schema**: Adds missing columns with appropriate defaults
5. **Reports Conflicts**: Throws exceptions for incompatible changes requiring manual intervention

### Schema Generation Process

```
Component Type → Property Analysis → Column Mapping → SQL Generation → Execution
```

Each property in your component is analyzed for:
- Data type (mapped to PostgreSQL types)
- Nullability
- Default values
- Special attributes (primary key, unique constraints)

## Usage Examples

### Basic Usage

```csharp
public class OrderService
{
    private readonly ITableManager _tableManager;
    private readonly IDataContext _dataContext;
    
    public OrderService(ITableManager tableManager, IDataContext dataContext)
    {
        _tableManager = tableManager;
        _dataContext = dataContext;
    }
    
    public async Task Initialize()
    {
        // Ensure table exists before any operations
        await _tableManager.EnsureTableExists<OrderComponent>();
    }
    
    public async Task<OrderComponent> CreateOrder(Guid entityId, string orderNumber)
    {
        // Table is guaranteed to exist after EnsureTableExists
        var order = new OrderComponent
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = entityId,
            OrderNumber = orderNumber,
            Status = OrderStatus.Pending,
            LastUpdated = DateTime.UtcNow
        };
        
        await _dataContext.Set(order);
        return order;
    }
}
```

### Application Startup Validation

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.RegisterJarvis();
        
        // Register your component handlers
        services.AddScoped<OrderHandler>();
        services.AddScoped<InvoiceHandler>();
        services.AddScoped<CustomerHandler>();
    }
    
    public async Task Configure(IApplicationBuilder app, ITableManager tableManager)
    {
        // Validate all component tables at startup
        var componentTypes = new[]
        {
            typeof(OrderComponent),
            typeof(InvoiceComponent),
            typeof(CustomerComponent)
        };
        
        foreach (var componentType in componentTypes)
        {
            var method = typeof(ITableManager)
                .GetMethod("EnsureTableExists")
                .MakeGenericMethod(componentType);
                
            await (Task)method.Invoke(tableManager, null);
        }
    }
}
```

### Handler Integration

```csharp
public class InvoiceHandler : ComponentHandler<InvoiceComponent>
{
    private readonly ITableManager _tableManager;
    private bool _tableValidated = false;
    
    public InvoiceHandler(IDataContext dataContext, ITableManager tableManager) 
        : base(dataContext)
    {
        _tableManager = tableManager;
    }
    
    protected override async Task<InvoiceComponent> OnCreate(
        Guid entityId, 
        InvoiceComponent component)
    {
        // Ensure table exists on first operation
        if (!_tableValidated)
        {
            await _tableManager.EnsureTableExists<InvoiceComponent>();
            _tableValidated = true;
        }
        
        return await base.OnCreate(entityId, component);
    }
}
```

### Handling Schema Evolution

```csharp
// Version 1: Original component
public record OrderComponent : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    public string OrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
}

// Version 2: Added new properties
public record OrderComponent : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    public string OrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
    
    // New properties - will be added as columns automatically
    public string? CustomerEmail { get; set; }     // Nullable - no issues
    public OrderStatus Status { get; set; }        // Enum with default
    public DateTime CreatedAt { get; set; }        // Non-nullable with default
}
```

## Integration with IVersionedComponent

Components implementing `IVersionedComponent` receive special handling:

### Automatic Version Column

```csharp
public record AuditedOrderComponent : IVersionedComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }  // Required by IVersionedComponent
    
    public string OrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
}
```

The Table Manager automatically:
1. Adds a `version` column with default value of 1
2. Creates the column as nullable integer
3. Handles version incrementation through DataContext integration

### Migration Path for Existing Tables

When adding versioning to an existing component:

```csharp
// Step 1: Original component
public record ProductComponent : IComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Name { get; set; }
}

// Step 2: Add IVersionedComponent
public record ProductComponent : IVersionedComponent  // Changed interface
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }  // Added property
    public string Name { get; set; }
}
```

The Table Manager will:
- Detect the missing `version` column
- Add it with `DEFAULT 1`
- Allow seamless transition to versioned tracking

## Configuration Options

### Service Registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register Jarvis with Table Manager
    services.RegisterJarvis();
    
    // Table Manager is automatically registered as:
    // services.TryAddScoped<ITableManager, PostgreSqlTableManager>();
    
    // Optional: Override with custom implementation
    services.Replace(ServiceDescriptor.Scoped<ITableManager, CustomTableManager>());
}
```

### Logging Configuration

Enable detailed logging for troubleshooting:

```csharp
services.AddLogging(builder =>
{
    builder
        .AddFilter("core.jarvis.Data.Schema", LogLevel.Debug)
        .AddFilter("core.jarvis.Data.Schema.PostgreSqlTableManager", LogLevel.Trace);
});
```

### Connection Requirements

The Table Manager requires a PostgreSQL connection with these permissions:
- `CREATE TABLE` - For new component tables
- `ALTER TABLE` - For adding columns
- `SELECT` on `information_schema` - For schema inspection

## Best Practices

### 1. Initialization Strategy

**Recommended: Lazy Initialization**
```csharp
public class ComponentService<TComponent> where TComponent : class, IComponent, new()
{
    private readonly ITableManager _tableManager;
    private readonly SemaphoreSlim _initLock = new(1);
    private bool _initialized = false;
    
    private async Task EnsureInitialized()
    {
        if (_initialized) return;
        
        await _initLock.WaitAsync();
        try
        {
            if (!_initialized)
            {
                await _tableManager.EnsureTableExists<TComponent>();
                _initialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }
}
```

### 2. Error Handling

```csharp
public async Task<bool> TryInitializeComponent<TComponent>() 
    where TComponent : class, IComponent, new()
{
    try
    {
        await _tableManager.EnsureTableExists<TComponent>();
        return true;
    }
    catch (SchemaValidationException ex)
    {
        _logger.LogError(ex, 
            "Schema validation failed for {Component}. Manual migration required for field '{Field}': {ExpectedType} → {ActualType}",
            typeof(TComponent).Name, ex.FieldName, ex.ExpectedType, ex.ActualType);
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to initialize table for {Component}", 
            typeof(TComponent).Name);
        throw;
    }
}
```

### 3. Component Design Guidelines

**DO:**
- Add nullable properties for optional data
- Provide sensible defaults for required properties
- Use appropriate data types that map cleanly to PostgreSQL
- Plan for schema evolution from the start

**DON'T:**
- Change property types after deployment
- Remove properties without migration plan
- Use complex nested objects without considering JSONB storage
- Assume table existence without validation

### 4. Testing Strategies

```csharp
[TestFixture]
public class ComponentSchemaTests
{
    private ITableManager _tableManager;
    
    [Test]
    public async Task NewComponent_ShouldCreateTable()
    {
        // Arrange
        await DropTableIfExists("test_component_component");
        
        // Act
        await _tableManager.EnsureTableExists<TestComponent>();
        
        // Assert
        var tableExists = await CheckTableExists("test_component_component");
        Assert.IsTrue(tableExists);
    }
    
    [Test]
    public async Task ExistingComponent_WithNewProperty_ShouldAddColumn()
    {
        // Arrange
        await _tableManager.EnsureTableExists<TestComponentV1>();
        
        // Act - Use extended version
        await _tableManager.EnsureTableExists<TestComponentV2>();
        
        // Assert
        var columns = await GetTableColumns("test_component_component");
        Assert.That(columns, Contains.Item("new_property"));
    }
}
```

### 5. Performance Considerations

- **Table validation is fast** - Uses PostgreSQL system catalogs
- **Column addition is safe** - Uses IF NOT EXISTS patterns
- **No locking issues** - PostgreSQL handles DDL concurrency
- **Cache validation results** - Avoid repeated checks in hot paths

## Troubleshooting

### Common Issues and Solutions

#### 1. Permission Denied
```
Error: permission denied for schema public
```
**Solution**: Ensure database user has CREATE TABLE permission:
```sql
GRANT CREATE ON SCHEMA public TO your_user;
```

#### 2. Type Mismatch
```
SchemaValidationException: Expected type 'TEXT' but found 'INTEGER'
```
**Solution**: Create manual migration:
```sql
-- Option 1: Rename old column and add new
ALTER TABLE order_component RENAME COLUMN status TO status_old;
ALTER TABLE order_component ADD COLUMN status TEXT;

-- Option 2: Convert data type (if compatible)
ALTER TABLE order_component 
ALTER COLUMN status TYPE TEXT USING status::TEXT;
```

#### 3. Column Already Exists
```
Error: column "version" of relation "product_component" already exists
```
**Solution**: This is handled automatically in latest version. For older versions, manually check column existence.

#### 4. Table Name Conflicts
```
Component 'OrderComponent' expects table 'order_component_component'
```
**Solution**: Follow naming convention or implement custom table name resolver.

### Debug Logging

Enable trace logging to see exact SQL being generated:

```csharp
logger.MinimumLevel.Override("core.jarvis.Data.Schema", LogEventLevel.Verbose)
```

Example output:
```
[DBG] Ensuring table exists for component OrderComponent -> table order_component_component
[DBG] Table order_component_component exists, validating schema
[DBG] Field customer_email not found in table order_component_component, will add it
[INF] Adding 1 missing fields to table order_component_component
[DBG] Ensured column customer_email exists in table order_component_component
```

## Advanced Topics

### Custom Type Mappings

The Table Manager includes default mappings for common types:

| C# Type | PostgreSQL Type | Notes |
|---------|----------------|-------|
| `Guid` | `UUID` | Primary keys and references |
| `string` | `TEXT` | Unlimited length |
| `int` | `INTEGER` | 32-bit integers |
| `long` | `BIGINT` | 64-bit integers |
| `decimal` | `DECIMAL(18,8)` | Financial calculations |
| `bool` | `BOOLEAN` | True/false values |
| `DateTime` | `TIMESTAMPTZ` | Always with timezone |
| `Enum` | `INTEGER` | Stored as numeric value |
| `Guid[]` | `UUID[]` | PostgreSQL arrays |
| Complex objects | `JSONB` | Serialized as JSON |

### Index Strategy

The Table Manager automatically creates indexes for:
- `owner_entity_id` - For entity queries
- `last_updated` - For temporal queries
- Properties ending with `Status` - For filtering
- Properties ending with `Type` - For categorization

### Future Enhancements

Planned improvements include:
- Column rename detection via attributes
- Custom constraint support
- Partition table support
- Migration script generation
- Schema diff reporting