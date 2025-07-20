namespace core.jarvis.api.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public Dictionary<string, List<string>> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, List<string>>();
    }

    public ValidationException(string message, Dictionary<string, List<string>> errors) : base(message)
    {
        Errors = errors ?? new Dictionary<string, List<string>>();
    }

    public ValidationException(string field, string error) : base($"Validation failed for {field}")
    {
        Errors = new Dictionary<string, List<string>>
        {
            [field] = new List<string> { error }
        };
    }
}