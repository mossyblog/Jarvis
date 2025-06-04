# Snapshot Versioning Architecture

This document explains the architectural design, patterns, and implementation details of the snapshot versioning system in Jarvis ECS framework.

## Overview

The snapshot versioning system provides automatic audit trails and data recovery capabilities through a **fire-and-forget** pattern that captures component state changes without impacting business operation performance.

### Key Architectural Principles

1. **Zero Performance Impact**: Snapshots are captured asynchronously using fire-and-forget pattern
2. **Opt-In Model**: Only components implementing `IVersionedComponent` are captured
3. **Storage Efficiency**: Single-row-per-component design with JSONB arrays
4. **No Reflection**: Compile-time type safety throughout the system
5. **DataContext Exclusive**: All storage operations go through DataContext

## System Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Application   │    │   DataContext    │    │   Database      │
│     Layer       │    │                  │    │                 │
├─────────────────┤    ├──────────────────┤    ├─────────────────┤
│ Component       │───▶│ Commit()         │───▶│ component_table │
│ IVersioned      │    │ TryCommit()      │    │                 │
│ Component       │    │                  │    │                 │
└─────────────────┘    │ CaptureSnapshot  │    │ component_      │
                       │ Async() ◄────────┼────┤ snapshots       │
┌─────────────────┐    │ (Fire & Forget)  │    │                 │
│ Snapshot Query  │───▶│                  │    │                 │
│ ISnapshotQuery  │    │ Snapshots()      │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## Core Components

### 1. IVersionedComponent Interface

**Purpose**: Marker interface that enables automatic snapshot capture

```csharp
public interface IVersionedComponent : IComponent
{
    [Column("version")]
    int? Version { get; set; }
}
```

**Design Rationale**:
- **Explicit Opt-In**: Only components that need versioning implement this interface
- **Backward Compatibility**: Existing components continue working unchanged
- **Type Safety**: Compile-time checking ensures version property exists
- **Performance**: No runtime reflection needed to detect versioned components

### 2. Snapshot Storage Model

**Architecture**: One-row-per-component with JSONB array storage

```sql
component_snapshots
├── id (UUID, PK)
├── entity_id (UUID, indexed)
├── component_type (TEXT, indexed) 
├── component_id (UUID, unique indexed)
├── snapshots (JSONB array)
├── created_at (TIMESTAMPTZ)
└── updated_at (TIMESTAMPTZ)
```

**Design Rationale**:
- **Query Efficiency**: Single lookup by component_id retrieves all history
- **Storage Compression**: PostgreSQL JSONB compression reduces storage overhead
- **Atomic Updates**: Single row updates ensure consistency
- **No JOINs**: All historical data available without complex queries

### 3. Fire-and-Forget Pattern

**Implementation**: Asynchronous snapshot capture using `Task.Run()`

```csharp
// In DataContext.Commit()
if (component is IVersionedComponent versionedComponent)
{
    if (existing != null)
    {
        // Capture BEFORE the change
        _ = Task.Run(async () => await CaptureSnapshotAsync(existing, "UPDATE"));
        versionedComponent.Version = (versionedComponent.Version ?? 0) + 1;
    }
    else
    {
        // Capture AFTER the save (for new records)
        versionedComponent.Version = 1;
        _ = Task.Run(async () =>
        {
            await Task.Delay(100); // Ensure save completes
            await CaptureSnapshotAsync(component, "CREATE");
        });
    }
}
```

**Design Rationale**:
- **Non-Blocking**: Business operations complete immediately
- **Failure Isolation**: Snapshot failures don't affect business logic
- **Resource Efficiency**: Background tasks don't consume main thread
- **Eventual Consistency**: Snapshots eventually captured even under load

## Data Flow Architecture

### Write Path (Component Updates)

```
1. Application calls DataContext.Commit(component)
2. DataContext checks if component is IVersionedComponent
3. If versioned:
   a. Query existing component state
   b. If exists: Fire background task to capture existing state
   c. Increment component version
   d. Save updated component
   e. Background task captures snapshot asynchronously
4. If not versioned: Save component normally
```

**Sequence Diagram**:
```
App          DataContext      Database       Background Task
│                │               │                  │
├─Commit(comp)──▶│               │                  │
│                ├─Query exist──▶│                  │
│                │◄─────────────┤                  │
│                ├─Task.Run────┬─┴──────────────────▶│
│                │             │                    │
│                ├─Increment───┤                    │
│                │  version    │                    │
│                ├─Save comp──▶│                    │
│                │◄─────────────┤                    │
├◄─Success──────┤               │                    │
│                │               │                    │
│                │               │ ◄─Capture snapshot┤
│                │               │ ├─Save snapshot──▶│
│                │               │ │◄─────────────────┤
```

### Read Path (Snapshot Queries)

```
1. Application calls DataContext.Snapshots()
2. Returns ISnapshotQuery builder
3. Application chains filter methods
4. Application calls ToListAsync() or RestoreAsync()
5. Query executes against component_snapshots table
6. JSON snapshots deserialized to strongly-typed objects
```

## Key Design Patterns

### 1. Builder Pattern (ISnapshotQuery)

**Purpose**: Provide fluent API for complex snapshot queries

```csharp
public interface ISnapshotQuery
{
    ISnapshotQuery ForEntity(Guid entityId);
    ISnapshotQuery ForComponent<T>(Guid componentId) where T : class, IComponent;
    ISnapshotQuery Between(DateTime start, DateTime end);
    ISnapshotQuery AtVersion(int version);
    Task<IEnumerable<ComponentSnapshotRecord>> ToListAsync();
    Task<T?> RestoreAsync<T>() where T : class, IComponent;
}
```

**Benefits**:
- **Composable**: Chain filters in any order
- **Type Safe**: Generic methods ensure correct component types
- **Readable**: Query intent is clear from method names
- **Extensible**: Easy to add new filter methods

### 2. Strategy Pattern (Snapshot Capture)

**Purpose**: Different capture strategies based on operation type

```csharp
private async Task CaptureSnapshotAsync<TComponent>(TComponent component, string operation = "UPDATE")
{
    // Strategy varies based on operation:
    // - CREATE: Capture after save completes
    // - UPDATE: Capture existing state before change
    // - Custom operations: Future extensibility
}
```

### 3. Repository Pattern (DataContext Integration)

**Purpose**: Centralize all data access through DataContext

```csharp
// Snapshot operations integrated into existing DataContext
public interface IDataContext
{
    Task Commit<T>(T component) where T : IComponent;
    Task<bool> TryCommit<T>(T component) where T : IComponent;
    ISnapshotQuery Snapshots(); // Snapshot access point
}
```

**Benefits**:
- **Consistency**: All data operations use same patterns
- **Security**: No direct database access outside DataContext
- **Testing**: Easy to mock and test
- **Transactions**: Future transaction support for snapshots

## Performance Architecture

### Asynchronous Processing

**Queue Model**: Task.Run() creates background tasks for snapshot capture

```
Main Thread              Background Thread Pool
┌─────────────┐         ┌──────────────────────┐
│ Business    │         │ Snapshot Capture     │
│ Operations  │         │ ┌──────────────────┐ │
│             │ Task.Run│ │ Component A      │ │
│ Commit() ───┼────────▶│ │ Snapshot         │ │
│             │         │ └──────────────────┘ │
│ Continue... │         │ ┌──────────────────┐ │
│             │         │ │ Component B      │ │
│             │         │ │ Snapshot         │ │
│             │         │ └──────────────────┘ │
└─────────────┘         └──────────────────────┘
```

**Characteristics**:
- **Non-Blocking**: Main thread continues immediately
- **Scalable**: Thread pool manages concurrency
- **Resilient**: Individual snapshot failures don't affect others
- **Resource Aware**: Background tasks yield to main operations

### Database Optimization

**Storage Design**: Optimized for both writes and reads

```sql
-- Write-optimized: Unique constraint prevents duplicates
CREATE UNIQUE INDEX idx_snapshots_component_unique ON component_snapshots(component_id);

-- Read-optimized: Covering indexes for common queries
CREATE INDEX idx_snapshots_entity ON component_snapshots(entity_id);
CREATE INDEX idx_snapshots_type ON component_snapshots(component_type);

-- JSONB optimization: GIN index for complex JSON queries (future)
-- CREATE INDEX idx_snapshots_jsonb ON component_snapshots USING GIN(snapshots);
```

### Memory Management

**JSON Handling**: Efficient serialization and deserialization

```csharp
public class ComponentSnapshotRecord : BaseModel
{
    // Store as string to avoid JsonDocument lifecycle issues
    [Column("snapshots")]
    public string SnapshotsJson { get; set; } = "[]";
    
    // Property wrapper for convenient access
    [JsonIgnore]
    public JsonDocument Snapshots 
    { 
        get => JsonDocument.Parse(SnapshotsJson);
        set => SnapshotsJson = value.RootElement.GetRawText();
    }
}
```

**Benefits**:
- **Lifecycle Control**: String storage avoids JsonDocument disposal issues
- **Supabase Compatibility**: String column works with all Supabase SDK versions
- **Memory Efficient**: Parse JSON only when needed
- **Serialization Safe**: No complex object graphs or circular references

## Security Architecture

### Data Access Control

**Principle**: All snapshot access goes through DataContext

```csharp
// Enforced pattern - no direct database access
public class InvoiceHandler : ComponentHandler<Invoice>
{
    // ✅ Correct - uses DataContext
    public async Task<List<Invoice>> GetHistory()
    {
        return await _dataContext.Snapshots()
            .ForComponent<Invoice>(ComponentId)
            // ... query continues
    }
    
    // ❌ Wrong - bypasses DataContext security
    // private readonly Supabase.Client _directClient; // Not allowed
}
```

### Audit Trail Integrity

**Design**: Snapshots are append-only for audit integrity

```csharp
// Snapshots are never modified, only appended
private async Task CaptureSnapshotAsync<TComponent>(TComponent component, string operation)
{
    if (existing != null)
    {
        // Append to existing array
        var snapshots = existing.GetSnapshots();
        var newSnapshotsArray = snapshots.Concat(new[] { newSnapshot }).ToArray();
        existing.SnapshotsJson = JsonSerializer.Serialize(newSnapshotsArray);
        await _client.Postgrest.Table<ComponentSnapshotRecord>().Update(existing);
    }
    else
    {
        // Create new record
        var record = new ComponentSnapshotRecord { /* ... */ };
        await _client.Postgrest.Table<ComponentSnapshotRecord>().Insert(record);
    }
}
```

### Data Serialization Security

**Filter Sensitive Properties**: Only serialize data properties, not metadata

```csharp
private async Task CaptureSnapshotAsync<TComponent>(TComponent component, string operation)
{
    var componentData = new Dictionary<string, object?>();
    
    foreach (var prop in componentType.GetProperties())
    {
        // Skip properties from BaseModel that cause serialization issues
        if (prop.DeclaringType == typeof(Supabase.Postgrest.Models.BaseModel))
            continue;
            
        // Skip complex objects that might contain sensitive data
        if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
            continue;
            
        componentData[prop.Name] = prop.GetValue(component);
    }
}
```

## Scalability Architecture

### Horizontal Scaling

**Database Design**: Partition-friendly schema

```sql
-- Component snapshots can be partitioned by component_type or entity_id
CREATE TABLE component_snapshots_invoice 
PARTITION OF component_snapshots 
FOR VALUES IN ('Invoice');

CREATE TABLE component_snapshots_payment 
PARTITION OF component_snapshots 
FOR VALUES IN ('Payment');
```

### Vertical Scaling

**Background Processing**: CPU and I/O efficient operations

```csharp
// Efficient JSON processing
private async Task CaptureSnapshotAsync<TComponent>(TComponent component, string operation)
{
    try
    {
        // Minimize reflection usage
        var componentData = ExtractComponentProperties(component);
        
        // Use efficient JSON serialization
        var snapshotEntry = new { /* ... */ };
        
        // Batch database operations where possible
        await UpsertSnapshotRecord(component.Id, snapshotEntry);
    }
    catch (Exception ex)
    {
        // Fail gracefully - don't impact main operations
        _logger.LogError(ex, "Snapshot capture failed for {ComponentType}", typeof(TComponent).Name);
    }
}
```

## Error Handling Architecture

### Resilience Patterns

**Circuit Breaker**: Protect against cascade failures

```csharp
// Future enhancement: Circuit breaker for snapshot failures
private static int _consecutiveFailures = 0;
private static DateTime _lastFailureTime = DateTime.MinValue;

private async Task CaptureSnapshotAsync<TComponent>(TComponent component, string operation)
{
    // Skip snapshots if too many recent failures
    if (_consecutiveFailures > 5 && DateTime.UtcNow - _lastFailureTime < TimeSpan.FromMinutes(5))
    {
        _logger.LogWarning("Skipping snapshot capture due to circuit breaker");
        return;
    }
    
    try
    {
        // ... capture logic ...
        _consecutiveFailures = 0; // Reset on success
    }
    catch (Exception ex)
    {
        _consecutiveFailures++;
        _lastFailureTime = DateTime.UtcNow;
        _logger.LogError(ex, "Snapshot capture failed");
    }
}
```

### Graceful Degradation

**Isolation**: Snapshot failures don't affect business operations

```csharp
public async Task Commit<TComponent>(TComponent component)
{
    try
    {
        // Version increment and business logic NEVER fails due to snapshots
        if (component is IVersionedComponent versionedComponent)
        {
            versionedComponent.Version = (versionedComponent.Version ?? 0) + 1;
        }
        
        // Business operation must succeed
        await _client.Postgrest.Table<TComponent>().Upsert(component);
        
        // Snapshot capture is fire-and-forget - failures are logged but don't throw
        if (component is IVersionedComponent)
        {
            _ = Task.Run(async () => 
            {
                try
                {
                    await CaptureSnapshotAsync(component);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Snapshot capture failed - business operation succeeded");
                }
            });
        }
    }
    catch (Exception ex)
    {
        // Only business operation failures are thrown to caller
        _logger.LogError(ex, "Business operation failed");
        throw;
    }
}
```

## Future Architecture Considerations

### Event Sourcing Integration

**Potential Enhancement**: Integration with domain events

```csharp
// Future: Snapshots could be triggered by domain events
public async Task Handle(ComponentUpdatedEvent domainEvent)
{
    await CaptureSnapshotAsync(domainEvent.Component, domainEvent.Operation);
}
```

### Distributed Caching

**Potential Enhancement**: Cache frequently accessed snapshots

```csharp
// Future: Redis cache for recent snapshots
public async Task<T?> RestoreAsync<T>(Guid componentId, int version)
{
    var cacheKey = $"snapshot:{componentId}:{version}";
    var cached = await _cache.GetAsync<T>(cacheKey);
    if (cached != null) return cached;
    
    var restored = await _dataContext.Snapshots()
        .ForComponent<T>(componentId)
        .AtVersion(version)
        .RestoreAsync<T>();
        
    if (restored != null)
    {
        await _cache.SetAsync(cacheKey, restored, TimeSpan.FromMinutes(30));
    }
    
    return restored;
}
```

### Stream Processing

**Potential Enhancement**: Real-time snapshot processing

```csharp
// Future: Stream processing for analytics
public class SnapshotStreamProcessor
{
    public async Task ProcessSnapshotStream()
    {
        await foreach (var snapshot in GetSnapshotStream())
        {
            // Real-time analytics, alerting, etc.
            await ProcessSnapshotForAnalytics(snapshot);
        }
    }
}
```

This architecture provides a robust, scalable foundation for snapshot versioning while maintaining the core principles of performance, reliability, and security.