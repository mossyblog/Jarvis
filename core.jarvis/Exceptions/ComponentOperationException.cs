namespace core.jarvis.Exceptions;

/// <summary>
/// Exception thrown when a component operation fails.
/// </summary>
public class ComponentOperationException : DomainException
{
    /// <summary>
    /// Gets the type of component that failed.
    /// </summary>
    public string ComponentType { get; }

    /// <summary>
    /// Gets the operation that failed.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentOperationException"/> class.
    /// </summary>
    /// <param name="componentType">The type of component.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="message">The error message.</param>
    public ComponentOperationException(
        string componentType, 
        string operation, 
        string message)
        : base(
            GenerateErrorCode(componentType, operation), 
            message, 
            new { ComponentType = componentType, Operation = operation })
    {
        ComponentType = componentType;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentOperationException"/> class with an inner exception.
    /// </summary>
    /// <param name="componentType">The type of component.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ComponentOperationException(
        string componentType, 
        string operation, 
        string message, 
        Exception innerException)
        : base(
            GenerateErrorCode(componentType, operation), 
            message, 
            innerException, 
            new { ComponentType = componentType, Operation = operation })
    {
        ComponentType = componentType;
        Operation = operation;
    }

    private static string GenerateErrorCode(string? componentType, string? operation)
    {
        var componentPart = string.IsNullOrEmpty(componentType) ? "" : componentType.ToUpperInvariant();
        var operationPart = string.IsNullOrEmpty(operation) ? "" : operation.ToUpperInvariant();
        
        // Handle the case where both are empty/null to avoid double underscore
        if (string.IsNullOrEmpty(componentPart) && string.IsNullOrEmpty(operationPart))
        {
            return "_FAILED";
        }
        
        // If one is empty, don't include the separator for that part
        if (string.IsNullOrEmpty(componentPart))
        {
            return $"{operationPart}_FAILED";
        }
        
        if (string.IsNullOrEmpty(operationPart))
        {
            return $"{componentPart}_FAILED";
        }
        
        // Both have values
        return $"{componentPart}_{operationPart}_FAILED";
    }
}