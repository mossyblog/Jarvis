# Schema Management API Reference

## Overview

The Jarvis ECS framework includes an automatic schema management system that validates and creates database tables for component types. This ensures your database schema stays in sync with your component definitions without manual intervention.

> **Note**: For a comprehensive guide on using the Table Manager, see the [Table Manager Guide](/docs/guides/table-manager.md).

## Core Interfaces

### ITableManager

```csharp
namespace core.jarvis.Data.Schema;

/// <summary>
/// Manages database table schema validation and creation for components.
/// </summary>
public interface ITableManager
{
    /// <summary>
    /// Ensures the table for the specified component type exists and matches the expected schema.
    /// - If table doesn't exist: Creates it
    /// - If table exists but missing fields: Adds missing fields  
    /// - If table exists but fields have wrong data types: Throws exception requiring manual migration
    /// </summary>
    /// <typeparam name="TComponent">The component type to validate/create table for</typeparam>
    /// <exception cref="SchemaValidationException">Thrown when existing fields have incompatible data types</exception>
    Task EnsureTableExists<TComponent>() where TComponent : class, IComponent, new();
    
    /// <summary>
    /// Validates all registered component tables in the system.
    /// </summary>
    Task ValidateAllComponentTables();
}
```

### PostgreSqlTableManager

The default implementation that provides PostgreSQL-specific schema management:

```csharp
public class PostgreSqlTableManager : ITableManager
{
    public PostgreSqlTableManager(IPgClient pgClient, ILogger<PostgreSqlTableManager> logger);
    
    // Implements ITableManager interface with PostgreSQL-specific logic
    public async Task EnsureTableExists<TComponent>();
    public async Task ValidateAllComponentTables();
}
```

## Automatic Table Creation

The schema management system automatically:

1. **Creates missing tables** based on component properties
2. **Adds missing columns** when components are extended
3. **Validates data types** and throws exceptions for incompatible changes
4. **Handles versioned components** by adding version columns when needed

## Usage Examples

### Individual Component

```csharp
// Ensure table exists for a specific component
await tableManager.EnsureTableExists<InvoiceComponent>();
```

### Application Startup

```csharp
// Validate all component tables during application startup
await tableManager.ValidateAllComponentTables();
```

### With Dependency Injection

```csharp
public class OrderHandler : ComponentHandler<OrderComponent>
{
    private readonly ITableManager _tableManager;
    
    public OrderHandler(IDataContext dataContext, ITableManager tableManager) 
        : base(dataContext)
    {
        _tableManager = tableManager;
    }
    
    public async Task Initialize()
    {
        // Ensure table exists before operations
        await _tableManager.EnsureTableExists<OrderComponent>();
    }
}
```

## Property Mapping

The system maps C# properties to PostgreSQL columns using these rules:

### Naming Conventions

- **PascalCase to snake_case**: `OrderNumber` → `order_number`
- **Component suffix**: `OrderComponent` → `order_component_component` table
- **Built-in properties**: 
  - `Id` → `id` (PRIMARY KEY)
  - `OwnerEntityId` → `owner_entity_id` (UNIQUE)
  - `LastUpdated` → `last_updated`
  - `Version` → `version` (for `IVersionedComponent`)

### Data Type Mappings

| C# Type | PostgreSQL Type | Example Property |
|---------|----------------|------------------|
| `Guid` | `UUID` | `public Guid Id { get; init; }` |
| `string` | `TEXT` | `public string Name { get; set; }` |
| `int` | `INTEGER` | `public int Count { get; set; }` |
| `long` | `BIGINT` | `public long BigNumber { get; set; }` |
| `decimal` | `DECIMAL(18,8)` | `public decimal Amount { get; set; }` |
| `double` | `DOUBLE PRECISION` | `public double Rate { get; set; }` |
| `float` | `REAL` | `public float Percentage { get; set; }` |
| `bool` | `BOOLEAN` | `public bool IsActive { get; set; }` |
| `DateTime` | `TIMESTAMPTZ` | `public DateTime CreatedAt { get; set; }` |
| `Enum` | `INTEGER` | `public OrderStatus Status { get; set; }` |
| `Guid[]` | `UUID[]` | `public Guid[] TagIds { get; set; }` |
| `string[]` | `TEXT[]` | `public string[] Tags { get; set; }` |
| `int[]` | `INTEGER[]` | `public int[] Values { get; set; }` |
| Complex objects | `JSONB` | `public Address Location { get; set; }` |

## Default Values

The system automatically sets appropriate defaults based on property names and types:

| Property Name | Default Value | SQL Expression |
|--------------|---------------|----------------|
| `Id` | New UUID | `gen_random_uuid()` |
| `OwnerEntityId` | Empty GUID | `'00000000-0000-0000-0000-000000000000'::uuid` |
| `LastUpdated` | Current time | `NOW()` |
| `CreatedAt` | Current time | `NOW()` |
| `UpdatedAt` | Current time | `NOW()` |
| `Version` | 1 | `1` |
| `bool` types | False | `FALSE` |
| Numeric types | 0 | `0` |
| `string` types | Empty string | `''` |
| Array types | Empty array | `'{}'` |
| Enum types | 0 | `0` |

## Schema Validation

### Compatible Changes (Auto-Applied)

- **Adding new nullable columns**: Properties with nullable types or reference types
- **Adding new non-nullable columns with defaults**: System provides appropriate defaults
- **Adding version column**: When implementing `IVersionedComponent` on existing tables
- **Creating indexes**: For common query patterns (owner_entity_id, last_updated, etc.)

### Incompatible Changes (Manual Migration Required)

- **Changing column data types**: e.g., `string` to `int`
- **Making nullable columns non-nullable**: Requires handling existing NULL values
- **Removing columns**: Not detected, requires manual DROP COLUMN
- **Renaming columns**: Appears as drop + add, requires manual migration
- **Changing constraints**: Primary key, unique constraints, etc.

## Exception Types

### SchemaValidationException

```csharp
public class SchemaValidationException : Exception
{
    public string TableName { get; }
    public string FieldName { get; }
    public string ExpectedType { get; }
    public string ActualType { get; }
    
    // Thrown when field types are incompatible
    public SchemaValidationException(
        string tableName, 
        string fieldName, 
        string expectedType, 
        string actualType);
        
    // Thrown for general schema validation failures
    public SchemaValidationException(
        string tableName, 
        string message);
}
```

### Error Handling Example

```csharp
try 
{
    await tableManager.EnsureTableExists<OrderComponent>();
}
catch (SchemaValidationException ex)
{
    // Manual migration required
    _logger.LogError(ex, 
        "Schema validation failed for table '{TableName}', field '{FieldName}': " +
        "Expected {ExpectedType} but found {ActualType}",
        ex.TableName, ex.FieldName, ex.ExpectedType, ex.ActualType);
        
    // Could trigger migration workflow or alert
    await notificationService.AlertDatabaseTeam(ex);
    
    throw new InvalidOperationException(
        $"Database schema requires manual migration for {ex.TableName}. " +
        $"See logs for details.", ex);
}
```

## Supporting Types

### ComponentFieldInfo

```csharp
public class ComponentFieldInfo
{
    public string PropertyName { get; set; }      // C# property name
    public string ColumnName { get; set; }        // PostgreSQL column name
    public string PostgreSqlType { get; set; }    // PostgreSQL data type
    public bool IsNullable { get; set; }          // NULL constraint
    public bool IsPrimaryKey { get; set; }        // PRIMARY KEY constraint
    public bool IsUnique { get; set; }            // UNIQUE constraint
    public string? DefaultValue { get; set; }     // DEFAULT expression
    public Type PropertyType { get; set; }        // .NET type
}
```

### DatabaseColumnInfo

```csharp
public class DatabaseColumnInfo
{
    public string ColumnName { get; set; }        // Actual column name
    public string DataType { get; set; }          // Actual data type
    public bool IsNullable { get; set; }          // NULL allowed
    public string? DefaultValue { get; set; }     // Current default
    public bool IsPrimaryKey { get; set; }        // Is primary key
    public bool IsUnique { get; set; }            // Has unique constraint
}
```

## Service Registration

### Default Registration

The Table Manager is automatically registered when you call `RegisterJarvis()`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Registers ITableManager as scoped service
    services.RegisterJarvis();
    
    // Equivalent to:
    // services.TryAddScoped<ITableManager, PostgreSqlTableManager>();
}
```

### Custom Implementation

```csharp
// Replace with custom implementation
services.Replace(ServiceDescriptor.Scoped<ITableManager, CustomTableManager>());

// Or register alongside for specific scenarios
services.AddScoped<ICustomTableManager, CustomTableManager>();
```

## Automatic Indexing

The Table Manager creates indexes for commonly queried fields:

```sql
-- Created automatically
CREATE INDEX idx_[table]_owner_entity_id ON [table](owner_entity_id);
CREATE INDEX idx_[table]_last_updated ON [table](last_updated);
CREATE INDEX idx_[table]_[field] ON [table]([field]) -- For fields ending with 'Status' or 'Type'
```

## Performance Characteristics

- **Table existence check**: ~1-5ms (system catalog query)
- **Schema validation**: ~5-20ms (depends on column count)
- **Column addition**: ~10-50ms per column
- **Index creation**: ~50-200ms (depends on table size)
- **No table locking**: Uses PostgreSQL's transactional DDL

## Thread Safety

The `PostgreSqlTableManager` is thread-safe for concurrent calls:
- Multiple threads can call `EnsureTableExists` for the same table
- PostgreSQL handles concurrent DDL operations
- Race conditions are prevented at the database level

## Related Documentation

- [Table Manager Guide](/docs/guides/table-manager.md) - Comprehensive usage guide
- [Component Development](/docs/guides/component-development.md) - Creating components
- [Versioned Components](/docs/api-reference/versioned-components.md) - Using IVersionedComponent
- [Data Context API](/docs/api-reference/data-context.md) - Component persistence
