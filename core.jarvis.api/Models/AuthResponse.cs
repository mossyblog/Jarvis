using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing an authentication response with tokens.
/// </summary>
public record AuthResponse : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// JWT access token for API authorization.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;
    
    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
    
    /// <summary>
    /// When the access token expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }
    
    /// <summary>
    /// The authenticated user's ID.
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// Token type (typically "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";
    
    /// <summary>
    /// Session ID for tracking this authentication session.
    /// </summary>
    public Guid SessionId { get; init; } = Guid.NewGuid();
}