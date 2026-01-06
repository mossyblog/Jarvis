namespace core.jarvis.Exceptions;

/// <summary>
/// Represents an error that occurs when a required component is not found on an entity.
/// </summary>
public class ComponentNotFoundException : DomainException
{
    /// <summary>
    /// Gets the name of the component type that was not found.
    /// </summary>
    public string? ComponentTypeName { get; }

    /// <summary>
    /// Gets the ID of the entity where the component was expected.
    /// </summary>
    public Guid? EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class.
    /// </summary>
    public ComponentNotFoundException()
        : base("COMPONENT_NOT_FOUND", "Component not found")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ComponentNotFoundException(string message)
        : base("COMPONENT_NOT_FOUND", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public ComponentNotFoundException(string message, Exception inner)
        : base("COMPONENT_NOT_FOUND", message, inner)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class with details about the missing component and entity.
    /// </summary>
    /// <param name="componentTypeName">The name of the component type that was not found.</param>
    /// <param name="entityId">The ID of the entity where the component was expected.</param>
    public ComponentNotFoundException(string componentTypeName, Guid entityId)
        : base("COMPONENT_NOT_FOUND", $"Component of type '{componentTypeName}' was not found on entity with ID '{entityId}'.")
    {
        ComponentTypeName = componentTypeName;
        EntityId = entityId;
    }
}
