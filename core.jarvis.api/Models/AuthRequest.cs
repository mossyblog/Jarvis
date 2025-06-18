using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing an authentication request.
/// </summary>
public record AuthRequest : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// User's email address for authentication.
    /// </summary>
    public string Email { get; init; } = string.Empty;
    
    /// <summary>
    /// User's password (will be validated, not stored).
    /// </summary>
    public string Password { get; init; } = string.Empty;
    
    /// <summary>
    /// Optional two-factor authentication code.
    /// </summary>
    public string? TwoFactorCode { get; init; }
    
    /// <summary>
    /// Client identifier for tracking sessions.
    /// </summary>
    public string? ClientId { get; init; }
}