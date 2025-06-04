namespace core.jarvis.Data;

/// <summary>
/// Represents the core data structure for an entity within the Jarvis system.
/// Holds identity, naming, and relationship identifiers.
/// Properties are automatically mapped to snake_case by PgTable.
/// </summary>
public class Entity
{
    public Entity()
    {
    }

    public Entity(Guid id)
    {
        Id = id;
    }

    public Entity(Guid id, string name) : this(id)
    {
        Name = name ?? string.Empty;
    }

    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the entity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent entity ID. Maps to parentid in database.
    /// </summary>
    public Guid ParentId { get; set; } // Guid.Empty = no parent

    /// <summary>
    /// List of child entity IDs. Maps to childrenids in database.
    /// </summary>
    public List<Guid> ChildrenIds { get; set; } = new List<Guid>();
}