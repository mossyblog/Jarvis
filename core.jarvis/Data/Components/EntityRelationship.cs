namespace core.jarvis.Data.Components;

/// <summary>
/// Component that tracks parent-child relationships between entities.
/// Supports hierarchical structures like Parent -> Children.
/// </summary>
public class EntityRelationship : BaseComponent, IVersionedComponent
{
    /// <summary>
    /// The parent entity ID if this entity has a parent.
    /// Null if this is a root entity.
    /// </summary>
    public Guid? ParentId { get; set; }
    
    /// <summary>
    /// Collection of child entity IDs.
    /// Empty if this entity has no children.
    /// </summary>
    public Guid[] ChildrenIds { get; set; } = Array.Empty<Guid>();
    
    /// <summary>
    /// The type of the parent entity (e.g., "Order", "Blog").
    /// Helps with type-safe queries and relationship validation.
    /// </summary>
    public string? ParentType { get; set; }
    
    /// <summary>
    /// The types of child entities (e.g., "Invoice").
    /// Stored as JSONB in the database.
    /// Maps child entity ID to its type for type-safe operations.
    /// </summary>
    public string ChildTypes { get; set; } = "{}";
    
    /// <summary>
    /// Version number for optimistic concurrency control.
    /// </summary>
    public int? Version { get; set; }
}