using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing a UIStudio layout configuration.
/// Stores grid layout settings, responsive breakpoints, and container definitions.
/// Table: ui_studio_layout (automatic snake_case mapping)
/// </summary>
public record UIStudioLayout : IComponent, IVersionedComponent
{
    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The entity this component belongs to.
    /// Required by IComponent interface.
    /// </summary>
    public Guid OwnerEntityId { get; set; }

    /// <summary>
    /// Name of the layout for display purposes.
    /// Maps to layout_name in database.
    /// </summary>
    public string LayoutName { get; init; } = string.Empty;

    /// <summary>
    /// Type of layout: "bento", "grid", "flex", "absolute".
    /// Maps to layout_type in database.
    /// </summary>
    public string LayoutType { get; init; } = "bento";

    /// <summary>
    /// Grid configuration stored as JSON.
    /// Includes columns, rows, gap settings, etc.
    /// Maps to grid_config in database.
    /// </summary>
    public Dictionary<string, object>? GridConfig { get; init; }

    /// <summary>
    /// Responsive breakpoint configurations stored as JSON.
    /// Defines how layout adapts to different screen sizes.
    /// Maps to responsive_breakpoints in database.
    /// </summary>
    public Dictionary<string, object>? ResponsiveBreakpoints { get; init; }

    /// <summary>
    /// Container constraints and settings stored as JSON.
    /// Includes max-width, padding, margins, etc.
    /// Maps to container_settings in database.
    /// </summary>
    public Dictionary<string, object>? ContainerSettings { get; init; }

    /// <summary>
    /// CSS classes to apply to the layout container.
    /// Maps to css_classes in database.
    /// </summary>
    public string? CssClasses { get; init; }

    /// <summary>
    /// Custom CSS styles for the layout.
    /// Maps to custom_styles in database.
    /// </summary>
    public string? CustomStyles { get; init; }

    /// <summary>
    /// Whether this layout is available as a template.
    /// Maps to is_template in database.
    /// </summary>
    public bool IsTemplate { get; init; } = false;

    /// <summary>
    /// Template category if this is a template layout.
    /// Maps to template_category in database.
    /// </summary>
    public string? TemplateCategory { get; init; }

    /// <summary>
    /// Preview thumbnail URL for the layout.
    /// Maps to thumbnail_url in database.
    /// </summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>
    /// Entity ID of the user who created this layout.
    /// Maps to created_by_entity_id in database.
    /// </summary>
    public Guid CreatedByEntityId { get; init; }

    /// <summary>
    /// Entity ID of the user who last modified this layout.
    /// Maps to modified_by_entity_id in database.
    /// </summary>
    public Guid? ModifiedByEntityId { get; init; }

    /// <summary>
    /// When the layout was created.
    /// Maps to created_at in database.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the layout was last updated.
    /// Required by IComponent interface.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version number for change tracking.
    /// Required by IVersionedComponent interface.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Layout configuration schema version for migration support.
    /// Maps to schema_version in database.
    /// </summary>
    public string SchemaVersion { get; init; } = "1.0";
}