using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing a permission for a specific resource and actions.
/// </summary>
public record Permission : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Resource this permission applies to (e.g., "tables", "users", "api").
    /// </summary>
    public string Resource { get; init; } = string.Empty;

    /// <summary>
    /// Actions allowed on the resource (e.g., ["read", "write", "delete"]).
    /// </summary>
    public string[] Actions { get; init; } = Array.Empty<string>();
}