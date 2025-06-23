using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing token validation state.
/// </summary>
public record TokenValidation : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the token is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The user ID associated with the token (if valid).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// When the token expires (if valid).
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Token claims (if valid).
    /// </summary>
    public Dictionary<string, string>? Claims { get; init; }

    /// <summary>
    /// Error message if token is invalid.
    /// </summary>
    public string? ErrorMessage { get; init; }
}