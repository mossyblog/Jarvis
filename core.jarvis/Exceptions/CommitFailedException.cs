namespace core.jarvis.Exceptions;

/// <summary>
/// Represents an exception that occurs when a commit operation fails.
/// </summary>
/// <remarks>
/// This exception is generally used to wrap other exceptions that might arise
/// during commit operations, providing additional context about the failure (e.g., the ID of the affected entity).
/// It is used in scenarios where a failure occurs while saving or deleting data within a data context.
/// </remarks>
/// <example>
/// Commonly thrown during commit operations when unexpected errors occur, offering the ability to include
/// the ID of the entity that caused the failure to aid in debugging and logging.
/// </example>
[Serializable] 
public class CommitFailedException : Exception
{
    // Summary: The ID of the entity that caused the commit failure, if applicable.
    public Guid? EntityId { get; set; } 
    
    // Summary: Initializes a new instance of the CommitFailedException class.
    public CommitFailedException() { }

    // Summary: Initializes a new instance of the CommitFailedException class with a specified error message.
    public CommitFailedException(string message) : base(message) { }
    
    // Summary: Initializes a new instance of the CommitFailedException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    public CommitFailedException(string message, Exception innerException) : base(message, innerException) { }
    
    // Summary: Initializes a new instance of the CommitFailedException class with a specified error message, a reference to the inner exception that is the cause of this exception, and the ID of the entity that caused the failure.
    public CommitFailedException(string message, Exception innerException, Guid entityId) : base(message, innerException)
    {
        EntityId = entityId;
    }
}
