using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing account and authentication data.
/// </summary>
public record Account : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Account's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Hashed password (from account table) - for persistent storage.
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
    /// Whether the account is active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// When the account was created.
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