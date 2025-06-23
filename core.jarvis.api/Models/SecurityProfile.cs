using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing a user's security profile information and role assignments.
/// </summary>
public record SecurityProfile : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Display name for the user.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Avatar URL or identifier.
    /// </summary>
    public string? Avatar { get; init; }

    /// <summary>
    /// Array of role entity IDs assigned to this user.
    /// </summary>
    public string[] RoleIds { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Array of permission entity IDs this user has.
    /// This is denormalized from roles for fast permission checks.
    /// </summary>
    public string[] PermissionIds { get; init; } = Array.Empty<string>();

    /// <summary>
    /// User preferences as key-value pairs.
    /// </summary>
    public Dictionary<string, object> Preferences { get; init; } = new();
}