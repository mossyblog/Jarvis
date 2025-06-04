using core.jarvis.Data;
using core.jarvis.Exceptions;
using core.jarvis.Validation;
using Microsoft.Extensions.Logging;

namespace core.jarvis.tests.Examples.Blog;

/// <summary>
/// Component handler for Blog entities.
/// Demonstrates blog management operations.
/// </summary>
public class BlogComponentHandler : ComponentHandler<BlogComponent>
{
    public BlogComponentHandler(IDataContext dataContext, ILogger<BlogComponentHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Creates a new blog with default settings.
    /// </summary>
    public async Task<BlogComponent> CreateBlog(string name, string? description = null)
    {
        Guard.AgainstEmpty(name, nameof(name));

        // Business rule validation
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

        var success = await DataContext.TryCommit(blog);
        if (!success)
        {
            throw new ConcurrencyException("Blog creation failed due to concurrent modification");
        }
        

        return blog;
    }

    /// <summary>
    /// Updates blog settings.
    /// </summary>
    public async Task<BlogComponent> UpdateSettings(string? theme = null, string? tone = null, string? targetAudience = null)
    {
        var blog = await GetRequired();

        if (!string.IsNullOrEmpty(theme)) blog.Theme = theme;
        if (!string.IsNullOrEmpty(tone)) blog.Tone = tone;
        if (!string.IsNullOrEmpty(targetAudience)) blog.TargetAudience = targetAudience;

        var success = await DataContext.TryCommit(blog);
        if (!success)
        {
            throw new ConcurrencyException("Blog settings update failed due to concurrent modification");
        }
        
        Logger.LogInformation("Updated settings for blog {BlogId}", blog.Id);

        return blog;
    }

    /// <summary>
    /// Updates blog description.
    /// </summary>
    public async Task<BlogComponent> UpdateDescription(string description)
    {
        Guard.AgainstNull(description, nameof(description));

        var blog = await GetRequired();
        blog.Description = description;

        var success = await DataContext.TryCommit(blog);
        if (!success)
        {
            throw new ConcurrencyException("Blog description update failed due to concurrent modification");
        }
        
        Logger.LogInformation("Updated description for blog {BlogId}", blog.Id);

        return blog;
    }
} 