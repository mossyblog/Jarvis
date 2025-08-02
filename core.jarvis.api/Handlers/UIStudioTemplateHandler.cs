using core.jarvis.Data;
using core.jarvis.api.Models;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;
using core.jarvis.api.Functions.UIStudio;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing UIStudioTemplate components.
/// Provides operations for creating, updating, and managing reusable templates.
/// </summary>
public class UIStudioTemplateHandler : ComponentHandler<UIStudioTemplate>
{
    public UIStudioTemplateHandler(
        IDataContext dataContext,
        ILogger<UIStudioTemplateHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Creates a new template with the specified configuration.
    /// </summary>
    /// <param name="template">The template to create</param>
    /// <returns>The created template component</returns>
    /// <exception cref="ArgumentException">Thrown when template configuration is invalid</exception>
    public async Task<UIStudioTemplate> CreateTemplate(UIStudioTemplate template)
    {
        ValidateTemplateConfiguration(template);
        await DataContext.Commit(template);
        return template;
    }

    /// <summary>
    /// Updates an existing template configuration.
    /// </summary>
    /// <param name="template">The updated template configuration</param>
    /// <returns>The updated template component</returns>
    /// <exception cref="InvalidOperationException">Thrown when template is not found</exception>
    /// <exception cref="ArgumentException">Thrown when template configuration is invalid</exception>
    public async Task<UIStudioTemplate> UpdateTemplate(UIStudioTemplate template)
    {
        var existingTemplate = await Get() ?? throw new InvalidOperationException("Template not found");
        
        ValidateTemplateConfiguration(template);
        await DataContext.Commit(template);
        return template;
    }

    /// <summary>
    /// Publishes a template, making it available to all users.
    /// </summary>
    /// <returns>The published template component</returns>
    /// <exception cref="InvalidOperationException">Thrown when template is not found</exception>
    public async Task<UIStudioTemplate> PublishTemplate()
    {
        var template = await Get() ?? throw new InvalidOperationException("Template not found");
        
        var publishedTemplate = template with 
        { 
            IsPublic = true,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(publishedTemplate);
        return publishedTemplate;
    }

    /// <summary>
    /// Unpublishes a template, making it private.
    /// </summary>
    /// <returns>The unpublished template component</returns>
    /// <exception cref="InvalidOperationException">Thrown when template is not found</exception>
    public async Task<UIStudioTemplate> UnpublishTemplate()
    {
        var template = await Get() ?? throw new InvalidOperationException("Template not found");
        
        var unpublishedTemplate = template with 
        { 
            IsPublic = false,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(unpublishedTemplate);
        return unpublishedTemplate;
    }

    /// <summary>
    /// Features a template, promoting it in template listings.
    /// </summary>
    /// <returns>The featured template component</returns>
    /// <exception cref="InvalidOperationException">Thrown when template is not found</exception>
    public async Task<UIStudioTemplate> FeatureTemplate()
    {
        var template = await Get() ?? throw new InvalidOperationException("Template not found");
        
        var featuredTemplate = template with 
        { 
            IsFeatured = true,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(featuredTemplate);
        return featuredTemplate;
    }

    /// <summary>
    /// Unfeatures a template, removing it from promoted listings.
    /// </summary>
    /// <returns>The unfeatured template component</returns>
    /// <exception cref="InvalidOperationException">Thrown when template is not found</exception>
    public async Task<UIStudioTemplate> UnfeatureTemplate()
    {
        var template = await Get() ?? throw new InvalidOperationException("Template not found");
        
        var unfeaturedTemplate = template with 
        { 
            IsFeatured = false,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(unfeaturedTemplate);
        return unfeaturedTemplate;
    }

    /// <summary>
    /// Records usage of this template.
    /// </summary>
    /// <returns>The updated template component with incremented usage count</returns>
    /// <exception cref="InvalidOperationException">Thrown when template is not found</exception>
    public async Task<UIStudioTemplate> RecordUsage()
    {
        var template = await Get() ?? throw new InvalidOperationException("Template not found");
        
        var updatedTemplate = template with 
        { 
            UsageCount = template.UsageCount + 1,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedTemplate);
        return updatedTemplate;
    }

    /// <summary>
    /// Adds a rating to this template.
    /// </summary>
    /// <param name="rating">Rating value (1-5)</param>
    /// <returns>The updated template component with new average rating</returns>
    /// <exception cref="InvalidOperationException">Thrown when template is not found</exception>
    /// <exception cref="ArgumentException">Thrown when rating is invalid</exception>
    public async Task<UIStudioTemplate> AddRating(int rating)
    {
        var template = await Get() ?? throw new InvalidOperationException("Template not found");
        
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5");
        }

        var newRatingCount = template.RatingCount + 1;
        var currentTotal = (template.AverageRating ?? 0) * template.RatingCount;
        var newAverage = (currentTotal + rating) / newRatingCount;
        
        var updatedTemplate = template with 
        { 
            AverageRating = newAverage,
            RatingCount = newRatingCount,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedTemplate);
        return updatedTemplate;
    }

    /// <summary>
    /// Gets all public templates by category.
    /// </summary>
    /// <param name="category">Category to filter by</param>
    /// <param name="includeSystemTemplates">Whether to include system templates</param>
    /// <returns>List of public templates in the category</returns>
    public async Task<List<UIStudioTemplate>> GetPublicTemplatesByCategory(string category, bool includeSystemTemplates = true)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioTemplate>(t => t.IsPublic && t.IsActive && t.Category == category);

        if (!includeSystemTemplates)
        {
            query = query.WithAll<UIStudioTemplate>(t => !t.IsSystemTemplate);
        }

        var results = await query.ToEntityComponents();
        var templates = new List<UIStudioTemplate>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioTemplateHandler>(result.Key);
            var template = await handler.Get();
            if (template != null)
            {
                templates.Add(template);
            }
        }

        return templates.OrderByDescending(t => t.IsFeatured)
                       .ThenByDescending(t => t.AverageRating ?? 0)
                       .ThenByDescending(t => t.UsageCount)
                       .ThenBy(t => t.TemplateName)
                       .ToList();
    }

    /// <summary>
    /// Gets all featured templates.
    /// </summary>
    /// <param name="templateType">Optional template type filter</param>
    /// <returns>List of featured templates</returns>
    public async Task<List<UIStudioTemplate>> GetFeaturedTemplates(string? templateType = null)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioTemplate>(t => t.IsFeatured && t.IsPublic && t.IsActive);

        if (!string.IsNullOrEmpty(templateType))
        {
            query = query.WithAll<UIStudioTemplate>(t => t.TemplateType == templateType);
        }

        var results = await query.ToEntityComponents();
        var templates = new List<UIStudioTemplate>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioTemplateHandler>(result.Key);
            var template = await handler.Get();
            if (template != null)
            {
                templates.Add(template);
            }
        }

        return templates.OrderByDescending(t => t.AverageRating ?? 0)
                       .ThenByDescending(t => t.UsageCount)
                       .ThenBy(t => t.TemplateName)
                       .ToList();
    }

    /// <summary>
    /// Gets templates by type.
    /// </summary>
    /// <param name="templateType">Type of template to filter by</param>
    /// <param name="publicOnly">Whether to only include public templates</param>
    /// <returns>List of templates of the specified type</returns>
    public async Task<List<UIStudioTemplate>> GetTemplatesByType(string templateType, bool publicOnly = true)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioTemplate>(t => t.TemplateType == templateType && t.IsActive);

        if (publicOnly)
        {
            query = query.WithAll<UIStudioTemplate>(t => t.IsPublic);
        }

        var results = await query.ToEntityComponents();
        var templates = new List<UIStudioTemplate>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioTemplateHandler>(result.Key);
            var template = await handler.Get();
            if (template != null)
            {
                templates.Add(template);
            }
        }

        return templates.OrderBy(t => t.Category)
                       .ThenByDescending(t => t.IsFeatured)
                       .ThenBy(t => t.TemplateName)
                       .ToList();
    }

    /// <summary>
    /// Searches templates by name or tags.
    /// </summary>
    /// <param name="searchTerm">Term to search for</param>
    /// <param name="publicOnly">Whether to only search public templates</param>
    /// <returns>List of matching templates</returns>
    public async Task<List<UIStudioTemplate>> SearchTemplates(string searchTerm, bool publicOnly = true)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<UIStudioTemplate>();
        }

        var query = DataContext.Query()
            .WithAll<UIStudioTemplate>(t => t.IsActive && 
                (t.TemplateName.Contains(searchTerm) || 
                 (t.Tags != null && t.Tags.Contains(searchTerm)) ||
                 (t.Description != null && t.Description.Contains(searchTerm))));

        if (publicOnly)
        {
            query = query.WithAll<UIStudioTemplate>(t => t.IsPublic);
        }

        var results = await query.ToEntityComponents();
        var templates = new List<UIStudioTemplate>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioTemplateHandler>(result.Key);
            var template = await handler.Get();
            if (template != null)
            {
                templates.Add(template);
            }
        }

        return templates.OrderByDescending(t => t.IsFeatured)
                       .ThenByDescending(t => t.AverageRating ?? 0)
                       .ThenByDescending(t => t.UsageCount)
                       .ToList();
    }

    /// <summary>
    /// Gets templates created by a specific user.
    /// </summary>
    /// <param name="createdByEntityId">Entity ID of the creator</param>
    /// <param name="includeInactive">Whether to include inactive templates</param>
    /// <returns>List of templates created by the user</returns>
    public async Task<List<UIStudioTemplate>> GetUserTemplates(Guid createdByEntityId, bool includeInactive = false)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioTemplate>(t => t.CreatedByEntityId == createdByEntityId);

        if (!includeInactive)
        {
            query = query.WithAll<UIStudioTemplate>(t => t.IsActive);
        }

        var results = await query.ToEntityComponents();
        var templates = new List<UIStudioTemplate>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioTemplateHandler>(result.Key);
            var template = await handler.Get();
            if (template != null)
            {
                templates.Add(template);
            }
        }

        return templates.OrderByDescending(t => t.CreatedAt).ToList();
    }

    /// <summary>
    /// Gets templates by owner entity ID.
    /// </summary>
    /// <param name="ownerEntityId">Owner entity ID</param>
    /// <returns>List of templates</returns>
    public async Task<List<UIStudioTemplate>> GetByOwner(Guid ownerEntityId)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioTemplate>(t => t.CreatedByEntityId == ownerEntityId && t.IsActive);

        var results = await query.ToEntityComponents();
        var templates = new List<UIStudioTemplate>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioTemplateHandler>(result.Key);
            var template = await handler.Get();
            if (template != null)
            {
                templates.Add(template);
            }
        }

        return templates.OrderByDescending(t => t.CreatedAt).ToList();
    }

    /// <summary>
    /// Searches templates by query string.
    /// </summary>
    /// <param name="searchQuery">Search query</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of matching templates</returns>
    public async Task<List<UIStudioTemplate>> SearchTemplates(string searchQuery, int limit = 20)
    {
        return await SearchTemplates(searchQuery, true); // Use existing search method
    }

    /// <summary>
    /// Restores a template from snapshot data.
    /// </summary>
    /// <param name="snapshotData">Snapshot data</param>
    /// <param name="restoredByEntityId">Entity ID of user performing restore</param>
    /// <returns>Restored template</returns>
    public async Task<UIStudioTemplate> RestoreFromSnapshot(object snapshotData, Guid restoredByEntityId)
    {
        // This would need proper deserialization logic based on snapshot format
        if (snapshotData is UIStudioTemplate templateData)
        {
            var restoredTemplate = templateData with
            {
                ModifiedByEntityId = restoredByEntityId,
                LastUpdated = DateTime.UtcNow
            };
            
            await DataContext.Commit(restoredTemplate);
            return restoredTemplate;
        }
        
        throw new InvalidOperationException("Invalid snapshot data format");
    }

    /// <summary>
    /// Validates the template configuration.
    /// </summary>
    /// <param name="template">Template to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    private static void ValidateTemplateConfiguration(UIStudioTemplate template)
    {
        if (string.IsNullOrWhiteSpace(template.TemplateName))
        {
            throw new ArgumentException("Template name is required");
        }

        if (string.IsNullOrWhiteSpace(template.TemplateType))
        {
            throw new ArgumentException("Template type is required");
        }

        var validTemplateTypes = new[] { "page", "layout", "component_set" };
        if (!validTemplateTypes.Contains(template.TemplateType.ToLowerInvariant()))
        {
            throw new ArgumentException($"Template type must be one of: {string.Join(", ", validTemplateTypes)}");
        }

        if (string.IsNullOrWhiteSpace(template.Category))
        {
            throw new ArgumentException("Template category is required");
        }

        if (template.CreatedByEntityId == Guid.Empty)
        {
            throw new ArgumentException("Created by entity ID is required");
        }

        if (template.AverageRating.HasValue && (template.AverageRating < 1 || template.AverageRating > 5))
        {
            throw new ArgumentException("Average rating must be between 1 and 5");
        }

        if (template.RatingCount < 0)
        {
            throw new ArgumentException("Rating count cannot be negative");
        }

        if (template.UsageCount < 0)
        {
            throw new ArgumentException("Usage count cannot be negative");
        }
    }
}