using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing a stored security token/session.
/// </summary>
public record SecurityToken : IComponent, IVersionedComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? Version { get; set; }
    
    /// <summary>
    /// The user this token belongs to.
    /// </summary>
    public Guid UserId { get; init; }
    
    /// <summary>
    /// Session ID for tracking.
    /// </summary>
    public Guid SessionId { get; init; }
    
    /// <summary>
    /// Hashed refresh token for security.
    /// </summary>
    public string RefreshTokenHash { get; set; } = string.Empty;
    
    /// <summary>
    /// When this token was issued.
    /// </summary>
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the refresh token expires.
    /// </summary>
    public DateTime RefreshExpiresAt { get; init; }
    
    /// <summary>
    /// Client identifier that created this session.
    /// </summary>
    public string? ClientId { get; init; }
    
    /// <summary>
    /// IP address of the client.
    /// </summary>
    public string? IpAddress { get; init; }
    
    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; init; }
    
    /// <summary>
    /// Whether this token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }
    
    /// <summary>
    /// When the token was revoked (if applicable).
    /// </summary>
    public DateTime? RevokedAt { get; set; }
}