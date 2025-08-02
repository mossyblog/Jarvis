using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component representing a UIStudio page definition.
/// Stores the main page configuration including metadata, type, and layout references.
/// Table: ui_studio_page (automatic snake_case mapping)
/// </summary>
public record UIStudioPage : IComponent, IVersionedComponent
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
    /// Name of the page for display purposes.
    /// Maps to page_name in database.
    /// </summary>
    public string PageName { get; init; } = string.Empty;

    /// <summary>
    /// URL slug for the page routing.
    /// Maps to page_slug in database.
    /// </summary>
    public string PageSlug { get; init; } = string.Empty;

    /// <summary>
    /// Type of page: "dynamic", "fixed", or "hybrid".
    /// Maps to page_type in database.
    /// </summary>
    public string PageType { get; init; } = "dynamic";

    /// <summary>
    /// Optional description of the page.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Entity ID of the layout component used by this page.
    /// Maps to layout_entity_id in database.
    /// </summary>
    public Guid? LayoutEntityId { get; init; }

    /// <summary>
    /// Whether the page is published and accessible to users.
    /// Maps to is_published in database.
    /// </summary>
    public bool IsPublished { get; init; } = false;

    /// <summary>
    /// Whether the page is marked as the default/home page.
    /// Maps to is_default in database.
    /// </summary>
    public bool IsDefault { get; init; } = false;

    /// <summary>
    /// Page-level metadata stored as JSON.
    /// Can include SEO settings, custom properties, etc.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Entity ID of the user who created this page.
    /// Maps to created_by_entity_id in database.
    /// </summary>
    public Guid CreatedByEntityId { get; init; }

    /// <summary>
    /// Entity ID of the user who last modified this page.
    /// Maps to modified_by_entity_id in database.
    /// </summary>
    public Guid? ModifiedByEntityId { get; init; }

    /// <summary>
    /// When the page was created.
    /// Maps to created_at in database.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the page was last updated.
    /// Required by IComponent interface.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version number for change tracking.
    /// Required by IVersionedComponent interface.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Sort order for page listing.
    /// Maps to sort_order in database.
    /// </summary>
    public int SortOrder { get; init; } = 0;

    /// <summary>
    /// Tags associated with this page for categorization.
    /// Stored as comma-separated string.
    /// </summary>
    public string? Tags { get; init; }
}