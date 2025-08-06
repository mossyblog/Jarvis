using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing the binding between UIStudio components and ECS components.
/// Stores field mappings, data source configurations, and component positioning.
/// Table: ui_studio_component_binding (automatic snake_case mapping)
/// </summary>
public record UIStudioComponentBinding : IComponent, IVersionedComponent
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
    /// Page slug this binding is associated with (for querying purposes only).
    /// Maps to page_slug in database.
    /// </summary>
    public string PageSlug { get; init; } = string.Empty;

    /// <summary>
    /// Type of UI component (e.g., "table", "card", "chart", "form").
    /// Maps to component_type in database.
    /// </summary>
    public string ComponentType { get; init; } = string.Empty;

    /// <summary>
    /// Unique identifier for this component instance within the page.
    /// Maps to component_instance_id in database.
    /// </summary>
    public string ComponentInstanceId { get; init; } = string.Empty;

    /// <summary>
    /// Component ID for easier identification.
    /// Maps to component_id in database.
    /// </summary>
    public string ComponentId { get; init; } = string.Empty;

    /// <summary>
    /// Name of the ECS component type this UI component is bound to.
    /// Maps to bound_component_type in database.
    /// </summary>
    public string BoundComponentType { get; init; } = string.Empty;

    /// <summary>
    /// Field mappings between UI component and ECS component stored as JSON.
    /// Maps UI field names to ECS component property paths.
    /// Maps to field_mappings in database.
    /// </summary>
    public Dictionary<string, object>? FieldMappings { get; init; }

    /// <summary>
    /// Data source configuration stored as JSON.
    /// Includes filtering, sorting, pagination settings.
    /// Maps to data_source_config in database.
    /// </summary>
    public Dictionary<string, object>? DataSourceConfig { get; init; }

    /// <summary>
    /// Type of data source: "api", "static", "computed", "database".
    /// Maps to data_source_type in database.
    /// </summary>
    public string DataSourceType { get; init; } = "api";

    /// <summary>
    /// Component positioning in the layout stored as JSON.
    /// Includes grid coordinates, size, z-index, etc.
    /// Maps to position_config in database.
    /// </summary>
    public Dictionary<string, object>? PositionConfig { get; init; }

    /// <summary>
    /// Grid position configuration for easier access.
    /// Maps to grid_position in database.
    /// </summary>
    public Dictionary<string, object>? GridPosition { get; init; }

    /// <summary>
    /// Component-specific styling configuration stored as JSON.
    /// Includes colors, fonts, spacing, etc.
    /// Maps to style_config in database.
    /// </summary>
    public Dictionary<string, object>? StyleConfig { get; init; }

    /// <summary>
    /// Component behavior configuration stored as JSON.
    /// Includes event handlers, interactions, validations.
    /// Maps to behavior_config in database.
    /// </summary>
    public Dictionary<string, object>? BehaviorConfig { get; init; }

    /// <summary>
    /// Whether this component is visible to users.
    /// Maps to is_visible in database.
    /// </summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// Whether this component is enabled for interaction.
    /// Maps to is_enabled in database.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Entity IDs that have permission to view this component.
    /// Stored as comma-separated string for simple cases.
    /// Maps to view_permissions in database.
    /// </summary>
    public string? ViewPermissions { get; init; }

    /// <summary>
    /// Entity IDs that have permission to edit this component.
    /// Stored as comma-separated string for simple cases.
    /// Maps to edit_permissions in database.
    /// </summary>
    public string? EditPermissions { get; init; }

    /// <summary>
    /// Sort order for component rendering within the page.
    /// Maps to sort_order in database.
    /// </summary>
    public int SortOrder { get; init; } = 0;

    /// <summary>
    /// Entity ID of the user who created this binding.
    /// Maps to created_by_entity_id in database.
    /// </summary>
    public Guid CreatedByEntityId { get; init; }

    /// <summary>
    /// Entity ID of the user who last modified this binding.
    /// Maps to modified_by_entity_id in database.
    /// </summary>
    public Guid? ModifiedByEntityId { get; init; }

    /// <summary>
    /// When the binding was created.
    /// Maps to created_at in database.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the binding was last updated.
    /// Required by IComponent interface.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version number for change tracking.
    /// Required by IVersionedComponent interface.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Component configuration schema version for migration support.
    /// Maps to schema_version in database.
    /// </summary>
    public string SchemaVersion { get; init; } = "1.0";
}