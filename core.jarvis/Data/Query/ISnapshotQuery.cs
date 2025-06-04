namespace core.jarvis.Data.Query;

/// <summary>
/// Query interface for retrieving component snapshots
/// </summary>
public interface ISnapshotQuery
{
    /// <summary>
    /// Filter snapshots by entity ID
    /// </summary>
    ISnapshotQuery ForEntity(Guid entityId);
    
    /// <summary>
    /// Filter snapshots by component ID
    /// </summary>
    ISnapshotQuery ForComponent<T>(Guid componentId) where T : class, IComponent;
    
    /// <summary>
    /// Filter snapshots by component type
    /// </summary>
    ISnapshotQuery ForComponentType<T>() where T : class, IComponent;
    
    /// <summary>
    /// Filter snapshots between date range
    /// </summary>
    ISnapshotQuery Between(DateTime start, DateTime end);
    
    /// <summary>
    /// Filter to a specific version
    /// </summary>
    ISnapshotQuery AtVersion(int version);
    
    /// <summary>
    /// Execute query and return all matching snapshot records
    /// </summary>
    Task<IEnumerable<ComponentSnapshots>> ToList();
    
    /// <summary>
    /// Execute query and return first matching snapshot record or null
    /// </summary>
    Task<ComponentSnapshots?> FirstOrDefault();
    
    /// <summary>
    /// Restore a component from snapshots (returns latest version matching criteria)
    /// </summary>
    Task<T?> Restore<T>() where T : class, IComponent;
}