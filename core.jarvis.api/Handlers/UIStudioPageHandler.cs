using core.jarvis.Data;
using core.jarvis.api.Models;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;
using core.jarvis.api.Functions.UIStudio;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing UIStudioPage components.
/// Provides operations for creating, updating, and managing page definitions.
/// </summary>
public class UIStudioPageHandler : ComponentHandler<UIStudioPage>
{
    public UIStudioPageHandler(
        IDataContext dataContext,
        ILogger<UIStudioPageHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Creates a new page with the specified configuration.
    /// </summary>
    /// <param name="page">The page configuration to create</param>
    /// <returns>The created page component</returns>
    /// <exception cref="InvalidOperationException">Thrown when page slug already exists</exception>
    public async Task<UIStudioPage> CreatePage(UIStudioPage page)
    {
        // Validate page slug uniqueness
        if (!string.IsNullOrEmpty(page.PageSlug))
        {
            var existingPage = await FindBySlug(page.PageSlug);
            if (existingPage != null)
            {
                throw new InvalidOperationException($"Page with slug '{page.PageSlug}' already exists");
            }
        }

        // Ensure only one default page exists
        if (page.IsDefault)
        {
            await ClearDefaultPages();
        }

        await DataContext.Commit(page);
        return page;
    }

    /// <summary>
    /// Updates an existing page configuration.
    /// </summary>
    /// <param name="page">The updated page configuration</param>
    /// <returns>The updated page component</returns>
    /// <exception cref="InvalidOperationException">Thrown when page is not found or slug conflicts</exception>
    public async Task<UIStudioPage> UpdatePage(UIStudioPage page)
    {
        var existingPage = await Get() ?? throw new InvalidOperationException("Page not found");

        // Check slug uniqueness if changed
        if (page.PageSlug != existingPage.PageSlug && !string.IsNullOrEmpty(page.PageSlug))
        {
            var conflictingPage = await FindBySlug(page.PageSlug);
            if (conflictingPage != null && conflictingPage.Id != page.Id)
            {
                throw new InvalidOperationException($"Page with slug '{page.PageSlug}' already exists");
            }
        }

        // Handle default page logic
        if (page.IsDefault && !existingPage.IsDefault)
        {
            await ClearDefaultPages();
        }

        await DataContext.Commit(page);
        return page;
    }

    /// <summary>
    /// Publishes a page, making it accessible to users.
    /// </summary>
    /// <returns>The published page component</returns>
    /// <exception cref="InvalidOperationException">Thrown when page is not found</exception>
    public async Task<UIStudioPage> PublishPage()
    {
        var page = await Get() ?? throw new InvalidOperationException("Page not found");
        
        var publishedPage = page with 
        { 
            IsPublished = true,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(publishedPage);
        return publishedPage;
    }

    /// <summary>
    /// Unpublishes a page, making it inaccessible to users.
    /// </summary>
    /// <returns>The unpublished page component</returns>
    /// <exception cref="InvalidOperationException">Thrown when page is not found</exception>
    public async Task<UIStudioPage> UnpublishPage()
    {
        var page = await Get() ?? throw new InvalidOperationException("Page not found");
        
        var unpublishedPage = page with 
        { 
            IsPublished = false,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(unpublishedPage);
        return unpublishedPage;
    }

    /// <summary>
    /// Sets this page as the default page.
    /// </summary>
    /// <returns>The updated page component</returns>
    /// <exception cref="InvalidOperationException">Thrown when page is not found</exception>
    public async Task<UIStudioPage> SetAsDefault()
    {
        var page = await Get() ?? throw new InvalidOperationException("Page not found");
        
        await ClearDefaultPages();
        
        var defaultPage = page with 
        { 
            IsDefault = true,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(defaultPage);
        return defaultPage;
    }

    /// <summary>
    /// Finds a page by its slug.
    /// </summary>
    /// <param name="slug">The page slug to search for</param>
    /// <returns>The page if found, null otherwise</returns>
    public async Task<UIStudioPage?> FindBySlug(string slug)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPage>(p => p.PageSlug == slug);

        var results = await query.ToEntityComponents();
        var pageEntity = results.FirstOrDefault();
        
        if (pageEntity.Equals(default(KeyValuePair<Guid, EntityComponents>))) return null;

        var handler = DataContext.For<UIStudioPageHandler>(pageEntity.Key);
        return await handler.Get();
    }

    /// <summary>
    /// Gets all published pages ordered by sort order.
    /// </summary>
    /// <returns>List of published pages</returns>
    public async Task<List<UIStudioPage>> GetPublishedPages()
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPage>(p => p.IsPublished);

        var results = await query.ToEntityComponents();
        var pages = new List<UIStudioPage>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioPageHandler>(result.Key);
            var page = await handler.Get();
            if (page != null)
            {
                pages.Add(page);
            }
        }

        return pages.OrderBy(p => p.SortOrder).ThenBy(p => p.PageName).ToList();
    }

    /// <summary>
    /// Gets the current default page.
    /// </summary>
    /// <returns>The default page if one exists, null otherwise</returns>
    public async Task<UIStudioPage?> GetDefaultPage()
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPage>(p => p.IsDefault && p.IsPublished);

        var results = await query.ToEntityComponents();
        var pageEntity = results.FirstOrDefault();
        
        if (pageEntity.Equals(default(KeyValuePair<Guid, EntityComponents>))) return null;

        var handler = DataContext.For<UIStudioPageHandler>(pageEntity.Key);
        return await handler.Get();
    }

    /// <summary>
    /// Gets pages by owner entity ID.
    /// </summary>
    /// <param name="ownerEntityId">Owner entity ID</param>
    /// <returns>List of pages</returns>
    public async Task<List<UIStudioPage>> GetByOwner(Guid ownerEntityId)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPage>(p => p.CreatedByEntityId == ownerEntityId);

        var results = await query.ToEntityComponents();
        var pages = new List<UIStudioPage>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioPageHandler>(result.Key);
            var page = await handler.Get();
            if (page != null)
            {
                pages.Add(page);
            }
        }

        return pages.OrderByDescending(p => p.CreatedAt).ToList();
    }

    /// <summary>
    /// Searches pages by query string.
    /// </summary>
    /// <param name="searchQuery">Search query</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of matching pages</returns>
    public async Task<List<UIStudioPage>> SearchPages(string searchQuery, int limit = 20)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPage>(p => 
                p.PageName.Contains(searchQuery) || 
                (p.Description != null && p.Description.Contains(searchQuery)) ||
                (p.Tags != null && p.Tags.Contains(searchQuery)));

        var results = await query.ToEntityComponents();
        var pages = new List<UIStudioPage>();

        foreach (var result in results.Take(limit))
        {
            var handler = DataContext.For<UIStudioPageHandler>(result.Key);
            var page = await handler.Get();
            if (page != null)
            {
                pages.Add(page);
            }
        }

        return pages.OrderByDescending(p => p.LastUpdated).ToList();
    }

    /// <summary>
    /// Restores a page from snapshot data.
    /// </summary>
    /// <param name="snapshotData">Snapshot data</param>
    /// <param name="restoredByEntityId">Entity ID of user performing restore</param>
    /// <returns>Restored page</returns>
    public async Task<UIStudioPage> RestoreFromSnapshot(object snapshotData, Guid restoredByEntityId)
    {
        // This would need proper deserialization logic based on snapshot format
        // For now, assuming the snapshot contains a UIStudioPage object
        if (snapshotData is UIStudioPage pageData)
        {
            var restoredPage = pageData with
            {
                ModifiedByEntityId = restoredByEntityId,
                LastUpdated = DateTime.UtcNow
            };
            
            await DataContext.Commit(restoredPage);
            return restoredPage;
        }
        
        throw new InvalidOperationException("Invalid snapshot data format");
    }

    /// <summary>
    /// Applies sorting to a list of pages.
    /// </summary>
    /// <param name="pages">Pages to sort</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortOrder">Sort order</param>
    /// <returns>Sorted pages</returns>
    private static IEnumerable<UIStudioPage> ApplySorting(IEnumerable<UIStudioPage> pages, string sortBy, string sortOrder)
    {
        var ascending = sortOrder.ToLower() != "desc";
        
        return sortBy.ToLower() switch
        {
            "name" => ascending ? pages.OrderBy(p => p.PageName) : pages.OrderByDescending(p => p.PageName),
            "slug" => ascending ? pages.OrderBy(p => p.PageSlug) : pages.OrderByDescending(p => p.PageSlug),
            "updated" => ascending ? pages.OrderBy(p => p.LastUpdated) : pages.OrderByDescending(p => p.LastUpdated),
            "created" => ascending ? pages.OrderBy(p => p.CreatedAt) : pages.OrderByDescending(p => p.CreatedAt),
            "sortorder" => ascending ? pages.OrderBy(p => p.SortOrder) : pages.OrderByDescending(p => p.SortOrder),
            _ => ascending ? pages.OrderBy(p => p.CreatedAt) : pages.OrderByDescending(p => p.CreatedAt)
        };
    }

    /// <summary>
    /// Clears the default flag from all existing pages.
    /// </summary>
    private async Task ClearDefaultPages()
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPage>(p => p.IsDefault);

        var results = await query.ToEntityComponents();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioPageHandler>(result.Key);
            var page = await handler.Get();
            
            if (page != null)
            {
                var updatedPage = page with 
                { 
                    IsDefault = false,
                    LastUpdated = DateTime.UtcNow
                };
                await DataContext.Commit(updatedPage);
            }
        }
    }

    /// <summary>
    /// Gets pages with applied filters.
    /// </summary>
    /// <param name="filters">Filters to apply</param>
    /// <returns>List of filtered pages</returns>
    /// <exception cref="NotImplementedException">Method not yet implemented</exception>
    public async Task<List<UIStudioPage>> GetPagesWithFilters(Dictionary<string, object> filters)
    {
        await Task.CompletedTask; // Suppress compiler warnings
        throw new NotImplementedException("GetPagesWithFilters method not yet implemented");
    }
}