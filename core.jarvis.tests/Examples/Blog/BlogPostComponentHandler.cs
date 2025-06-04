using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.Validation;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Examples.Blog;

/// <summary>
/// Component handler for BlogPost entities.
/// Demonstrates blog post management operations.
/// </summary>
public class BlogPostComponentHandler : ComponentHandler<BlogPostComponent>
{
    public BlogPostComponentHandler(IDataContext dataContext, ILogger<BlogPostComponentHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Gets all blog posts for the entity.
    /// </summary>
    public async Task<IList<BlogPostComponent>> GetAllPosts()
    {
        try
        {
            Logger.LogInformation("Getting all posts for entity {EntityId}", OwnerEntityId);
            
            // Get all entities that have BlogPostComponent
            var entityComponents = await DataContext.Query()
                .WithAll<BlogPostComponent>(p => true)  // Get all blog posts
                .ToEntityComponents();

            Logger.LogInformation("Found {Count} entity components", entityComponents.Count);

            var posts = new List<BlogPostComponent>();
            foreach (var kvp in entityComponents)
            {
                var post = kvp.Value.Get<BlogPostComponent>();
                if (post != null)
                {
                    Logger.LogInformation("Found post {PostId} with OwnerEntityId {OwnerEntityId}", post.Id, post.OwnerEntityId);
                    if (post.OwnerEntityId == OwnerEntityId)
                        posts.Add(post);
                }
            }
            
            Logger.LogInformation("Returning {Count} posts for entity {EntityId}", posts.Count, OwnerEntityId);
            return posts;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get posts for entity {EntityId}", OwnerEntityId);
            throw;
        }
    }

    /// <summary>
    /// Creates a new blog post.
    /// </summary>
    public async Task<BlogPostComponent> CreatePost(
        string title, 
        string content, 
        string? excerpt = null,
        string[]? tags = null)
    {
        Guard.AgainstEmpty(title, nameof(title));
        Guard.AgainstEmpty(content, nameof(content));

        var post = new BlogPostComponent
        {
            OwnerEntityId = OwnerEntityId,
            Title = title,
            Content = content,
            Excerpt = excerpt ?? (content.Length > 200 ? content.Substring(0, 200) + "..." : content),
            Tags = tags,
            Status = "draft",
            IsPublished = false,
            WordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            CreatedAt = DateTime.UtcNow
        };

        var success = await DataContext.TryCommit(post);
        if (!success)
        {
            throw new ConcurrencyException("Blog post creation failed due to concurrent modification");
        }

        return post;
    }

    /// <summary>
    /// Publishes a draft post.
    /// </summary>
    public async Task<BlogPostComponent> PublishPost(Guid postId)
    {
        var posts = await GetAllPosts();
        var post = posts.FirstOrDefault(p => p.Id == postId);

        if (post == null)
            throw new EntityNotFoundException(postId, nameof(BlogPostComponent));

        Ensure(post.Status == "draft", "Only draft posts can be published");

        post.Status = "published";
        post.PublishedAt = DateTime.UtcNow;
        post.IsPublished = true;
        // Explicitly set all again to force ORM dirty tracking
        post.Status = post.Status;
        post.PublishedAt = post.PublishedAt;
        post.IsPublished = post.IsPublished;

        var success = await DataContext.TryCommit(post);
        if (!success)
        {
            throw new ConcurrencyException("Blog post publish failed due to concurrent modification");
        }

        return post;
    }

    /// <summary>
    /// Updates post content.
    /// </summary>
    public async Task<BlogPostComponent> UpdateContent(Guid postId, string title, string content, string? excerpt = null)
    {
        Guard.AgainstEmpty(title, nameof(title));
        Guard.AgainstEmpty(content, nameof(content));

        var posts = await GetAllPosts();
        var post = posts.FirstOrDefault(p => p.Id == postId);

        if (post == null)
            throw new EntityNotFoundException(postId, nameof(BlogPostComponent));

        Ensure(post.Status != "published", "Cannot modify published posts");

        post.Title = title;
        post.Content = content;
        post.Excerpt = excerpt ?? (content.Length > 200 ? content.Substring(0, 200) + "..." : content);

        var success = await DataContext.TryCommit(post);
        if (!success)
        {
            throw new ConcurrencyException("Blog post update failed due to concurrent modification");
        }

        return post;
    }

    /// <summary>
    /// Archives a post.
    /// </summary>
    public async Task<BlogPostComponent> ArchivePost(Guid postId)
    {
        var posts = await GetAllPosts();
        var post = posts.FirstOrDefault(p => p.Id == postId);

        if (post == null)
            throw new EntityNotFoundException(postId, nameof(BlogPostComponent));

        post.Status = "archived";

        var success = await DataContext.TryCommit(post);
        if (!success)
        {
            throw new ConcurrencyException("Blog post archive failed due to concurrent modification");
        }

        return post;
    }
} 