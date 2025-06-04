namespace core.jarvis.tests.Examples.Blog;

/// <summary>
/// Request model for blog post generation.
/// </summary>
public class BlogPostGenerationRequest
{
    public string? Topic { get; set; }
    public string? Keywords { get; set; }
    public int? TargetWordCount { get; set; } = 800;
    public string? Style { get; set; }
    public string[]? References { get; set; }
}

/// <summary>
/// Generated content result.
/// </summary>
public class GeneratedContent
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string[]? Tags { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Context for content generation.
/// </summary>
public class ContentGenerationContext
{
    public string BlogName { get; set; } = string.Empty;
    public string? BlogDescription { get; set; }
    public string Theme { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public int ExistingPostsCount { get; set; }
    public List<string> RecentTopics { get; set; } = new();
    public string? RequestedTopic { get; set; }
    public string? RequestedKeywords { get; set; }
    public int TargetWordCount { get; set; }
    public string Style { get; set; } = string.Empty;
}

/// <summary>
/// Service for generating blog content using AI.
/// </summary>
public interface IBlogContentGenerator
{
    Task<GeneratedContent> GenerateContent(
        BlogComponent blog,
        IList<BlogPostComponent> existingPosts,
        BlogPostGenerationRequest request);
}

/// <summary>
/// Exception thrown when content generation fails.
/// </summary>
public class ContentGenerationException : Exception
{
    public ContentGenerationException(string message) : base(message) { }
    public ContentGenerationException(string message, Exception innerException) : base(message, innerException) { }
} 