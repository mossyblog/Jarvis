namespace core.jarvis.Data;

/// <summary>
/// Marker interface for all components that can be attached to an Entity.
/// Concrete components implement this interface.
/// Properties are automatically mapped to snake_case by PgTable.
/// </summary>
public interface IComponent
{
    /// <summary>
    /// Unique identifier for the component instance.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The entity ID that owns this component.
    /// Maps to owner_entity_id in database.
    /// </summary>
    public Guid OwnerEntityId { get; set; }
    
    /// <summary>
    /// Timestamp of last update.
    /// Maps to updated_at in database.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}