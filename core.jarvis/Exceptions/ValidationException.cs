namespace core.jarvis.Exceptions;

/// <summary>
/// Exception thrown when validation fails for one or more fields.
/// </summary>
public class ValidationException : DomainException
{
    /// <summary>
    /// Gets the validation errors by field name.
    /// </summary>
    public Dictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a message.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public ValidationException(string message)
        : base("VALIDATION_ERROR", message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class for a single field error.
    /// </summary>
    /// <param name="field">The field that failed validation.</param>
    /// <param name="error">The validation error message.</param>
    public ValidationException(string field, string error)
        : base("VALIDATION_ERROR", $"Validation failed for {field}")
    {
        Errors = new Dictionary<string, string[]> { [field] = new[] { error } };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class for multiple field errors.
    /// </summary>
    /// <param name="errors">The validation errors by field name.</param>
    public ValidationException(Dictionary<string, string[]> errors)
        : base("VALIDATION_ERROR", "Validation failed for multiple fields")
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}