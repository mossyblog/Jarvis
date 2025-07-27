# Concurrency Control in Jarvis

## Overview

Jarvis implements a dual-strategy optimistic concurrency control system that supports both version-based and timestamp-based tracking. The framework automatically selects the appropriate strategy based on whether a component implements the `IVersionedComponent` interface.

## Implementation

### Component Interfaces

#### IComponent Interface
All components must implement the base `IComponent` interface:
```csharp
public interface IComponent
{
    Guid Id { get; init; }
    Guid OwnerEntityId { get; set; }
    DateTime LastUpdated { get; set; } // Tracks last modification time
}
```

#### IVersionedComponent Interface
Components requiring version-based concurrency control implement `IVersionedComponent`:
```csharp
public interface IVersionedComponent : IComponent
{
    int? Version { get; set; } // Increments on each update
}
```

### Concurrency Control Methods

#### TryCommit Method
The `IDataContext.TryCommit<TComponent>()` method provides concurrency-aware persistence:

```csharp
// Returns true if save succeeded, false if there was a concurrency conflict
Task<bool> TryCommit<TComponent>(TComponent component)
    where TComponent : class, IComponent, new();
```

#### Commit Method
The `IDataContext.Commit<TComponent>()` method performs persistence without concurrency checks:

```csharp
// Always attempts to save, regardless of concurrent modifications
Task Commit<TComponent>(TComponent component)
    where TComponent : class, IComponent, new();
```

### How It Works

#### Version-Based Concurrency (IVersionedComponent)
For components implementing `IVersionedComponent`:
1. **New Records**: Sets initial version to 1
2. **Existing Records**: 
   - Compares component version with database version
   - If versions match, increments version and saves
   - If versions differ, returns `false` (concurrency conflict)
   - Automatically creates snapshots before and after changes

#### Timestamp-Based Concurrency (IComponent)
For components only implementing `IComponent`:
1. **New Records**: Always succeeds with current timestamp
2. **Existing Records**:
   - Compares the `LastUpdated` timestamp with database value
   - If timestamps match (within 10ms tolerance), update proceeds
   - If timestamps differ, returns `false` (concurrency conflict)

### Usage Examples

#### Using TryCommit with Concurrency Control
```csharp
// Load a component
var handler = dataContext.For<MyHandler>(entityId);
var component = await handler.Get();

// Make changes
component.Value = 42;

// Try to save - will fail if someone else modified it
var success = await dataContext.TryCommit(component);
if (!success)
{
    // Handle concurrency conflict
    // Typically: reload, reapply changes, retry
    throw new ConcurrencyException(
        $"Update failed due to concurrent modification of {component.GetType().Name}");
}
```

#### Using Commit without Concurrency Control
```csharp
// For operations where you want to force an update
var component = await handler.Get();
component.Value = 42;

// Always saves, overwriting any concurrent changes
await dataContext.Commit(component);
```

#### Implementing a Versioned Component
```csharp
public record InventoryComponent : BaseComponent, IVersionedComponent
{
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public int? Version { get; set; } // Required for IVersionedComponent
}
```

### Handler Pattern

Handlers should use `TryCommit` for concurrency-sensitive operations and handle failures appropriately:

```csharp
public class InventoryHandler : ComponentHandler<InventoryComponent>
{
    public async Task DecrementQuantity(int amount)
    {
        var inventory = await Get();
        
        if (inventory.Quantity < amount)
            throw new ValidationException("Insufficient inventory");
        
        var updated = inventory with { Quantity = inventory.Quantity - amount };
        
        // Use TryCommit for concurrency-sensitive operations
        var success = await DataContext.TryCommit(updated);
        if (!success)
        {
            throw new ConcurrencyException(
                "Inventory was modified by another process. Please retry.");
        }
    }
    
    public async Task ForceUpdatePrice(decimal newPrice)
    {
        var inventory = await Get();
        var updated = inventory with { Price = newPrice };
        
        // Use Commit when you need to force an update
        await DataContext.Commit(updated);
    }
}
```

## Audit and Snapshot Integration

### Automatic Audit Logging
Both `TryCommit` and `Commit` automatically create audit events:
- **Component Creation**: Logs `COMPONENT_CREATED` event
- **Component Update**: Logs `COMPONENT_UPDATED` event with before/after states
- **Concurrency Conflicts**: Logs specific conflict events:
  - `COMPONENT_VERSION_CONFLICT` for version mismatches
  - `COMPONENT_CONCURRENCY_CONFLICT` for timestamp mismatches

### Automatic Snapshots
For components implementing `IVersionedComponent`:
- **On Create**: Captures initial state snapshot
- **Before Update**: Captures current state before changes
- **Version Tracking**: Each snapshot includes the version number

## Implementation Details

### Version-Based Strategy
- **Preferred for**: High-value data requiring strict consistency
- **Benefits**: Deterministic conflict detection, audit trail via snapshots
- **Trade-offs**: Requires schema changes, additional storage for snapshots

### Timestamp-Based Strategy
- **Used for**: Simple components not requiring versioning
- **Benefits**: No schema changes required, works out-of-the-box
- **Trade-offs**: 10ms tolerance window, potential for clock skew issues

### Postgrest/Supabase Considerations
- Uses `Upsert` operation for atomic insert/update
- Implements "read-check-update" pattern for concurrency control
- All concurrency checks happen at the SDK level, not database level

## Database Schema

### Basic Component Schema
All component tables must include these required columns:

```sql
CREATE TABLE my_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL UNIQUE,
    -- component fields...
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Versioned Component Schema
Components implementing `IVersionedComponent` require an additional column:

```sql
CREATE TABLE versioned_component (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL UNIQUE,
    version INT,  -- Required for version-based concurrency
    -- component fields...
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_updated TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Snapshot Storage Schema
Snapshots are automatically stored in the `component_snapshots` table:

```sql
CREATE TABLE component_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    component_id UUID NOT NULL,
    component_type TEXT NOT NULL,
    entity_id UUID NOT NULL,
    snapshots JSONB NOT NULL,  -- Array of snapshot entries
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

## Best Practices

1. **Choose the Right Strategy**:
   - Use `IVersionedComponent` for business-critical data
   - Use basic `IComponent` for simple, frequently updated data

2. **Handler Design**:
   - Always use `TryCommit` for user-initiated updates
   - Use `Commit` only for system operations or forced updates
   - Handle `ConcurrencyException` with appropriate retry logic

3. **Transaction Boundaries**:
   - Keep transactions short to minimize conflict windows
   - Avoid holding component state across async boundaries

4. **Testing**:
   - Test concurrency scenarios with multiple concurrent operations
   - Verify audit trail completeness for conflict scenarios
   - Ensure snapshot integrity for versioned components