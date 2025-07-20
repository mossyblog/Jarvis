using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Navigation item component for menu structure.
/// </summary>
public record NavigationItem : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Unique identifier for the navigation item (menu ID).
    /// </summary>
    public string MenuId { get; init; } = string.Empty;
    
    /// <summary>
    /// Display label for the navigation item.
    /// </summary>
    public string Label { get; init; } = string.Empty;
    
    /// <summary>
    /// Icon name/class for the navigation item.
    /// </summary>
    public string Icon { get; init; } = string.Empty;
    
    /// <summary>
    /// URL/route for the navigation item.
    /// </summary>
    public string Href { get; init; } = string.Empty;
    
    /// <summary>
    /// Sort order for display.
    /// </summary>
    public int SortOrder { get; init; }
    
    /// <summary>
    /// Required permission entity ID to view this item.
    /// </summary>
    public Guid? RequiredPermissionId { get; init; }
    
    /// <summary>
    /// Whether this item is active/enabled.
    /// </summary>
    public bool IsActive { get; init; } = true;
    
    /// <summary>
    /// Badge configuration as JSON (if any).
    /// </summary>
    public string? BadgeConfig { get; init; }
}