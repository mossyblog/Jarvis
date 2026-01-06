namespace core.jarvis.Data.Query;

/// <summary>
/// Implementation of snapshot query functionality
/// </summary>
public class SnapshotQuery : ISnapshotQuery
{
    private readonly IPgClient _pgClient;
    private Guid? _entityId;
    private Guid? _componentId;
    private string? _componentType;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private int? _version;
    
    public SnapshotQuery(IPgClient pgClient)
    {
        _pgClient = pgClient ?? throw new ArgumentNullException(nameof(pgClient));
    }
    
    public ISnapshotQuery ForEntity(Guid entityId)
    {
        _entityId = entityId;
        return this;
    }
    
    public ISnapshotQuery ForComponent<T>(Guid componentId) where T : class, IComponent
    {
        _componentId = componentId;
        _componentType = typeof(T).Name;
        return this;
    }
    
    public ISnapshotQuery ForComponentType<T>() where T : class, IComponent
    {
        _componentType = typeof(T).Name;
        return this;
    }
    
    public ISnapshotQuery Between(DateTime start, DateTime end)
    {
        _startDate = start;
        _endDate = end;
        return this;
    }
    
    public ISnapshotQuery AtVersion(int version)
    {
        _version = version;
        return this;
    }
    
    public async Task<IEnumerable<ComponentSnapshots>> ToList()
    {
        // Build raw SQL query since ComponentSnapshots doesn't implement IComponent
        // Use string interpolation for simplicity in test environment
        var whereClause = "WHERE 1=1";

        if (_entityId.HasValue)
            whereClause += $" AND entity_id = '{_entityId.Value}'";

        if (_componentId.HasValue)
            whereClause += $" AND component_id = '{_componentId.Value}'";

        if (!string.IsNullOrEmpty(_componentType))
            whereClause += $" AND component_type = '{_componentType}'";

        var sql = $@"SELECT 
            id as Id,
            entity_id as EntityId, 
            component_type as ComponentType, 
            component_id as ComponentId, 
            snapshots as Snapshots, 
            created_at as CreatedAt, 
            last_updated as LastUpdated 
            FROM component_snapshots {whereClause}";

        // Execute raw SQL query with explicit column aliases for proper mapping
        var records = await _pgClient.Query<ComponentSnapshots>(sql, null);

        // Debug: Log what we retrieved
        foreach (var record in records)
        {
            Console.WriteLine($"Retrieved snapshot record:");
            Console.WriteLine($"  ID: {record.Id}");
            Console.WriteLine($"  EntityId: {record.EntityId}");
            Console.WriteLine($"  ComponentType: {record.ComponentType}");
            Console.WriteLine($"  ComponentId: {record.ComponentId}");
            Console.WriteLine($"  Snapshots length: {record.Snapshots?.Length ?? 0}");
            Console.WriteLine($"  Snapshots content: {record.Snapshots?.Substring(0, Math.Min(100, record.Snapshots?.Length ?? 0))}");
            Console.WriteLine($"  CreatedAt: {record.CreatedAt}");
            Console.WriteLine($"  LastUpdated: {record.LastUpdated}");
        }

        // Apply client-side filtering for date range and version since they're in JSONB
        if (_startDate.HasValue || _endDate.HasValue || _version.HasValue)
        {
            records = records.Where(r => FilterSnapshots(r)).ToList();
        }

        return records;
    }
    
    public async Task<ComponentSnapshots?> FirstOrDefault()
    {
        var results = await ToList();
        return results.FirstOrDefault();
    }
    
    public async Task<T?> Restore<T>() where T : class, IComponent
    {
        var record = await FirstOrDefault();
        if (record == null) return null;
        
        var snapshots = record.GetSnapshots();
        if (!snapshots.Any()) return null;
        
        // If version specified, find that specific version
        if (_version.HasValue)
        {
            var snapshot = snapshots.FirstOrDefault(s => s.Version == _version.Value);
            return snapshot?.Deserialize<T>();
        }
        
        // Otherwise return the latest snapshot
        var latest = snapshots.OrderByDescending(s => s.Version).First();
        return latest.Deserialize<T>();
    }
    
    private bool FilterSnapshots(ComponentSnapshots record)
    {
        var snapshots = record.GetSnapshots();
        
        if (_version.HasValue)
        {
            // Check if any snapshot has the requested version
            if (!snapshots.Any(s => s.Version == _version.Value))
                return false;
        }
        
        if (_startDate.HasValue || _endDate.HasValue)
        {
            // Check if any snapshot falls within the date range
            var filteredSnapshots = snapshots.AsEnumerable();
            
            if (_startDate.HasValue)
                filteredSnapshots = filteredSnapshots.Where(s => s.Timestamp >= _startDate.Value);
                
            if (_endDate.HasValue)
                filteredSnapshots = filteredSnapshots.Where(s => s.Timestamp <= _endDate.Value);
                
            if (!filteredSnapshots.Any())
                return false;
        }
        
        return true;
    }
}