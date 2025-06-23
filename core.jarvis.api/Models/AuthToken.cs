using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing authentication tokens and session data.
/// </summary>
public record AuthToken : IComponent, IVersionedComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? Version { get; set; }

    /// <summary>
    /// JWT access token for API authorization.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Hashed refresh token for secure storage.
    /// </summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    /// <summary>
    /// When the access token expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// When the refresh token expires.
    /// </summary>
    public DateTime RefreshExpiresAt { get; init; }

    /// <summary>
    /// Token type (typically "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Session ID for tracking this authentication session.
    /// </summary>
    public Guid SessionId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Client identifier for tracking sessions.
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

    /// <summary>
    /// When this token was issued.
    /// </summary>
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;
}