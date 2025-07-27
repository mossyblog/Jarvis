# Versioned Components

## Overview

The `IVersionedComponent` interface provides optional versioning support for components that require stronger concurrency control and audit trail capabilities. Components implementing this interface automatically receive version-based optimistic locking and enhanced snapshot tracking.

## IVersionedComponent Interface

```csharp
namespace core.jarvis.data;

/// <summary>
/// Interface for components that support versioning and snapshot tracking.
/// Components that implement this interface will have automatic version incrementation during updates.
/// </summary>
public interface IVersionedComponent
{
    /// <summary>
    /// Version number for optimistic concurrency control. Increments on each update.
    /// Maps to version column in database.
    /// </summary>
    int? Version { get; set; }
}
```

## When to Use Versioned Components

Use `IVersionedComponent` for components that:

- Require strict concurrency control (e.g., financial records, inventory counts)
- Need detailed audit trails with version history
- Are frequently updated by multiple users
- Have complex business rules that depend on data consistency

## Implementation Example

```csharp
public record InvoiceComponent : IComponent, IVersionedComponent
{
    public Guid Id { get; init; }
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Versioning support
    public int? Version { get; set; }
    
    // Business properties
    public string InvoiceNumber { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
}
```

## Database Schema

Versioned components automatically get a `version` column:

```sql
CREATE TABLE invoices (
    id uuid PRIMARY KEY,
    owner_entity_id uuid NOT NULL,
    last_updated timestamptz DEFAULT NOW(),
    version integer DEFAULT 1,
    invoice_number text,
    amount decimal,
    status text
);
```

## Concurrency Control

### Version-Based Optimistic Locking

For versioned components, the DataContext uses version numbers for concurrency control:

```csharp
// Load component (version = 1)
var invoice = await handler.Get();

// Another user updates it (version becomes 2)
// This update will fail with ConcurrencyException
invoice.Amount = 1500.00m;
await dataContext.Commit(invoice); // Throws ConcurrencyException
```

### Automatic Version Increment

The system automatically increments version numbers:

```csharp
var invoice = new InvoiceComponent 
{ 
    InvoiceNumber = "INV-001",
    Amount = 1000.00m,
    Version = null // Will be set to 1 on first save
};

await dataContext.Commit(invoice);
Console.WriteLine(invoice.Version); // Output: 1

invoice.Amount = 1200.00m;
await dataContext.Commit(invoice);
Console.WriteLine(invoice.Version); // Output: 2
```

## Enhanced Snapshot System

Versioned components receive enhanced snapshot tracking with pre-change state capture:

```csharp
// Query version history
var snapshots = await dataContext.Snapshots()
    .ForEntity(entityId)
    .ForComponent<InvoiceComponent>()
    .Execute();

foreach (var snapshot in snapshots)
{
    Console.WriteLine($"Version {snapshot.Version}: {snapshot.Amount} on {snapshot.Timestamp}");
}
```

## Schema Management

The `ITableManager` automatically handles version column creation:

```csharp
// Ensures version column exists for versioned components
await tableManager.EnsureTableExists<InvoiceComponent>();
```

## Best Practices

1. **Use sparingly**: Only implement `IVersionedComponent` when needed, as it adds overhead
2. **Handle concurrency gracefully**: Always check for `ConcurrencyException` in user-facing operations
3. **Version on creation**: Set `Version = null` for new components (system will set to 1)
4. **Don't modify version manually**: Let the DataContext manage version increments

## Error Handling

```csharp
try 
{
    await dataContext.Commit(invoice);
}
catch (ConcurrencyException ex)
{
    // Handle version conflict
    return BadRequest("The record was modified by another user. Please refresh and try again.");
}
```

## Migration from Non-Versioned

To add versioning to existing components:

1. Add `IVersionedComponent` to component definition
2. Run `ITableManager.EnsureTableExists<T>()` to add version column
3. Existing records will have `version = 1` by default
