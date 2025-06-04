namespace core.jarvis.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleException"/> class.
    /// </summary>
    /// <param name="rule">The name of the violated business rule.</param>
    /// <param name="message">The error message describing the violation.</param>
    /// <param name="context">Optional context information about the violation.</param>
    public BusinessRuleException(string rule, string message, object? context = null)
        : base($"RULE_{rule.ToUpperInvariant()}", message, context)
    {
    }
}