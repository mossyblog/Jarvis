using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing an error state.
/// Used for returning error information as a component instead of anonymous objects.
/// </summary>
public record Error : IComponent
{
    /// <inheritdoc/>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <inheritdoc/>
    public Guid OwnerEntityId { get; set; }
    
    /// <inheritdoc/>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Additional error details if available.
    /// </summary>
    public Dictionary<string, string[]>? Details { get; init; }

    /// <summary>
    /// HTTP status code associated with this error.
    /// </summary>
    public int StatusCode { get; init; } = 500;
}