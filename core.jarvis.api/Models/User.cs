using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing user account and authentication data.
/// </summary>
public record User : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Hashed password (from users table) - for persistent storage.
    /// </summary>
    public string PasswordHash { get; init; } = string.Empty;

    /// <summary>
    /// Plain password - for authentication requests only (not persisted).
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Optional two-factor authentication code.
    /// </summary>
    public string? TwoFactorCode { get; init; }

    /// <summary>
    /// Authentication method type (password, otp, pin, etc.).
    /// </summary>
    public string AuthMethod { get; init; } = "password";

    /// <summary>
    /// Client identifier for tracking sessions.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// When the user was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// IP address for authentication request tracking.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// User agent for authentication request tracking.
    /// </summary>
    public string? UserAgent { get; init; }
}