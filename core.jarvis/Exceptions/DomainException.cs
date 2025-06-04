namespace core.jarvis.Exceptions;

/// <summary>
/// Base exception for all domain-specific errors in the Jarvis system.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Gets the error code that categorizes this exception.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets additional context information about the error.
    /// </summary>
    public object? Context { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="context">Optional context information.</param>
    protected DomainException(string code, string message, object? context = null)
        : base(message)
    {
        Code = code;
        Context = context;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="context">Optional context information.</param>
    protected DomainException(string code, string message, Exception innerException, object? context = null)
        : base(message, innerException)
    {
        Code = code;
        Context = context;
    }
}