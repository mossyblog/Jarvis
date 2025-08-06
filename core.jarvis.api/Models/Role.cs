using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing a role with its associated permissions.
/// </summary>
public record Role : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Name of the role (e.g., "superuser", "dev", "default").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable description of the role.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Array of permission entity IDs assigned to this role.
    /// </summary>
    public string[] PermissionIds { get; init; } = Array.Empty<string>();
}