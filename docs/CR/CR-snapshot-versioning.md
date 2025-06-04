# Change Request: Entity/Component Snapshot Versioning

## Overview

This change request proposes adding automatic snapshot versioning to the Jarvis ECS framework. When `Commit` or `TryCommit` operations succeed, the system will automatically capture a snapshot of the previous state before the new data is persisted. This provides a complete audit trail of all component mutations with full state history using a "fire and forget" pattern.

## Business Requirements

- **Full State History**: Capture complete component state at each point in time
- **Audit Compliance**: Meet regulatory requirements for data change tracking
- **Debugging Support**: Enable investigation of data issues by viewing historical states
- **Recovery Capability**: Allow rollback to previous component states if needed
- **Performance**: Minimal impact on write operations using fire-and-forget pattern
- **Storage Efficiency**: Use JSON blob storage in a centralized table

## Technical Design

### Core Concepts

1. **Automatic Snapshotting**: Snapshots created automatically during `Commit`/`TryCommit`
2. **Pre-Commit Capture**: Previous state captured before new data overwrites it
3. **Fire and Forget**: Snapshot operations are asynchronous and non-blocking
4. **Version Tracking**: Each component maintains a version number that increments on write
5. **Centralized Storage**: Single `component_snapshots` table stores all historical data
6. **JSON Serialization**: Component state stored as JSON blobs for flexibility

### Database Schema

```sql
CREATE TABLE component_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id UUID NOT NULL,
    component_type VARCHAR(255) NOT NULL,
    component_id UUID NOT NULL UNIQUE, -- One row per component
    snapshots JSONB NOT NULL DEFAULT '[]', -- Array of historical snapshots
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Indexes for performance
    INDEX idx_snapshots_entity (entity_id),
    INDEX idx_snapshots_component (component_id),
    INDEX idx_snapshots_type (component_type)
);

-- Add version column to all component tables
ALTER TABLE [component_table] ADD COLUMN version INT DEFAULT 1;
```

### Snapshot Array Structure

Each element in the `snapshots` JSONB array contains:
```json
{
    "version": 1,
    "data": { /* component state */ },
    "operation": "CREATE|UPDATE|DELETE",
    "timestamp": "2024-01-15T10:30:00Z",
    "created_by": "user@example.com"
}
```

### API Design

#### IDataContext Extensions

```csharp
public interface IDataContext
{
    // Existing methods...
    
    // Query historical snapshots (no save methods - snapshots happen automatically)
    ISnapshotQuery Snapshots();
}

public interface ISnapshotQuery
{
    ISnapshotQuery ForEntity(Guid entityId);
    ISnapshotQuery ForComponent<T>(Guid componentId) where T : class, IComponent;
    ISnapshotQuery ForComponentType<T>() where T : class, IComponent;
    ISnapshotQuery Between(DateTime start, DateTime end);
    ISnapshotQuery AtVersion(int version);
    
    Task<IEnumerable<ComponentSnapshot>> ToListAsync();
    Task<ComponentSnapshot> FirstOrDefaultAsync();
    Task<T> RestoreAsync<T>() where T : class, IComponent;
}

public class ComponentSnapshotRecord
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string ComponentType { get; set; }
    public Guid ComponentId { get; set; }
    public List<Snapshot> Snapshots { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Snapshot
{
    public int Version { get; set; }
    public JsonDocument Data { get; set; }
    public string Operation { get; set; }
    public DateTime Timestamp { get; set; }
    public string CreatedBy { get; set; }
    
    public T Deserialize<T>() where T : class, IComponent
    {
        return JsonSerializer.Deserialize<T>(Data.RootElement.GetRawText());
    }
}
```

#### Implementation in DataContext

```csharp
public async Task Commit<TComponent>(TComponent component) 
    where TComponent : Supabase.Postgrest.Models.BaseModel, IComponent, new()
{
    try
    {
        // Fire and forget snapshot capture
        _ = CaptureSnapshotAsync(component);
        
        // Proceed with the actual commit
        await _client.Postgrest.Table<TComponent>().Upsert(component);
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to save component {ComponentType} for entity {OwnerEntityId}", 
            typeof(TComponent).Name, component.OwnerEntityId);            
        throw;
    }
}

public async Task<bool> TryCommit<TComponent>(TComponent component) 
    where TComponent : Supabase.Postgrest.Models.BaseModel, IComponent, new()
{
    try
    {
        // Existing concurrency logic...
        var existing = await GetExistingComponent<TComponent>(component.Id);
        
        if (existing != null)
        {
            // Fire and forget snapshot of the existing state
            _ = CaptureSnapshotAsync(existing, "UPDATE");
        }
        
        // Continue with normal TryCommit logic...
        await _client.Postgrest.Table<TComponent>().Upsert(component);
        
        if (existing == null)
        {
            // Fire and forget snapshot of the new state
            _ = CaptureSnapshotAsync(component, "CREATE");
        }
        
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to save component {ComponentType}", typeof(TComponent).Name);            
        throw;
    }
}

private async Task CaptureSnapshotAsync<TComponent>(TComponent component, string operation = "UPDATE")
    where TComponent : IComponent
{
    try
    {
        // Create new snapshot entry
        var snapshotEntry = new 
        {
            version = component.Version ?? 1,
            data = component,
            operation = operation,
            timestamp = DateTime.UtcNow,
            created_by = _currentUser ?? "system"
        };
        
        // Try to get existing snapshot record
        var existing = await _client.Postgrest
            .Table<ComponentSnapshotRecord>()
            .Filter("component_id", Operator.Equals, component.Id.ToString())
            .Single();
        
        if (existing != null)
        {
            // Append to existing snapshots array using PostgreSQL jsonb operators
            var updateQuery = $@"
                UPDATE component_snapshots 
                SET snapshots = snapshots || '{JsonSerializer.Serialize(snapshotEntry)}'::jsonb,
                    updated_at = NOW()
                WHERE component_id = '{component.Id}'";
            
            await _client.Rpc("exec_raw_sql", new { query = updateQuery });
        }
        else
        {
            // Create new record with first snapshot
            var record = new ComponentSnapshotRecord
            {
                EntityId = component.OwnerEntityId,
                ComponentType = typeof(TComponent).Name,
                ComponentId = component.Id,
                Snapshots = new List<Snapshot> { snapshotEntry },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _client.Postgrest.Table<ComponentSnapshotRecord>().Insert(record);
        }
    }
    catch (Exception ex)
    {
        // Log but don't fail the operation - fire and forget
        _logger.LogWarning(ex, "Failed to capture snapshot for {ComponentType} {ComponentId}", 
            typeof(TComponent).Name, component.Id);
    }
}
```

### Handler Integration

Component handlers automatically benefit from snapshotting:

```csharp
public class InvoiceHandler : ComponentHandler<Invoice>
{
    public async Task UpdateAmount(decimal newAmount, string reason)
    {
        var invoice = await Get();
        invoice.Amount = newAmount;
        
        // Snapshot automatically captured on Commit
        await _dataContext.Commit(invoice);
    }
    
    public async Task<bool> TryUpdateStatus(string newStatus)
    {
        var invoice = await Get();
        invoice.Status = newStatus;
        
        // Snapshot automatically captured if TryCommit succeeds
        return await _dataContext.TryCommit(invoice);
    }
}
```

## Usage Examples

### Basic Snapshotting

```csharp
// All commits automatically create snapshots
var invoice = new Invoice { OwnerEntityId = entityId, Amount = 100.00m };
await _dataContext.Commit(invoice);  // Snapshot captured automatically

// Update creates another snapshot
invoice.Amount = 150.00m;
await _dataContext.Commit(invoice);  // Previous state snapshotted before update

// Concurrency-safe updates also create snapshots
invoice.Status = "PAID";
var success = await _dataContext.TryCommit(invoice);  // Snapshot if successful
```

### Querying History

```csharp
// Get snapshot record for a component (contains all versions)
var snapshotRecord = await _dataContext.Snapshots()
    .ForComponent<Invoice>(invoiceId)
    .FirstOrDefaultAsync();

// Access all historical versions
foreach (var snapshot in snapshotRecord.Snapshots)
{
    _logger.LogInfo($"Version {snapshot.Version}: {snapshot.Operation} at {snapshot.Timestamp}");
}

// Get specific version from the array
var v2 = snapshotRecord.Snapshots.FirstOrDefault(s => s.Version == 2);

// Get recent changes
var recentSnapshots = snapshotRecord.Snapshots
    .Where(s => s.Timestamp > DateTime.UtcNow.AddDays(-7))
    .ToList();
```

### Restoring Previous State

```csharp
// Get the snapshot record
var snapshotRecord = await _dataContext.Snapshots()
    .ForComponent<Invoice>(invoiceId)
    .FirstOrDefaultAsync();

// Method 1: Restore by version number
var v3Snapshot = snapshotRecord.Snapshots.FirstOrDefault(s => s.Version == 3);
var restoredInvoice = v3Snapshot.Deserialize<Invoice>();

// Method 2: Restore by timestamp
var yesterdaySnapshot = snapshotRecord.Snapshots
    .Where(s => s.Timestamp.Date == DateTime.UtcNow.AddDays(-1).Date)
    .LastOrDefault();
var yesterdayInvoice = yesterdaySnapshot?.Deserialize<Invoice>();

// Method 3: Restore previous version (undo last change)
var previousSnapshot = snapshotRecord.Snapshots[^2]; // Second to last
var previousInvoice = previousSnapshot.Deserialize<Invoice>();

// Save restored version as current (creates new version and adds to snapshot array)
await _dataContext.Commit(restoredInvoice);
```

## Implementation Plan

### Phase 1: Core Infrastructure
1. Create `component_snapshots` table with JSONB array
2. Add version column to component tables via migration
3. Implement `ComponentSnapshotRecord` and `Snapshot` classes
4. Add `CaptureSnapshotAsync` private method to DataContext
5. Create PostgreSQL function for atomic array append (if needed)

### Phase 2: Automatic Integration
1. Modify `Commit` to include fire-and-forget snapshotting
2. Modify `TryCommit` to include fire-and-forget snapshotting
3. Ensure version tracking is maintained in components
4. Test fire-and-forget behavior doesn't impact performance

### Phase 3: Query and Recovery
1. Implement full `ISnapshotQuery` functionality
2. Add restore capabilities
3. Create helper methods for common scenarios
4. Add performance optimizations (batching, caching)

### Phase 4: Testing and Migration
1. Comprehensive unit tests for snapshot functionality
2. Integration tests with real Supabase
3. Performance testing with large datasets
4. Migration guide for existing systems

## Benefits

1. **Complete Audit Trail**: Every change is captured automatically
2. **Zero Code Changes**: Works transparently with existing `Commit`/`TryCommit`
3. **Storage Efficiency**: One row per component regardless of version count
4. **Debugging Power**: Can trace exact sequence of changes in chronological order
5. **Compliance**: Meets regulatory requirements for data history
6. **Recovery Options**: Can restore to any previous state
7. **Performance**: Fire-and-forget pattern minimizes impact
8. **Query Efficiency**: Single row fetch gets entire component history

## Considerations

1. **Array Size Limits**: PostgreSQL JSONB arrays have practical size limits (~1GB)
2. **Retention Policy**: May need to truncate old snapshots from arrays periodically
3. **Privacy**: May need to purge PII from historical data in arrays
4. **Performance**: Array append operations are atomic but get slower with size
5. **Migration**: Existing data won't have historical snapshots
6. **Query Performance**: Large arrays may impact query performance over time

## Alternative Approaches Considered

1. **Shadow Tables**: Rejected - creates schema maintenance burden
2. **Event Sourcing**: Rejected - too complex for current needs
3. **Temporal Tables**: Rejected - database-specific, less flexible
4. **Audit Logs Only**: Rejected - doesn't capture full state

## Security Considerations

1. **Access Control**: Snapshot queries respect same permissions as components
2. **Sensitive Data**: Option to exclude fields from snapshots
3. **Retention**: Automated cleanup of old snapshots per policy
4. **Encryption**: Snapshot data can be encrypted at rest

## Test Scenarios

Following the test guidelines in `/core.jarvis.tests/Guidelines.md`, here are the key test scenarios:

### 1. Automatic Snapshot Creation Tests

```csharp
public class SnapshotCreationTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify that Commit automatically creates snapshots
    /// PURPOSE: Ensure snapshot functionality works transparently
    /// BUSINESS CONTEXT: Audit trail requirement for all data changes
    /// WHY IMPORTANT: Core feature must work without code changes
    /// ARCHITECTURAL SIGNIFICANCE: Validates fire-and-forget pattern
    /// FUTURE RESILIENCE: Protects against breaking automatic snapshotting
    /// </summary>
    [Fact]
    public async Task Commit_Should_Create_Initial_Snapshot()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Test", 
            Value = 100 
        };
        
        // Act
        await TestDataContext().Commit(component);
        
        // Assert
        var snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefaultAsync();
            
        snapshots.ShouldNotBeNull();
        snapshots.Snapshots.Count.ShouldBe(1);
        snapshots.Snapshots[0].Operation.ShouldBe("CREATE");
        snapshots.Snapshots[0].Version.ShouldBe(1);
    }
    
    /// <summary>
    /// INTENT: Verify that updates capture previous state
    /// PURPOSE: Ensure we can track all historical changes
    /// BUSINESS CONTEXT: Compliance requires full change history
    /// WHY IMPORTANT: Must capture state before changes
    /// ARCHITECTURAL SIGNIFICANCE: Validates pre-commit snapshot capture
    /// FUTURE RESILIENCE: Ensures historical data integrity
    /// </summary>
    [Fact]
    public async Task Commit_Should_Capture_Previous_State_On_Update()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Original", 
            Value = 100 
        };
        await TestDataContext().Commit(component);
        
        // Act
        component.Name = "Updated";
        component.Value = 200;
        await TestDataContext().Commit(component);
        
        // Assert
        var snapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefaultAsync();
            
        snapshots.Snapshots.Count.ShouldBe(2);
        
        var firstSnapshot = snapshots.Snapshots[0].Deserialize<TestComponent>();
        firstSnapshot.Name.ShouldBe("Original");
        firstSnapshot.Value.ShouldBe(100);
        
        var secondSnapshot = snapshots.Snapshots[1].Deserialize<TestComponent>();
        secondSnapshot.Name.ShouldBe("Updated");
        secondSnapshot.Value.ShouldBe(200);
    }
}
```

### 2. Restore Operation Tests

```csharp
public class SnapshotRestoreTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify components can be restored to previous versions
    /// PURPOSE: Enable rollback functionality
    /// BUSINESS CONTEXT: Error recovery and audit investigations
    /// WHY IMPORTANT: Must be able to recover from mistakes
    /// ARCHITECTURAL SIGNIFICANCE: Validates restore pattern
    /// FUTURE RESILIENCE: Ensures recovery capability remains intact
    /// </summary>
    [Fact]
    public async Task Should_Restore_Component_To_Previous_Version()
    {
        // Arrange - Create component with multiple versions
        var entityId = Guid.NewGuid();
        var component = new TestComponent 
        { 
            OwnerEntityId = entityId, 
            Name = "Version1", 
            Value = 1 
        };
        await TestDataContext().Commit(component);
        
        component.Name = "Version2";
        component.Value = 2;
        await TestDataContext().Commit(component);
        
        component.Name = "Version3";
        component.Value = 3;
        await TestDataContext().Commit(component);
        
        // Act - Restore to version 2
        var snapshotRecord = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefaultAsync();
            
        var v2Snapshot = snapshotRecord.Snapshots
            .FirstOrDefault(s => s.Version == 2);
        var restoredComponent = v2Snapshot.Deserialize<TestComponent>();
        
        await TestDataContext().Commit(restoredComponent);
        
        // Assert
        var current = await TestDataContext()
            .For<TestHandler>(entityId)
            .Get();
            
        current.Name.ShouldBe("Version2");
        current.Value.ShouldBe(2);
        
        // Verify new snapshot was created
        var finalSnapshots = await TestDataContext().Snapshots()
            .ForComponent<TestComponent>(component.Id)
            .FirstOrDefaultAsync();
            
        finalSnapshots.Snapshots.Count.ShouldBe(4);
    }
}
```

### 3. Performance Tests

```csharp
public class SnapshotPerformanceTests : IntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify snapshots don't impact commit performance
    /// PURPOSE: Ensure fire-and-forget pattern works
    /// BUSINESS CONTEXT: Performance SLAs must be maintained
    /// WHY IMPORTANT: Snapshotting cannot slow down operations
    /// ARCHITECTURAL SIGNIFICANCE: Validates async pattern
    /// FUTURE RESILIENCE: Protects against performance regression
    /// </summary>
    [Fact]
    public async Task Snapshot_Should_Not_Block_Commit_Operations()
    {
        // Arrange
        var components = Enumerable.Range(0, 100)
            .Select(_ => new TestComponent 
            { 
                OwnerEntityId = Guid.NewGuid(), 
                Name = "Perf Test", 
                Value = Random.Shared.Next(1000) 
            })
            .ToList();
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        foreach (var component in components)
        {
            await TestDataContext().Commit(component);
        }
        stopwatch.Stop();
        
        // Assert
        var avgTimePerCommit = stopwatch.ElapsedMilliseconds / components.Count;
        avgTimePerCommit.ShouldBeLessThan(50); // 50ms per commit max
        
        // Verify snapshots were created (async)
        await Task.Delay(1000); // Allow async operations to complete
        
        foreach (var component in components.Take(5)) // Spot check
        {
            var snapshots = await TestDataContext().Snapshots()
                .ForComponent<TestComponent>(component.Id)
                .FirstOrDefaultAsync();
                
            snapshots.ShouldNotBeNull();
        }
    }
}
```

## Conclusion

This snapshot versioning system provides a robust, transparent way to track all component changes with zero impact on existing code. By automatically capturing snapshots during `Commit` and `TryCommit` operations using a fire-and-forget pattern, we achieve comprehensive versioning without any changes to handler logic or additional API complexity. The single-row-per-component design with JSONB arrays provides extremely efficient storage and query performance, allowing the entire history of a component to be retrieved in a single database operation while maintaining the simplicity of the Jarvis ECS architecture.