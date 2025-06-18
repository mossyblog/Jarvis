using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing a token refresh request.
/// </summary>
public record RefreshTokenRequest : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The refresh token to exchange for new tokens.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
    
    /// <summary>
    /// Optional client identifier for validation.
    /// </summary>
    public string? ClientId { get; init; }
}