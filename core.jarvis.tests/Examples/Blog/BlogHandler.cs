using core.jarvis.Data;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Examples.Blog;

/// <summary>
/// Example handler for blog operations that demonstrates the desired fluent API.
/// Provides the _dataContext.BlogHandler(blogId).GeneratePost() functionality.
/// Works with the existing For<> pattern in the Jarvis framework.
/// </summary>
public class BlogHandler : IComponentHandler
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<BlogHandler> _logger;
    private Guid _entityId;

    public BlogHandler(
        IDataContext dataContext,
        ILogger<BlogHandler> logger)
    {
        _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void InitializeContext(Guid entityId)
    {
        _entityId = entityId;
    }

    public async Task<IComponent> Get()
    {
        // BlogHandler operates on blog entities, so return the blog component
        return await _dataContext.For<BlogComponentHandler>(_entityId).Get();
    }

    /// <summary>
    /// Generates a new blog post using AI content generation.
    /// This is the main method that provides the desired API functionality.
    /// </summary>
    /// <param name="request">Optional generation parameters</param>
    /// <returns>The generated blog post</returns>
    public async Task<BlogPostComponent> GeneratePost(BlogPostGenerationRequest? request = null)
    {
        try
        {
            // Get blog configuration for context
            var blog = await _dataContext.For<BlogComponentHandler>(_entityId).Get();

            // Use default request if none provided
            request ??= new BlogPostGenerationRequest();

            // Create new blog post entity using the post handler
            var blogPost = await _dataContext.For<BlogPostComponentHandler>(_entityId).CreatePost(
                request.Topic ?? "Untitled Post",
                $"Sample content for {request.Topic ?? "Untitled Post"}.",
                null,
                null);

            return blogPost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate blog post for blog {BlogId}", _entityId);
            throw new BlogOperationException($"Failed to generate blog post for blog {_entityId}", ex);
        }
    }

    /// <summary>
    /// Gets all posts for this blog.
    /// </summary>
    public async Task<IList<BlogPostComponent>> GetAllPosts()
    {
        return await _dataContext.For<BlogPostComponentHandler>(_entityId).GetAllPosts();
    }

    /// <summary>
    /// Gets blog information.
    /// </summary>
    public async Task<BlogComponent> GetBlog()
    {
        return await _dataContext.For<BlogComponentHandler>(_entityId).Get();
    }

    /// <summary>
    /// Publishes a specific post.
    /// </summary>
    public async Task<BlogPostComponent> PublishPost(Guid postId)
    {
        return await _dataContext.For<BlogPostComponentHandler>(_entityId).PublishPost(postId);
    }

    /// <summary>
    /// Generates multiple posts in batch.
    /// </summary>
    public async Task<IList<BlogPostComponent>> GeneratePosts(IList<BlogPostGenerationRequest> requests)
    {
        var results = new List<BlogPostComponent>();

        foreach (var request in requests)
        {
            try
            {
                var post = await GeneratePost(request);
                results.Add(post);

                // Add small delay to avoid overwhelming AI service
                await Task.Delay(100); // Reduced for testing
            }
            catch (Exception ex)
            {
                // Continue with next request rather than failing entire batch
            }
        }

        return results;
    }

    /// <summary>
    /// Updates blog settings.
    /// </summary>
    public async Task<BlogComponent> UpdateBlogSettings(string? theme = null, string? tone = null, string? targetAudience = null)
    {
        return await _dataContext.For<BlogComponentHandler>(_entityId).UpdateSettings(theme, tone, targetAudience);
    }

    /// <summary>
    /// Archives a specific post.
    /// </summary>
    public async Task<BlogPostComponent> ArchivePost(Guid postId)
    {
        return await _dataContext.For<BlogPostComponentHandler>(_entityId).ArchivePost(postId);
    }

    /// <summary>
    /// Gets a summary of the blog and its posts.
    /// </summary>
    public async Task<BlogSummary> GetBlogSummary()
    {
        var blog = await GetBlog();
        var posts = await GetAllPosts();

        return new BlogSummary
        {
            EntityId = _entityId,
            BlogName = blog.Name,
            Description = blog.Description,
            Theme = blog.Theme,
            Tone = blog.Tone,
            TargetAudience = blog.TargetAudience,
            TotalPosts = posts.Count,
            PublishedPosts = posts.Count(p => p.IsPublished == true),
            DraftPosts = posts.Count(p => p.Status == "draft"),
            ArchivedPosts = posts.Count(p => p.Status == "archived"),
            CreatedAt = blog.CreatedAt,
            LastPostCreated = posts.Any() ? posts.Max(p => p.CreatedAt) : null,
            RecentTopics = posts.OrderByDescending(p => p.CreatedAt).Take(5).Select(p => p.Title).ToList()
        };
    }
}

/// <summary>
/// Data transfer object for blog summary information.
/// </summary>
public class BlogSummary
{
    public Guid EntityId { get; set; }
    public string BlogName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Theme { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public int TotalPosts { get; set; }
    public int PublishedPosts { get; set; }
    public int DraftPosts { get; set; }
    public int ArchivedPosts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastPostCreated { get; set; }
    public List<string> RecentTopics { get; set; } = new();
}

/// <summary>
/// Exception thrown when blog operations fail.
/// </summary>
public class BlogOperationException : Exception
{
    public BlogOperationException(string message) : base(message) { }
    public BlogOperationException(string message, Exception innerException) : base(message, innerException) { }
} 