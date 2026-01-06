namespace core.jarvis.Exceptions;

/// <summary>
/// Exception thrown when an optimistic concurrency conflict is detected during data persistence.
/// This typically indicates that the data being saved was modified by another process
/// since it was originally read.
/// </summary>
public class ConcurrencyException : DomainException
{
    /// <summary>
    /// Gets the ID of the entity involved in the concurrency conflict.
    /// </summary>
    public Guid? EntityId { get; }

    /// <summary>
    /// Gets the name of the component type involved in the concurrency conflict.
    /// </summary>
    public string? ComponentType { get; }

    /// <summary>
    /// Gets the expected version number.
    /// </summary>
    public int? ExpectedVersion { get; }

    /// <summary>
    /// Gets the actual version number found in the database.
    /// </summary>
    public int? ActualVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    public ConcurrencyException()
        : base("CONCURRENCY_CONFLICT", "Concurrency conflict detected")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConcurrencyException(string message)
        : base("CONCURRENCY_CONFLICT", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public ConcurrencyException(string message, Exception inner)
        : base("CONCURRENCY_CONFLICT", message, inner)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class
    /// with details about the concurrency conflict.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="componentType">The type of component.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <param name="actualVersion">The actual version found.</param>
    public ConcurrencyException(Guid entityId, string componentType, int? expectedVersion, int? actualVersion)
        : base("CONCURRENCY_CONFLICT",
            $"Concurrency conflict for {componentType} on entity {entityId}. Expected version: {expectedVersion}, Actual version: {actualVersion}")
    {
        EntityId = entityId;
        ComponentType = componentType;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
