using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing reusable templates for UIStudio pages and layouts.
/// Enables template sharing, categorization, and quick page creation.
/// Table: ui_studio_template (automatic snake_case mapping)
/// </summary>
public record UIStudioTemplate : IComponent, IVersionedComponent
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
    /// Name of the template for display purposes.
    /// Maps to template_name in database.
    /// </summary>
    public string TemplateName { get; init; } = string.Empty;

    /// <summary>
    /// Type of template: "page", "layout", "component_set".
    /// Maps to template_type in database.
    /// </summary>
    public string TemplateType { get; init; } = "page";

    /// <summary>
    /// Category for organizing templates.
    /// Maps to category in database.
    /// </summary>
    public string Category { get; init; } = "general";

    /// <summary>
    /// Subcategory for more specific organization.
    /// Maps to subcategory in database.
    /// </summary>
    public string? Subcategory { get; init; }

    /// <summary>
    /// Description of what this template provides.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Template configuration data stored as JSON.
    /// Contains the complete template structure and settings.
    /// Maps to template_data in database.
    /// </summary>
    public Dictionary<string, object>? TemplateData { get; init; }

    /// <summary>
    /// Preview/thumbnail image URL for the template.
    /// Maps to preview_image_url in database.
    /// </summary>
    public string? PreviewImageUrl { get; init; }

    /// <summary>
    /// Whether this template is available to all users.
    /// Maps to is_public in database.
    /// </summary>
    public bool IsPublic { get; init; } = false;

    /// <summary>
    /// Whether this template is featured/promoted.
    /// Maps to is_featured in database.
    /// </summary>
    public bool IsFeatured { get; init; } = false;

    /// <summary>
    /// Whether this template is officially provided by the system.
    /// Maps to is_system_template in database.
    /// </summary>
    public bool IsSystemTemplate { get; init; } = false;

    /// <summary>
    /// Number of times this template has been used.
    /// Maps to usage_count in database.
    /// </summary>
    public int UsageCount { get; init; } = 0;

    /// <summary>
    /// Average rating given by users (1-5 scale).
    /// Maps to average_rating in database.
    /// </summary>
    public decimal? AverageRating { get; init; }

    /// <summary>
    /// Number of ratings received.
    /// Maps to rating_count in database.
    /// </summary>
    public int RatingCount { get; init; } = 0;

    /// <summary>
    /// Tags for template searchability.
    /// Stored as comma-separated string.
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// Required component types for this template to work.
    /// Stored as comma-separated string.
    /// Maps to required_components in database.
    /// </summary>
    public string? RequiredComponents { get; init; }

    /// <summary>
    /// Minimum schema version required for this template.
    /// Maps to min_schema_version in database.
    /// </summary>
    public string? MinSchemaVersion { get; init; }

    /// <summary>
    /// Entity ID of the user who created this template.
    /// Maps to created_by_entity_id in database.
    /// </summary>
    public Guid CreatedByEntityId { get; init; }

    /// <summary>
    /// Entity ID of the user who last modified this template.
    /// Maps to modified_by_entity_id in database.
    /// </summary>
    public Guid? ModifiedByEntityId { get; init; }

    /// <summary>
    /// When the template was created.
    /// Maps to created_at in database.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the template was last updated.
    /// Required by IComponent interface.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version number for change tracking.
    /// Required by IVersionedComponent interface.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Sort order for template listing.
    /// Maps to sort_order in database.
    /// </summary>
    public int SortOrder { get; init; } = 0;

    /// <summary>
    /// Whether this template is currently active/available.
    /// Maps to is_active in database.
    /// </summary>
    public bool IsActive { get; init; } = true;
}