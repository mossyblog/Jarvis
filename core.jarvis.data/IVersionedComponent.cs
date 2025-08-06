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