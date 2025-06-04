# Snapshot Versioning Guide

This guide explains how to implement and use the snapshot versioning system in Jarvis ECS framework for automatic audit trails and data recovery.

## Overview

Snapshot versioning automatically captures the state of components when they are modified, providing:
- **Audit Trail**: Complete history of all changes to your data
- **Data Recovery**: Ability to restore previous states
- **Fire-and-Forget**: Zero performance impact on business operations (snapshots are captured asynchronously)
- **Automatic Versioning**: Version numbers are managed automatically
- **Compliance**: Built-in data lineage for regulatory requirements

## Quick Start

### 1. Enable Versioning on Your Component

```csharp
using core.jarvis.Data;

public class Invoice : BaseComponent, IVersionedComponent
{
    public decimal Amount { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string? ApprovalReason { get; set; }
    
    // Required for IVersionedComponent
    public int? Version { get; set; }
```

### 2. Use Normal Commit Operations

Snapshots are captured automatically when you save components using `DataContext.Commit()` or `DataContext.TryCommit()`:

```csharp
public class InvoiceHandler : DataContextComponentHandler<Invoice>
{
    public InvoiceHandler(IDataContext dataContext, ILogger<InvoiceHandler> logger)
        : base(dataContext, logger) { }

    public async Task ApproveInvoice(string approvalReason)
    {
        var invoice = await Get();
        
        // Version is managed automatically - no need to set it
        invoice.Status = "APPROVED";
        invoice.ApprovalReason = approvalReason;
        
        // Snapshot of the PREVIOUS state is captured before the update
        await DataContext.Commit(invoice);  // Version incremented automatically
    }
    
    public async Task<bool> TryApproveInvoice(string approvalReason)
    {
        var invoice = await Get();
        invoice.Status = "APPROVED";
        invoice.ApprovalReason = approvalReason;
        
        // TryCommit also captures snapshots automatically
        return await TryCommit(invoice);  // Returns false on concurrency conflict
    }
}
```

### 3. Query Historical Data

```csharp
public class InvoiceAuditService
{
    private readonly IDataContext _dataContext;
    
    public async Task<List<Snapshot>> GetInvoiceHistory(Guid invoiceId)
    {
        // Get the snapshot record for this component
        var snapshotRecord = await _dataContext.Snapshots()
            .ForComponent<Invoice>(invoiceId)
            .FirstOrDefault();
            
        if (snapshotRecord == null)
            return new List<Snapshot>();
            
        // All versions are stored in a single record
        return snapshotRecord.GetSnapshots()
            .OrderBy(s => s.Version)
            .ToList();
    }
    
    public async Task<Invoice?> RestoreToVersion(Guid invoiceId, int version)
    {
        // RestoreAsync returns the component directly
        return await _dataContext.Snapshots()
            .ForComponent<Invoice>(invoiceId)
            .AtVersion(version)
            .Restore<Invoice>();
    }
    
    public async Task<List<Invoice>> GetChangesInDateRange(
        Guid invoiceId, 
        DateTime start, 
        DateTime end)
    {
        // Between() applies client-side filtering on snapshot timestamps
        var records = await _dataContext.Snapshots()
            .ForComponent<Invoice>(invoiceId)
            .Between(start, end)
            .ToList();
            
        // Since filtering is client-side, extract matching snapshots
        if (!records.Any()) return new List<Invoice>();
        
        return records.First().GetSnapshots()
            .Where(s => s.Timestamp >= start && s.Timestamp <= end)
            .Select(s => s.Deserialize<Invoice>())
            .ToList();
    }
}
```

## Core Concepts

### IVersionedComponent Interface

Only components implementing `IVersionedComponent` have snapshots captured:

```csharp
public interface IVersionedComponent : IComponent
{
    /// <summary>
    /// Version number for snapshot tracking. Increments on each update.
    /// Maps to version column in database.
    /// </summary>
    int? Version { get; set; }
}
```

**Why use an interface?**
- **Backward Compatibility**: Existing components continue working unchanged
- **Opt-In Model**: Only components that need versioning pay the storage cost
- **Clear Intent**: Explicit declaration of versioning requirements

### Snapshot Storage Model

Snapshots use a **one-row-per-component** design where all historical versions are stored in a single JSONB array:

```sql
CREATE TABLE component_snapshots (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    entity_id UUID NOT NULL,
    component_type VARCHAR(255) NOT NULL,
    component_id UUID NOT NULL UNIQUE,
    snapshots JSONB NOT NULL DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX idx_snapshots_entity ON component_snapshots(entity_id);
CREATE INDEX idx_snapshots_component ON component_snapshots(component_id);
CREATE INDEX idx_snapshots_type ON component_snapshots(component_type);
```

The `snapshots` column contains an array of all versions:
```json
[
  {
    "version": 1,
    "operation": "CREATE",
    "timestamp": "2025-01-15T10:30:00Z",
    "created_by": "system",
    "data": {
      "Id": "550e8400-e29b-41d4-a716-446655440000",
      "OwnerEntityId": "660e8400-e29b-41d4-a716-446655440000",
      "Amount": 1000.00,
      "Status": "DRAFT",
      "Version": 1,
      "CreatedAt": "2025-01-15T10:30:00Z",
      "UpdatedAt": "2025-01-15T10:30:00Z"
    }
  },
  {
    "version": 2,
    "operation": "UPDATE",
    "timestamp": "2025-01-15T11:00:00Z",
    "created_by": "system",
    "data": {
      "Id": "550e8400-e29b-41d4-a716-446655440000",
      "OwnerEntityId": "660e8400-e29b-41d4-a716-446655440000",
      "Amount": 1500.00,
      "Status": "APPROVED",
      "Version": 2,
      "CreatedAt": "2025-01-15T10:30:00Z",
      "UpdatedAt": "2025-01-15T11:00:00Z"
    }
  }
]
```

**Key Points:**
- Each component has ONE snapshot record containing ALL versions
- New versions are appended to the existing array
- The `data` object excludes BaseModel properties to avoid serialization issues

### Synchronous Snapshot Capture

Snapshots are captured **synchronously** as part of the commit operation:

```csharp
// In DataContext.TryCommit - actual implementation
public async Task<bool> TryCommit<TComponent>(TComponent component) 
    where TComponent : class, IComponent, new()
{
    // For new components (version 1)
    if (existing == null)
    {
        if (component is IVersionedComponent versioned)
        {
            versioned.Version = 1;
        }
        
        await _pgClient.From<TComponent>().Insert(component);
        
        // Capture initial snapshot AFTER insert
        if (component is IVersionedComponent)
        {
            await CaptureSnapshotAsync(component, "CREATE");
        }
    }
    // For updates
    else if (component is IVersionedComponent versionedComp && existing is IVersionedComponent existingVersioned)
    {
        // Version-based concurrency check
        if (versionedComp.Version != existingVersioned.Version)
            return false; // Concurrency conflict
        
        // Snapshot the EXISTING state BEFORE update
        await Snapshot(existing, "UPDATE");
        
        // Increment version
        versionedComp.Version = (existingVersioned.Version ?? 0) + 1;
        
        await _pgClient.From<TComponent>().Upsert(component);
    }
}
```

**Important Notes:**
- Snapshots are captured **synchronously** within the same operation
- Snapshot errors are logged via `ErrorHandlingPolicy.LogAndContinue()` but don't fail the main operation
- For creates: captures the state AFTER the insert
- For updates: captures the state BEFORE the change (preserving history)
- Version numbers are automatically incremented on each update

## Advanced Usage

### Date Range Queries

```csharp
public async Task<List<Snapshot>> GetChangesInPeriod(
    Guid invoiceId, 
    DateTime start, 
    DateTime end)
{
    // Between() uses client-side filtering on the JSONB data
    var records = await _dataContext.Snapshots()
        .ForComponent<Invoice>(invoiceId)
        .Between(start, end)
        .ToList();
        
    // Extract snapshots from the single record
    if (!records.Any())
        return new List<Snapshot>();
        
    return records.First().GetSnapshots()
        .Where(s => s.Timestamp >= start && s.Timestamp <= end)
        .OrderBy(s => s.Version)
        .ToList();
}
```

### Entity-Level Snapshots

```csharp
public async Task<Dictionary<string, List<Snapshot>>> GetEntityHistory(Guid entityId)
{
    // Get all snapshot records for an entity
    var snapshotRecords = await _dataContext.Snapshots()
        .ForEntity(entityId)
        .ToList();
        
    var history = new Dictionary<string, List<Snapshot>>();
    
    foreach (var record in snapshotRecords)
    {
        // Group snapshots by component type
        history[record.ComponentType] = record.GetSnapshots()
            .OrderBy(s => s.Version)
            .ToList();
    }
    
    return history;
}

public async Task<List<object>> GetEntityStateAtTime(Guid entityId, DateTime pointInTime)
{
    var records = await _dataContext.Snapshots()
        .ForEntity(entityId)
        .ToList();
        
    var components = new List<object>();
    
    foreach (var record in records)
    {
        // Find the latest snapshot before the specified time
        var snapshot = record.GetSnapshots()
            .Where(s => s.Timestamp <= pointInTime)
            .OrderByDescending(s => s.Version)
            .FirstOrDefault();
            
        if (snapshot != null)
        {
            var componentType = Type.GetType(record.ComponentType);
            if (componentType != null)
            {
                var component = snapshot.Deserialize(componentType);
                components.Add(component);
            }
        }
    }
    
    return components;
}
```

### Working with Snapshot Data

The snapshot system captures all component properties automatically:

```csharp
public class InvoiceAuditHandler : DataContextComponentHandler<Invoice>
{
    public InvoiceAuditHandler(IDataContext dataContext, ILogger<InvoiceAuditHandler> logger)
        : base(dataContext, logger) { }

    public async Task AnalyzeInvoiceChanges(Guid invoiceId)
    {
        var record = await DataContext.Snapshots()
            .ForComponent<Invoice>(invoiceId)
            .FirstOrDefault();
            
        if (record == null) return;
        
        var snapshots = record.GetSnapshots();
        
        foreach (var snapshot in snapshots)
        {
            // Access snapshot metadata
            Logger.LogInformation("Version: {Version}", snapshot.Version);
            Logger.LogInformation("Operation: {Operation}", snapshot.Operation);
            Logger.LogInformation("Timestamp: {Timestamp}", snapshot.Timestamp);
            Logger.LogInformation("Created By: {CreatedBy}", snapshot.CreatedBy); // Always "system" currently
            
            // Deserialize to get the component state
            var invoice = snapshot.Deserialize<Invoice>();
            Logger.LogInformation("Status at v{Version}: {Status}", snapshot.Version, invoice.Status);
            Logger.LogInformation("Amount at v{Version}: {Amount}", snapshot.Version, invoice.Amount);
        }
    }
}
```

**Note**: The `created_by` field is currently always set to "system". User tracking would require extending the snapshot capture mechanism.

## Database Schema Requirements

### Component Table Schema

Every versioned component table must include a version column:

```sql
-- Add version column to existing tables
ALTER TABLE invoice ADD COLUMN version INTEGER;
ALTER TABLE payment ADD COLUMN version INTEGER;

-- Create index for performance
CREATE INDEX idx_invoice_version ON invoice(version);
```

### Snapshot Table Schema

The snapshot table is automatically created by the setup script:

```sql
-- This is handled automatically by setup-test-database.sql
CREATE TABLE component_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id UUID NOT NULL,
    component_type TEXT NOT NULL,
    component_id UUID NOT NULL UNIQUE,
    snapshots JSONB NOT NULL DEFAULT '[]',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_snapshots_entity ON component_snapshots(entity_id);
CREATE INDEX idx_snapshots_component ON component_snapshots(component_id);
CREATE INDEX idx_snapshots_type ON component_snapshots(component_type);
```

## Performance Considerations

### Storage Efficiency

The one-row-per-component design is storage efficient:
- **JSONB Compression**: PostgreSQL automatically compresses similar JSON structures
- **Single Row per Component**: All versions in one row eliminates JOIN operations
- **Append-Only**: New versions are appended to the JSONB array
- **No Duplicates**: Each component has exactly one snapshot record

### Query Performance

```csharp
// FAST: Direct component lookup (single row fetch)
var record = await _dataContext.Snapshots()
    .ForComponent<Invoice>(specificId)
    .FirstOrDefault();

// MODERATE: Entity-wide queries (multiple rows, but indexed)
var records = await _dataContext.Snapshots()
    .ForEntity(entityId)
    .ToList();

// IMPORTANT: Version filtering is done client-side
// The entire snapshot array is fetched, then filtered in memory
var v3State = await _dataContext.Snapshots()
    .ForComponent<Invoice>(id)
    .AtVersion(3)  // Client-side filter
    .Restore<Invoice>();
```

### Memory Usage

Snapshots exclude BaseModel properties to reduce storage:

```json
{
  "version": 2,
  "operation": "UPDATE",
  "timestamp": "2025-01-15T11:00:00Z",
  "created_by": "system",
  "data": {
    // Component properties only - no BaseModel fields
    "Id": "550e8400-e29b-41d4-a716-446655440000",
    "OwnerEntityId": "660e8400-e29b-41d4-a716-446655440000",
    "Amount": 1500.00,
    "Status": "APPROVED",
    "Version": 2,
    "CreatedAt": "2025-01-15T10:30:00Z",
    "UpdatedAt": "2025-01-15T11:00:00Z"
  }
}
```

## Best Practices

### 1. Version Management

Let the system handle versions automatically:

```csharp
// DON'T manually set versions
invoice.Version = 5;  // ❌ Don't do this

// DO let the system manage versions
await Commit(invoice);  // ✅ Version incremented automatically
```

### 2. Monitor Snapshot Growth

```sql
-- Check snapshot storage usage
SELECT 
    component_type,
    COUNT(*) as component_count,
    AVG(jsonb_array_length(snapshots)) as avg_snapshots_per_component,
    pg_size_pretty(SUM(pg_column_size(snapshots))) as total_size
FROM component_snapshots 
GROUP BY component_type;
```

### 3. Archive Old Snapshots

Consider archiving snapshots older than your retention policy:

```sql
-- Archive snapshots older than 7 years
CREATE TABLE component_snapshots_archive AS 
SELECT * FROM component_snapshots 
WHERE created_at < NOW() - INTERVAL '7 years';

-- Clean up old snapshots (implement carefully)
-- DELETE FROM component_snapshots 
-- WHERE created_at < NOW() - INTERVAL '7 years';
```

### 4. Test Snapshot Recovery

```csharp
[Fact]
public async Task Should_Restore_Previous_Invoice_State()
{
    // Arrange
    var entityId = Guid.NewGuid();
    var invoice = new InvoiceTestComponent 
    { 
        Id = Guid.NewGuid(),
        OwnerEntityId = entityId,
        Amount = 1000, 
        Status = "DRAFT" 
    };
    await TestDataContext().Commit(invoice);
    
    // Act - Modify
    invoice.Amount = 2000;
    invoice.Status = "APPROVED";
    await TestDataContext().Commit(invoice);
    
    // Assert - Restore version 1
    var originalState = await TestDataContext().Snapshots()
        .ForComponent<InvoiceTestComponent>(invoice.Id)
        .AtVersion(1)
        .Restore<InvoiceTestComponent>();
        
    originalState.ShouldNotBeNull();
    originalState.Amount.ShouldBe(1000);
    originalState.Status.ShouldBe("DRAFT");
    originalState.Version.ShouldBe(1);
}
```

## Troubleshooting

### Snapshots Not Being Created

1. **Check Interface Implementation**:
   ```csharp
   // Wrong
   public class Invoice : BaseModel, IComponent { }
   
   // Correct
   public class Invoice : BaseModel, IVersionedComponent { }
   ```

2. **Check Version Column**:
   ```sql
   -- Verify version column exists
   SELECT column_name FROM information_schema.columns 
   WHERE table_name = 'invoice' AND column_name = 'version';
   ```

3. **Check Database Logs**:
   ```csharp
   // Enable debug logging to see snapshot capture attempts
   builder.Services.AddLogging(logging => 
       logging.SetMinimumLevel(LogLevel.Debug));
   ```

### Performance Issues

1. **Check Snapshot Array Sizes**:
   ```sql
   -- Find components with many versions
   SELECT 
       component_id,
       component_type,
       jsonb_array_length(snapshots) as version_count,
       pg_column_size(snapshots) as size_bytes
   FROM component_snapshots
   ORDER BY jsonb_array_length(snapshots) DESC
   LIMIT 10;
   ```

2. **Monitor Async Operations**:
   ```csharp
   // Enable debug logging to see snapshot operations
   services.AddLogging(config => 
   {
       config.AddConsole();
       config.SetMinimumLevel(LogLevel.Debug);
   });
   
   // Look for snapshot capture errors in logs
   // "Failed to capture snapshot for {ComponentType} {ComponentId}"
   ```

### Restore Failures

1. **Check JSON Structure**:
   ```sql
   -- Verify snapshot JSON structure
   SELECT snapshots FROM component_snapshots 
   WHERE component_id = 'your-component-id';
   ```

2. **Version Mismatches**:
   ```csharp
   // Ensure version exists
   var snapshot = await _dataContext.Snapshots()
       .ForComponent<Invoice>(id)
       .AtVersion(999)  // May not exist
       .Restore<Invoice>();
   // Returns null if version doesn't exist
   ```

## Migration Guide

### Adding Versioning to Existing Components

1. **Update Component Class**:
   ```csharp
   // Before
   public class Payment : BaseModel, IComponent
   
   // After  
   public class Payment : BaseModel, IVersionedComponent
   {
       [Column("version")]
       public int? Version { get; set; }
   }
   ```

2. **Update Database Schema**:
   ```sql
   ALTER TABLE payment ADD COLUMN version INTEGER;
   ```

3. **Initialize Existing Data**:
   ```sql
   -- Set version 1 for existing records
   UPDATE payment SET version = 1 WHERE version IS NULL;
   ```

### Migrating from Custom Audit Solutions

If you have existing audit tables, you can migrate historical data:

```csharp
public async Task MigrateExistingAuditData()
{
    var auditRecords = await GetLegacyAuditRecords();
    
    foreach (var group in auditRecords.GroupBy(r => r.ComponentId))
    {
        var snapshots = group.OrderBy(r => r.CreatedAt)
            .Select((r, index) => new
            {
                version = index + 1,
                operation = r.Operation,
                timestamp = r.CreatedAt,
                created_by = r.UserId ?? "system",
                data = JsonSerializer.Deserialize<Dictionary<string, object>>(r.DataJson)
            })
            .ToArray();
            
        var record = new ComponentSnapshotRecord
        {
            Id = Guid.NewGuid(),
            ComponentId = group.Key,
            EntityId = group.First().EntityId,
            ComponentType = group.First().ComponentType,
            Snapshots = JsonSerializer.Serialize(snapshots),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Use Supabase client to insert
        await _supabaseClient.From<ComponentSnapshotRecord>()
            .Insert(record);
    }
}
```

## Key Takeaways

1. **Automatic Versioning**: Version numbers are managed by the framework - never set them manually
2. **Synchronous Capture**: Snapshots are captured synchronously but errors don't fail operations
3. **One Record Per Component**: All versions stored in a single JSONB array
4. **Client-Side Filtering**: Version and date filtering happens in memory after fetching
5. **Opt-In Model**: Only components implementing `IVersionedComponent` are versioned
6. **Handler Pattern**: Use `DataContextComponentHandler<T>` base class for component handlers
7. **Direct DataContext Access**: Call `DataContext.Commit()` or `DataContext.TryCommit()` for saves

This guide provides everything needed to implement and use snapshot versioning effectively in your Jarvis ECS applications.