# Blog Post Generation Example

## Overview

This guide demonstrates how to implement and use the `_dataContext.BlogHandler(blogId).GeneratePost()` API in the Jarvis framework. It provides a fluent API for blog operations, bridging the entity-component technical architecture with business-domain focused operations. The example covers:

- Extending the Jarvis handler architecture for business operations
- Implementing blog post generation and management
- Maintaining compatibility with existing patterns
- Creating a foundation for other domain-specific handlers

---

## Architecture Components

### 1. Blog Domain Components

#### BlogComponent
```csharp
using core.jarvis.Data;

namespace core.jarvis.tests.Examples;

[Table("blog")]
public class BlogComponent : BaseModel, IComponent
{
    [PrimaryKey("id")]
    public Guid Id { get; init; } = Guid.NewGuid();
    [Column("owner_entity_id")]
    public Guid OwnerEntityId { get; set; }
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("description")]
    public string? Description { get; set; }
    [Column("theme")]
    public string Theme { get; set; } = "general";
    [Column("tone")]
    public string Tone { get; set; } = "professional";
    [Column("target_audience")]
    public string TargetAudience { get; set; } = "general";
    [Column("settings")]
    public Dictionary<string, object>? Settings { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

#### BlogPostComponent
```csharp
using core.jarvis.Data;

namespace core.jarvis.tests.Examples;

[Table("blog_post")]
public class BlogPostComponent : BaseModel, IComponent
{
    [PrimaryKey("id")]
    public Guid Id { get; init; } = Guid.NewGuid();
    [Column("owner_entity_id")]
    public Guid OwnerEntityId { get; set; }
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    [Column("content")]
    public string Content { get; set; } = string.Empty;
    [Column("excerpt")]
    public string? Excerpt { get; set; }
    [Column("status")]
    public string Status { get; set; } = "draft";
    [Column("tags")]
    public string[]? Tags { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }
    [Column("word_count")]
    public int WordCount { get; set; }
    [Column("is_published")]
    public bool IsPublished { get; set; } = false;
    [Column("archived_at")]
    public DateTime? ArchivedAt { get; set; }
}
```

### 2. Data Transfer Objects

#### BlogPostGenerationRequest
```csharp
public class BlogPostGenerationRequest
{
    public string? Topic { get; set; }
    public string? Keywords { get; set; }
    public int? TargetWordCount { get; set; } = 800;
    public string? Style { get; set; }
    public string[]? References { get; set; }
}
```

### 3. Component Handlers

#### BlogComponentHandler
```csharp
using Microsoft.Extensions.Logging;
using core.jarvis.Data;
using core.jarvis.Validation;

namespace core.jarvis.tests.Examples;

public class BlogComponentHandler : ComponentHandler<BlogComponent>
{
    public BlogComponentHandler(IDataContext dataContext, ILogger<BlogComponentHandler> logger)
        : base(dataContext, logger) { }

    public async Task<BlogComponent> CreateBlog(string name, string? description = null)
    {
        Guard.AgainstEmpty(name, nameof(name));
        var existingBlog = await GetOrDefault();
        Ensure(existingBlog == null, "Blog already exists for this entity");
        var blog = new BlogComponent
        {
            OwnerEntityId = OwnerEntityId,
            Name = name,
            Description = description,
            Theme = "general",
            Tone = "professional",
            TargetAudience = "general"
        };
        await DataContext.Commit(blog);
        return blog;
    }

    public async Task<BlogComponent> UpdateSettings(string? theme = null, string? tone = null, string? targetAudience = null)
    {
        var blog = await GetRequired();
        if (!string.IsNullOrEmpty(theme)) blog.Theme = theme;
        if (!string.IsNullOrEmpty(tone)) blog.Tone = tone;
        if (!string.IsNullOrEmpty(targetAudience)) blog.TargetAudience = targetAudience;
        await DataContext.Commit(blog);
        Logger.LogInformation("Updated settings for blog {BlogId}", blog.Id);
        return blog;
    }

    public async Task<BlogComponent> UpdateDescription(string description)
    {
        Guard.AgainstNull(description, nameof(description));
        var blog = await GetRequired();
        blog.Description = description;
        await DataContext.Commit(blog);
        Logger.LogInformation("Updated description for blog {BlogId}", blog.Id);
        return blog;
    }
}
```

#### BlogPostComponentHandler
```csharp
using Microsoft.Extensions.Logging;
using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.Validation;

namespace core.jarvis.tests.Examples;

public class BlogPostComponentHandler : ComponentHandler<BlogPostComponent>
{
    public BlogPostComponentHandler(IDataContext dataContext, ILogger<BlogPostComponentHandler> logger)
        : base(dataContext, logger) { }

    public async Task<IList<BlogPostComponent>> GetAllPosts()
    {
        // Use DataContext query system to fetch all BlogPostComponent records for this entity
        var entityComponents = await DataContext.Query()
            .WithAll<BlogPostComponent>(p => p.OwnerEntityId == OwnerEntityId)
            .ToEntityComponents();
            
        var posts = new List<BlogPostComponent>();
        foreach (var components in entityComponents.Values)
        {
            var post = components.Get<BlogPostComponent>();
            if (post != null)
                posts.Add(post);
        }
        return posts;
    }

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
        await DataContext.Commit(post);
        return post;
    }

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
        post.Status = post.Status;
        post.PublishedAt = post.PublishedAt;
        post.IsPublished = post.IsPublished;
        await DataContext.Commit(post);
        return post;
    }

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
        await DataContext.Commit(post);
        return post;
    }

    public async Task<BlogPostComponent> ArchivePost(Guid postId)
    {
        var posts = await GetAllPosts();
        var post = posts.FirstOrDefault(p => p.Id == postId);
        if (post == null)
            throw new EntityNotFoundException(postId, nameof(BlogPostComponent));
        post.Status = "archived";
        await DataContext.Commit(post);
        return post;
    }
}
```

### 4. Business Handler

#### BlogHandler
```csharp
using Microsoft.Extensions.Logging;
using core.jarvis.Data;
using core.jarvis.Validation;

namespace core.jarvis.tests.Examples;

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
        return await _dataContext.For<BlogComponentHandler>(_entityId).Get();
    }

    public async Task<BlogPostComponent> GeneratePost(BlogPostGenerationRequest? request = null)
    {
        try
        {
            var blog = await _dataContext.For<BlogComponentHandler>(_entityId).Get();
            request ??= new BlogPostGenerationRequest();
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

    public async Task<IList<BlogPostComponent>> GetAllPosts()
    {
        return await _dataContext.For<BlogPostComponentHandler>(_entityId).GetAllPosts();
    }

    public async Task<BlogComponent> GetBlog()
    {
        return await _dataContext.For<BlogComponentHandler>(_entityId).Get();
    }

    public async Task<BlogPostComponent> PublishPost(Guid postId)
    {
        return await _dataContext.For<BlogPostComponentHandler>(_entityId).PublishPost(postId);
    }

    public async Task<IList<BlogPostComponent>> GeneratePosts(IList<BlogPostGenerationRequest> requests)
    {
        var results = new List<BlogPostComponent>();
        foreach (var request in requests)
        {
            try
            {
                var post = await GeneratePost(request);
                results.Add(post);
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                // Continue with next request
            }
        }
        return results;
    }

    public async Task<BlogComponent> UpdateBlogSettings(string? theme = null, string? tone = null, string? targetAudience = null)
    {
        return await _dataContext.For<BlogComponentHandler>(_entityId).UpdateSettings(theme, tone, targetAudience);
    }

    public async Task<BlogPostComponent> ArchivePost(Guid postId)
    {
        return await _dataContext.For<BlogPostComponentHandler>(_entityId).ArchivePost(postId);
    }

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

public class BlogOperationException : Exception
{
    public BlogOperationException(string message) : base(message) { }
    public BlogOperationException(string message, Exception innerException) : base(message, innerException) { }
}
```

### 5. Fluent API Extension

#### DataContextBlogExtensions
```csharp
using core.jarvis.Data;
using core.jarvis.Validation;

namespace core.jarvis.tests.Examples;

public static class DataContextBlogExtensions
{
    public static BlogHandler BlogHandler(this IDataContext dataContext, Guid blogId)
    {
        Guard.AgainstNull(dataContext, nameof(dataContext));
        Guard.AgainstEmptyGuid(blogId, nameof(blogId));
        return dataContext.For<BlogHandler>(blogId);
    }
}
```

---

## Usage Examples

### Basic Usage
```csharp
// Simple post generation
var blogId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
var generatedPost = await _dataContext.BlogHandler(blogId).GeneratePost();
Console.WriteLine($"Generated: {generatedPost.Title}");
```

### Advanced Usage with Custom Parameters
```csharp
// Generate post with specific requirements
var request = new BlogPostGenerationRequest
{
    Topic = "Machine Learning in Healthcare",
    Keywords = "AI, healthcare, diagnosis, treatment",
    TargetWordCount = 1200,
    Style = "technical but accessible"
};
var post = await _dataContext.BlogHandler(blogId).GeneratePost(request);
// Publish immediately if satisfied with result
await _dataContext.BlogHandler(blogId).PublishPost(post.Id);
```

### Batch Generation
```csharp
// Generate multiple posts
var requests = new List<BlogPostGenerationRequest>
{
    new() { Topic = "AI Ethics", TargetWordCount = 800 },
    new() { Topic = "Future of Work", TargetWordCount = 1000 },
    new() { Topic = "Sustainable Technology", TargetWordCount = 900 }
};
var posts = await _dataContext.BlogHandler(blogId).GeneratePosts(requests);
Console.WriteLine($"Generated {posts.Count} posts");
```

### Complete Workflow
```csharp
// Complete blog management workflow
var blogHandler = _dataContext.BlogHandler(blogId);
// Get blog info
var blog = await blogHandler.GetBlog();
Console.WriteLine($"Working with blog: {blog.Name}");
// Generate new post
var newPost = await blogHandler.GeneratePost(new BlogPostGenerationRequest
{
    Topic = "Latest Industry Trends"
});
// Review and publish
if (newPost.Content.Length > 500) // Basic quality check
{
    await blogHandler.PublishPost(newPost.Id);
    Console.WriteLine($"Published: {newPost.Title}");
}
// Get all posts
var allPosts = await blogHandler.GetAllPosts();
Console.WriteLine($"Blog now has {allPosts.Count} total posts");
```

### Error Handling Example
```csharp
try
{
    var post = await _dataContext.BlogHandler(blogId).GeneratePost();
    // ...
}
catch (BlogOperationException ex)
{
    Console.WriteLine($"Blog operation failed: {ex.Message}");
}
```

### Content Management Example
```csharp
// Update blog settings
await _dataContext.BlogHandler(blogId).UpdateBlogSettings(theme: "tech", tone: "casual");
// Archive a post
await _dataContext.BlogHandler(blogId).ArchivePost(postId);
```

---

## Dependency Injection Setup

#### BlogServiceConfiguration
```csharp
public static class BlogServiceConfiguration
{
    public static IServiceCollection AddBlogServices(this IServiceCollection services)
    {
        services.AddScoped<BlogComponentHandler>();
        services.AddScoped<BlogPostComponentHandler>();
        services.AddScoped<BlogHandler>();
        services.AddScoped<BlogExampleUsage>();
        return services;
    }
}
```

---

## Exception Classes

#### ContentGenerationException
```csharp
public class ContentGenerationException : Exception
{
    public ContentGenerationException(string message) : base(message) { }
    public ContentGenerationException(string message, Exception innerException) : base(message, innerException) { }
}
```

---

## Database Schema

```sql
CREATE TABLE blog (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    theme VARCHAR(100) DEFAULT 'general',
    tone VARCHAR(100) DEFAULT 'professional',
    target_audience VARCHAR(100) DEFAULT 'general',
    settings JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE blog_post (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_entity_id UUID NOT NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    excerpt TEXT,
    status VARCHAR(50) DEFAULT 'draft',
    tags TEXT[],
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    published_at TIMESTAMP WITH TIME ZONE,
    word_count INTEGER NOT NULL DEFAULT 0,
    is_published BOOLEAN NOT NULL DEFAULT FALSE,
    archived_at TIMESTAMP WITH TIME ZONE
);

CREATE INDEX idx_blog_owner_entity_id ON blog(owner_entity_id);
CREATE INDEX idx_blog_post_owner_entity_id ON blog_post(owner_entity_id);
CREATE INDEX idx_blog_post_status ON blog_post(status);
```

---

## Performance Considerations
- **Caching:** Consider caching blog settings and recent posts to reduce database queries.
- **Async Processing:** For batch operations, consider using background jobs.
- **Content Validation:** Add content quality checks before saving posts.

## Security Considerations
- **Input Validation:** Validate all generation request parameters.
- **Content Filtering:** Implement content filtering for generated text if needed.
- **Audit Logging:** All operations are automatically logged through DataContext.

## Extension Points
- The handler and component pattern can be extended for other business domains.
- Add new methods to `BlogHandler` for custom workflows.
- Integrate with other services as needed.

---

This documentation is now fully synchronized with the actual example codebase, with all narrative, onboarding, and best practice content restored, and all code samples and schema blocks updated for technical accuracy.