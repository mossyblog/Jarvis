using core.jarvis.Data;
using core.jarvis.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Examples.Blog;

/// <summary>
/// Comprehensive example demonstrating the blog post generation functionality.
/// Shows how to use the _dataContext.BlogHandler(blogId).GeneratePost() API.
/// </summary>
public class BlogExampleUsage
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<BlogExampleUsage> _logger;

    public BlogExampleUsage(IDataContext dataContext, ILogger<BlogExampleUsage> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    /// <summary>
    /// Demonstrates the basic blog post generation workflow.
    /// </summary>
    public async Task<BlogPostComponent> BasicGenerationExample()
    {
        // Step 1: Create a blog entity
        var blogEntityId = Guid.NewGuid();
        var blog = await _dataContext.For<BlogComponentHandler>(blogEntityId)
            .CreateBlog("Tech Insights Blog", "A blog about emerging technology trends");


        // Step 2: Use the fluent API to generate a post
        var generatedPost = await _dataContext.BlogHandler(blogEntityId).GeneratePost();


        return generatedPost;
    }

    /// <summary>
    /// Demonstrates advanced post generation with custom parameters.
    /// </summary>
    public async Task<BlogPostComponent> AdvancedGenerationExample()
    {
        // Step 1: Set up blog with custom settings
        var blogEntityId = Guid.NewGuid();
        var blog = await _dataContext.For<BlogComponentHandler>(blogEntityId)
            .CreateBlog("Developer Insights", "Advanced software development topics");

        await _dataContext.For<BlogComponentHandler>(blogEntityId)
            .UpdateSettings("technology", "technical", "software developers");

        // Step 2: Generate post with specific requirements
        var request = new BlogPostGenerationRequest
        {
            Topic = "Microservices Architecture Best Practices",
            Keywords = "microservices, architecture, scalability, containers",
            TargetWordCount = 1200,
            Style = "technical but accessible"
        };

        var post = await _dataContext.BlogHandler(blogEntityId).GeneratePost(request);

        _logger.LogInformation("Generated advanced post: {Title}", post.Title);

        // Step 3: Publish the post if quality is satisfactory
        if (post.WordCount > 800)
        {
            await _dataContext.BlogHandler(blogEntityId).PublishPost(post.Id);
            _logger.LogInformation("Published post: {Title}", post.Title);
        }

        return post;
    }

    /// <summary>
    /// Demonstrates batch generation of multiple posts.
    /// </summary>
    public async Task<IList<BlogPostComponent>> BatchGenerationExample()
    {
        // Step 1: Set up blog
        var blogEntityId = Guid.NewGuid();
        await _dataContext.For<BlogComponentHandler>(blogEntityId)
            .CreateBlog("Weekly Tech Updates", "Regular updates on technology trends");

        // Step 2: Define multiple generation requests
        var requests = new List<BlogPostGenerationRequest>
        {
            new() { Topic = "AI in Healthcare", TargetWordCount = 800 },
            new() { Topic = "Future of Remote Work", TargetWordCount = 900 },
            new() { Topic = "Sustainable Technology", TargetWordCount = 750 },
            new() { Topic = "Cybersecurity Trends", TargetWordCount = 1000 }
        };

        // Step 3: Generate posts in batch
        var posts = await _dataContext.BlogHandler(blogEntityId).GeneratePosts(requests);

        _logger.LogInformation("Generated {PostCount} posts in batch", posts.Count);

        return posts;
    }

    /// <summary>
    /// Demonstrates complete blog management workflow.
    /// </summary>
    public async Task<BlogSummary> CompleteWorkflowExample()
    {
        // Step 1: Create and configure blog
        var blogEntityId = Guid.NewGuid();
        var blogHandler = _dataContext.BlogHandler(blogEntityId);

        var blog = await _dataContext.For<BlogComponentHandler>(blogEntityId)
            .CreateBlog("Complete Example Blog", "Demonstrating full workflow");

        await blogHandler.UpdateBlogSettings("business", "professional", "business leaders");

        // Step 2: Generate initial posts
        var initialPosts = new List<BlogPostGenerationRequest>
        {
            new() { Topic = "Digital Transformation", TargetWordCount = 1000 },
            new() { Topic = "Leadership in Tech", TargetWordCount = 800 }
        };

        var posts = await blogHandler.GeneratePosts(initialPosts);

        // Step 3: Publish quality posts
        foreach (var post in posts.Where(p => p.WordCount > 500))
        {
            await blogHandler.PublishPost(post.Id);
        }

        // Step 4: Generate additional content
        var additionalPost = await blogHandler.GeneratePost(new BlogPostGenerationRequest
        {
            Topic = "Innovation Strategies",
            Keywords = "innovation, strategy, technology, business",
            TargetWordCount = 900
        });

        // Step 5: Get comprehensive summary
        var summary = await blogHandler.GetBlogSummary();

        _logger.LogInformation("Blog summary: {TotalPosts} total, {PublishedPosts} published", 
            summary.TotalPosts, summary.PublishedPosts);

        return summary;
    }

    /// <summary>
    /// Demonstrates error handling and validation.
    /// </summary>
    public async Task ErrorHandlingExample()
    {
        try
        {
            var blogEntityId = Guid.NewGuid();
            
            // This will work - create blog first
            await _dataContext.For<BlogComponentHandler>(blogEntityId)
                .CreateBlog("Error Example Blog");

            // This will work - generate post
            var post = await _dataContext.BlogHandler(blogEntityId).GeneratePost();

            // This will fail - try to publish non-existent post
            await _dataContext.BlogHandler(blogEntityId).PublishPost(Guid.NewGuid());
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning("Expected error - entity not found: {Message}", ex.Message);
        }
        catch (BlogOperationException ex)
        {
            _logger.LogError(ex, "Blog operation failed: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in blog operations");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates content management operations.
    /// </summary>
    public async Task ContentManagementExample()
    {
        var blogEntityId = Guid.NewGuid();
        var blogHandler = _dataContext.BlogHandler(blogEntityId);

        // Create blog
        await _dataContext.For<BlogComponentHandler>(blogEntityId)
            .CreateBlog("Content Management Example");

        // Generate multiple posts
        var posts = await blogHandler.GeneratePosts(new[]
        {
            new BlogPostGenerationRequest { Topic = "Content Strategy" },
            new BlogPostGenerationRequest { Topic = "SEO Best Practices" },
            new BlogPostGenerationRequest { Topic = "Social Media Integration" }
        });

        // Publish some posts
        foreach (var post in posts.Take(2))
        {
            await blogHandler.PublishPost(post.Id);
        }

        // Archive old content
        if (posts.Count > 2)
        {
            await blogHandler.ArchivePost(posts.Last().Id);
        }

        // Get all posts to review status
        var allPosts = await blogHandler.GetAllPosts();
        
        _logger.LogInformation("Content status: {Published} published, {Draft} draft, {Archived} archived",
            allPosts.Count(p => p.IsPublished == true),
            allPosts.Count(p => p.Status == "draft"),
            allPosts.Count(p => p.Status == "archived"));

        // Get final summary
        var summary = await blogHandler.GetBlogSummary();
        _logger.LogInformation("Final blog state: {Summary}", System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
}

/// <summary>
/// Example of how to configure dependency injection for blog services.
/// </summary>
public static class BlogServiceConfiguration
{
    public static IServiceCollection AddBlogServices(this IServiceCollection services)
    {
        // Register component handlers
        services.AddScoped<BlogComponentHandler>();
        services.AddScoped<BlogPostComponentHandler>();
        
        // Register business handler
        services.AddScoped<BlogHandler>();
        
        // Register example usage class
        services.AddScoped<BlogExampleUsage>();

        return services;
    }
} 