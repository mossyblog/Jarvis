using core.jarvis.Data;

namespace core.jarvis.tests.Examples.Blog;

/// <summary>
/// Example component representing a blog entity.
/// Demonstrates blog configuration and metadata management.
/// Table: blog (automatic snake_case mapping)
/// </summary>
public record BlogComponent : IComponent, IVersionedComponent
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
    /// Name of the blog.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the blog.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Theme/category of the blog content.
    /// </summary>
    public string Theme { get; set; } = "general";

    /// <summary>
    /// Writing tone for the blog.
    /// </summary>
    public string Tone { get; set; } = "professional";

    /// <summary>
    /// Target audience for the blog.
    /// Maps to target_audience in database.
    /// </summary>
    public string TargetAudience { get; set; } = "general";

    /// <summary>
    /// Additional settings stored as JSON.
    /// </summary>
    public Dictionary<string, object>? Settings { get; set; }

    /// <summary>
    /// When the blog was created.
    /// Maps to created_at in database.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the blog was last updated.
    /// Maps to updated_at in database.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public int? Version { get; set; }
} 