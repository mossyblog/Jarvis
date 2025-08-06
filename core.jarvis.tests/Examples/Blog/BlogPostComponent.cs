using core.jarvis.Data;

namespace core.jarvis.tests.Examples.Blog;

/// <summary>
/// Example component representing a blog post.
/// Demonstrates content management and post lifecycle.
/// Table: blog_post (automatic snake_case mapping)
/// </summary>
public record BlogPostComponent : IComponent, IVersionedComponent
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
    /// Title of the blog post.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Full content of the blog post.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Brief excerpt of the post.
    /// </summary>
    public string? Excerpt { get; set; }

    /// <summary>
    /// Current status of the post (draft, published, archived).
    /// </summary>
    public string Status { get; set; } = "draft";

    /// <summary>
    /// Tags associated with the post.
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// When the post was created.
    /// Maps to created_at in database.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the post was published (null if not published).
    /// Maps to published_at in database.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Word count of the content.
    /// Maps to word_count in database.
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Whether the post is published.
    /// Maps to is_published in database.
    /// </summary>
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// When the post was archived (null if not archived).
    /// Maps to archived_at in database.
    /// </summary>
    public DateTime? ArchivedAt { get; set; }
    
    /// <summary>
    /// When the post was last updated.
    /// Maps to updated_at in database.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public int? Version { get; set; }
} 