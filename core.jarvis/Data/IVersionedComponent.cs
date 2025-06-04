namespace core.jarvis.Data;

/// <summary>
/// Interface for components that support versioning and snapshot tracking.
/// Components that implement this interface will have automatic snapshot tracking.
/// </summary>
public interface IVersionedComponent : IComponent
{
    /// <summary>
    /// Version number for snapshot tracking. Increments on each update.
    /// Maps to version column in database.
    /// </summary>
    int? Version { get; set; }
}