# Snapshot API Reference

This document provides detailed API reference for the snapshot versioning system in Jarvis ECS framework.

## Core Interfaces

### IVersionedComponent

Components that implement this interface automatically have snapshots captured.

```csharp
namespace core.jarvis.Data;

/// <summary>
/// Interface for components that support automatic snapshot versioning
/// </summary>
public interface IVersionedComponent : IComponent
{
    /// <summary>
    /// The version number of this component instance.
    /// Automatically incremented on each save operation.
    /// </summary>
    [Column("version")]
    int? Version { get; set; }
}
```

**Usage:**
```csharp
[Table("invoice")]
public class Invoice : BaseModel, IVersionedComponent
{
    // ... other properties ...
    
    [Column("version")]
    public int? Version { get; set; }
}
```

### ISnapshotQuery

Provides fluent API for querying historical snapshots.

```csharp
namespace core.jarvis.Data;

/// <summary>
/// Fluent interface for querying component snapshots
/// </summary>
public interface ISnapshotQuery
{
    /// <summary>
    /// Filter snapshots by entity ID
    /// </summary>
    /// <param name="entityId">The entity ID to filter by</param>
    /// <returns>Query builder for method chaining</returns>
    ISnapshotQuery ForEntity(Guid entityId);
    
    /// <summary>
    /// Filter snapshots for a specific component instance
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <param name="componentId">The component ID to filter by</param>
    /// <returns>Query builder for method chaining</returns>
    ISnapshotQuery ForComponent<T>(Guid componentId) where T : class, IComponent;
    
    /// <summary>
    /// Filter snapshots by component type
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <returns>Query builder for method chaining</returns>
    ISnapshotQuery ForComponentType<T>() where T : class, IComponent;
    
    /// <summary>
    /// Filter snapshots by date range
    /// </summary>
    /// <param name="start">Start date (inclusive)</param>
    /// <param name="end">End date (inclusive)</param>
    /// <returns>Query builder for method chaining</returns>
    ISnapshotQuery Between(DateTime start, DateTime end);
    
    /// <summary>
    /// Filter snapshots by specific version number
    /// </summary>
    /// <param name="version">The version number to filter by</param>
    /// <returns>Query builder for method chaining</returns>
    ISnapshotQuery AtVersion(int version);
    
    /// <summary>
    /// Execute query and return all matching snapshot records
    /// </summary>
    /// <returns>List of snapshot records</returns>
    Task<IEnumerable<ComponentSnapshotRecord>> ToListAsync();
    
    /// <summary>
    /// Execute query and return first matching snapshot record
    /// </summary>
    /// <returns>First snapshot record or null if none found</returns>
    Task<ComponentSnapshotRecord?> FirstOrDefaultAsync();
    
    /// <summary>
    /// Restore component to a specific state
    /// </summary>
    /// <typeparam name="T">The component type to restore</typeparam>
    /// <returns>Restored component instance or null if not found</returns>
    Task<T?> RestoreAsync<T>() where T : class, IComponent;
}
```

## Data Models

### ComponentSnapshotRecord

Represents a collection of snapshots for a single component.

```csharp
namespace core.jarvis.Data;

/// <summary>
/// Database record containing all snapshots for a component
/// </summary>
[Table("component_snapshots")]
public class ComponentSnapshotRecord : BaseModel
{
    /// <summary>
    /// Unique identifier for this snapshot record
    /// </summary>
    [PrimaryKey("id")]
    public Guid Id { get; set; }
    
    /// <summary>
    /// The entity that owns the component
    /// </summary>
    [Column("entity_id")]
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// The .NET type name of the component
    /// </summary>
    [Column("component_type")]
    public string ComponentType { get; set; } = string.Empty;
    
    /// <summary>
    /// The unique identifier of the component instance
    /// </summary>
    [Column("component_id")]
    public Guid ComponentId { get; set; }
    
    /// <summary>
    /// JSON array of snapshots stored as string for database compatibility
    /// </summary>
    [Column("snapshots")]
    [JsonProperty("snapshots")]
    public string SnapshotsJson { get; set; } = "[]";
    
    /// <summary>
    /// Convenience property to work with snapshots as JsonDocument
    /// </summary>
    [JsonIgnore]
    public JsonDocument Snapshots 
    { 
        get => JsonDocument.Parse(SnapshotsJson);
        set => SnapshotsJson = value.RootElement.GetRawText();
    }
    
    /// <summary>
    /// When this snapshot record was first created
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When this snapshot record was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Parse the JSON snapshots into strongly-typed objects
    /// </summary>
    /// <returns>List of snapshot entries</returns>
    public List<Snapshot> GetSnapshots();
}
```

### Snapshot

Represents a single point-in-time snapshot of a component.

```csharp
namespace core.jarvis.Data;

/// <summary>
/// A single snapshot entry containing component state at a point in time
/// </summary>
public class Snapshot
{
    /// <summary>
    /// The version number of the component when this snapshot was taken
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// The component data as JSON document
    /// </summary>
    public JsonDocument Data { get; set; } = JsonDocument.Parse("{}");
    
    /// <summary>
    /// The operation that triggered this snapshot (CREATE, UPDATE, etc.)
    /// </summary>
    public string Operation { get; set; } = "UPDATE";
    
    /// <summary>
    /// When this snapshot was captured
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Who or what created this snapshot
    /// </summary>
    public string CreatedBy { get; set; } = "system";
    
    /// <summary>
    /// Deserialize the snapshot data to a strongly-typed component
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <returns>Deserialized component instance</returns>
    /// <exception cref="InvalidOperationException">If deserialization fails</exception>
    public T Deserialize<T>() where T : class, IComponent;
}
```

## DataContext Extensions

### Snapshots() Method

Access point for snapshot queries from `IDataContext`.

```csharp
namespace core.jarvis.Data;

public interface IDataContext
{
    // ... other methods ...
    
    /// <summary>
    /// Create a new snapshot query
    /// </summary>
    /// <returns>Snapshot query builder</returns>
    ISnapshotQuery Snapshots();
}
```

**Usage:**
```csharp
// Query snapshots for a specific component
var snapshots = await _dataContext.Snapshots()
    .ForComponent<Invoice>(invoiceId)
    .ToListAsync();

// Query snapshots in date range
var recentSnapshots = await _dataContext.Snapshots()
    .ForEntity(entityId)
    .Between(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow)
    .ToListAsync();

// Restore to specific version
var restored = await _dataContext.Snapshots()
    .ForComponent<Invoice>(invoiceId)
    .AtVersion(5)
    .RestoreAsync<Invoice>();
```

## Automatic Snapshot Behavior

### Commit() Method Changes

The `DataContext.Commit<T>()` method automatically captures snapshots for versioned components:

```csharp
public async Task Commit<TComponent>(TComponent component) 
    where TComponent : BaseModel, IComponent, new()
{
    // Check if this is an update by looking for existing component
    TComponent? existing = await GetExistingComponent(component.Id);
    
    // Check if component supports versioning
    if (component is IVersionedComponent versionedComponent)
    {
        if (existing != null)
        {
            // This is an update - capture snapshot of existing state
            _ = Task.Run(async () => await CaptureSnapshotAsync(existing, "UPDATE"));
            versionedComponent.Version = (versionedComponent.Version ?? 0) + 1;
        }
        else
        {
            // This is an insert - set initial version
            versionedComponent.Version = 1;
            // After save, capture initial snapshot
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // Small delay to ensure save completes
                await CaptureSnapshotAsync(component, "CREATE");
            });
        }
    }
    
    await _client.Postgrest.Table<TComponent>().Upsert(component);
}
```

### TryCommit() Method Changes

The `DataContext.TryCommit<T>()` method also supports snapshot capture:

```csharp
public async Task<bool> TryCommit<TComponent>(TComponent component) 
    where TComponent : BaseModel, IComponent, new()
{
    try
    {
        // ... concurrency checking logic ...
        
        if (existing == null)
        {
            // New record - check if versioning is supported
            if (component is IVersionedComponent versionedComponent)
            {
                versionedComponent.Version = 1;
                _ = Task.Run(async () => await CaptureSnapshotAsync(component, "CREATE"));
            }
            
            await _client.Postgrest.Table<TComponent>().Upsert(component);
            return true;
        }

        // Fire and forget snapshot of the existing state before update (if versioned)
        if (existing is IVersionedComponent)
        {
            _ = Task.Run(async () => await CaptureSnapshotAsync(existing, "UPDATE"));
        }

        // ... rest of update logic ...
    }
    catch (Exception ex)
    {
        // ... error handling ...
        throw;
    }
}
```

## Query Examples

### Basic Queries

```csharp
// Get all snapshots for a component
var allSnapshots = await _dataContext.Snapshots()
    .ForComponent<Invoice>(invoiceId)
    .ToListAsync();

// Get snapshots for an entire entity
var entitySnapshots = await _dataContext.Snapshots()
    .ForEntity(entityId)
    .ToListAsync();

// Get snapshots of a specific type
var invoiceSnapshots = await _dataContext.Snapshots()
    .ForComponentType<Invoice>()
    .ToListAsync();
```

### Date Range Queries

```csharp
// Last 30 days
var recentSnapshots = await _dataContext.Snapshots()
    .ForComponent<Invoice>(invoiceId)
    .Between(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow)
    .ToListAsync();

// Specific date range
var quarterSnapshots = await _dataContext.Snapshots()
    .ForEntity(entityId)
    .Between(new DateTime(2025, 1, 1), new DateTime(2025, 3, 31))
    .ToListAsync();
```

### Version-Specific Queries

```csharp
// Get specific version
var version3 = await _dataContext.Snapshots()
    .ForComponent<Invoice>(invoiceId)
    .AtVersion(3)
    .RestoreAsync<Invoice>();

// Check if version exists
var snapshotRecord = await _dataContext.Snapshots()
    .ForComponent<Invoice>(invoiceId)
    .AtVersion(5)
    .FirstOrDefaultAsync();

if (snapshotRecord != null)
{
    var snapshots = snapshotRecord.GetSnapshots();
    var hasVersion5 = snapshots.Any(s => s.Version == 5);
}
```

### Processing Snapshot Data

```csharp
// Get all versions of a component
var snapshotRecord = await _dataContext.Snapshots()
    .ForComponent<Invoice>(invoiceId)
    .FirstOrDefaultAsync();

if (snapshotRecord != null)
{
    var snapshots = snapshotRecord.GetSnapshots();
    
    foreach (var snapshot in snapshots.OrderBy(s => s.Version))
    {
        var invoice = snapshot.Deserialize<Invoice>();
        Console.WriteLine($"Version {snapshot.Version}: Status={invoice.Status}, Amount={invoice.Amount}");
    }
}

// Find when status changed
var statusChanges = snapshots
    .Select(s => new { 
        Version = s.Version, 
        Status = s.Deserialize<Invoice>().Status,
        Timestamp = s.Timestamp 
    })
    .Where((current, index) => 
        index == 0 || current.Status != snapshots[index - 1].Deserialize<Invoice>().Status)
    .ToList();
```

## Error Handling

### Common Exceptions

```csharp
try
{
    var restored = await _dataContext.Snapshots()
        .ForComponent<Invoice>(invoiceId)
        .AtVersion(999)
        .RestoreAsync<Invoice>();
}
catch (InvalidOperationException ex)
{
    // Thrown when JSON cannot be deserialized to target type
    _logger.LogError(ex, "Failed to deserialize snapshot");
}

// Check for null results
var snapshot = await _dataContext.Snapshots()
    .ForComponent<Invoice>(nonExistentId)
    .FirstOrDefaultAsync();

if (snapshot == null)
{
    // No snapshots found for this component
}
```

### Validation

```csharp
public async Task<T?> SafeRestore<T>(Guid componentId, int version) 
    where T : class, IComponent
{
    try
    {
        var restored = await _dataContext.Snapshots()
            .ForComponent<T>(componentId)
            .AtVersion(version)
            .RestoreAsync<T>();
            
        return restored;
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning(ex, "Failed to restore {ComponentType} {ComponentId} to version {Version}", 
            typeof(T).Name, componentId, version);
        return null;
    }
}
```

## Performance Considerations

### Efficient Queries

```csharp
// FAST: Component-specific queries use unique index
var componentSnapshots = await _dataContext.Snapshots()
    .ForComponent<Invoice>(invoiceId)  // Uses unique index on component_id
    .FirstOrDefaultAsync();

// SLOWER: Entity queries scan multiple components
var entitySnapshots = await _dataContext.Snapshots()
    .ForEntity(entityId)  // Uses index on entity_id but returns more data
    .ToListAsync();

// SLOWEST: Type queries scan all components of that type
var allInvoiceSnapshots = await _dataContext.Snapshots()
    .ForComponentType<Invoice>()  // Uses index on component_type
    .ToListAsync();
```

### Memory Management

```csharp
// Process large snapshot sets in batches
var snapshotRecords = await _dataContext.Snapshots()
    .ForEntity(entityId)
    .ToListAsync();

foreach (var record in snapshotRecords)
{
    var snapshots = record.GetSnapshots();
    
    // Process snapshots one at a time to avoid loading all into memory
    foreach (var snapshot in snapshots)
    {
        // Process individual snapshot
        ProcessSnapshot(snapshot);
        
        // Optionally dispose JsonDocument if processing many
        snapshot.Data.Dispose();
    }
}
```

This API reference provides complete coverage of the snapshot versioning system's public interface and usage patterns.