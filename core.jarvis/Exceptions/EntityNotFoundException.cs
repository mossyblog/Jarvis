namespace core.jarvis.Exceptions;

/// <summary>
/// Exception thrown when an entity cannot be found.
/// </summary>
public class EntityNotFoundException : DomainException
{
    /// <summary>
    /// Gets the ID of the entity that was not found.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// Gets the type of entity that was not found.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityId">The ID of the entity that was not found.</param>
    /// <param name="entityType">The type of entity that was not found.</param>
    public EntityNotFoundException(Guid entityId, string entityType)
        : base("NOT_FOUND", $"{entityType} with ID {entityId} not found", new { EntityId = entityId, EntityType = entityType })
    {
        EntityId = entityId;
        EntityType = entityType;
    }
}