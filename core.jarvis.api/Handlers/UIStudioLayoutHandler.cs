using core.jarvis.Data;
using core.jarvis.api.Models;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing UIStudioLayout components.
/// Provides operations for creating, updating, and managing layout configurations.
/// </summary>
public class UIStudioLayoutHandler : ComponentHandler<UIStudioLayout>
{
    public UIStudioLayoutHandler(
        IDataContext dataContext,
        ILogger<UIStudioLayoutHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Creates a new layout with the specified configuration.
    /// </summary>
    /// <param name="layout">The layout configuration to create</param>
    /// <returns>The created layout component</returns>
    /// <exception cref="ArgumentException">Thrown when layout configuration is invalid</exception>
    public async Task<UIStudioLayout> CreateLayout(UIStudioLayout layout)
    {
        ValidateLayoutConfiguration(layout);
        await DataContext.Commit(layout);
        return layout;
    }

    /// <summary>
    /// Updates an existing layout configuration.
    /// </summary>
    /// <param name="layout">The updated layout configuration</param>
    /// <returns>The updated layout component</returns>
    /// <exception cref="InvalidOperationException">Thrown when layout is not found</exception>
    /// <exception cref="ArgumentException">Thrown when layout configuration is invalid</exception>
    public async Task<UIStudioLayout> UpdateLayout(UIStudioLayout layout)
    {
        var existingLayout = await Get() ?? throw new InvalidOperationException("Layout not found");
        
        ValidateLayoutConfiguration(layout);
        await DataContext.Commit(layout);
        return layout;
    }

    /// <summary>
    /// Creates a template from this layout.
    /// </summary>
    /// <param name="templateName">Name for the template</param>
    /// <param name="category">Template category</param>
    /// <param name="isPublic">Whether the template should be public</param>
    /// <returns>The updated layout marked as template</returns>
    /// <exception cref="InvalidOperationException">Thrown when layout is not found</exception>
    public async Task<UIStudioLayout> CreateTemplate(string templateName, string category, bool isPublic = false)
    {
        var layout = await Get() ?? throw new InvalidOperationException("Layout not found");
        
        var templateLayout = layout with 
        { 
            LayoutName = templateName,
            IsTemplate = true,
            TemplateCategory = category,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(templateLayout);
        return templateLayout;
    }

    /// <summary>
    /// Clones this layout to create a new layout entity.
    /// </summary>
    /// <param name="newLayoutName">Name for the cloned layout</param>
    /// <param name="newEntityId">Entity ID for the new layout</param>
    /// <returns>The cloned layout component</returns>
    /// <exception cref="InvalidOperationException">Thrown when layout is not found</exception>
    public async Task<UIStudioLayout> CloneLayout(string newLayoutName, Guid newEntityId)
    {
        var layout = await Get() ?? throw new InvalidOperationException("Layout not found");
        
        var clonedLayout = layout with 
        { 
            Id = Guid.NewGuid(),
            OwnerEntityId = newEntityId,
            LayoutName = newLayoutName,
            IsTemplate = false,
            TemplateCategory = null,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        var newHandler = DataContext.For<UIStudioLayoutHandler>(newEntityId);
        await DataContext.Commit(clonedLayout);
        return clonedLayout;
    }

    /// <summary>
    /// Gets all available layout templates.
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <returns>List of layout templates</returns>
    public async Task<List<UIStudioLayout>> GetTemplates(string? category = null)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioLayout>(l => l.IsTemplate);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.WithAll<UIStudioLayout>(l => l.TemplateCategory == category);
        }

        var results = await query.ToEntityComponents();
        var layouts = new List<UIStudioLayout>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioLayoutHandler>(result.Key);
            var layout = await handler.Get();
            if (layout != null)
            {
                layouts.Add(layout);
            }
        }

        return layouts.OrderBy(l => l.TemplateCategory).ThenBy(l => l.LayoutName).ToList();
    }

    /// <summary>
    /// Gets layouts by type.
    /// </summary>
    /// <param name="layoutType">Type of layout to filter by</param>
    /// <returns>List of layouts of the specified type</returns>
    public async Task<List<UIStudioLayout>> GetByType(string layoutType)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioLayout>(l => l.LayoutType == layoutType);

        var results = await query.ToEntityComponents();
        var layouts = new List<UIStudioLayout>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioLayoutHandler>(result.Key);
            var layout = await handler.Get();
            if (layout != null)
            {
                layouts.Add(layout);
            }
        }

        return layouts.OrderBy(l => l.LayoutName).ToList();
    }

    /// <summary>
    /// Updates the grid configuration for this layout.
    /// </summary>
    /// <param name="gridConfig">New grid configuration</param>
    /// <returns>The updated layout component</returns>
    /// <exception cref="InvalidOperationException">Thrown when layout is not found</exception>
    /// <exception cref="ArgumentException">Thrown when grid configuration is invalid</exception>
    public async Task<UIStudioLayout> UpdateGridConfig(Dictionary<string, object> gridConfig)
    {
        var layout = await Get() ?? throw new InvalidOperationException("Layout not found");
        
        ValidateGridConfiguration(gridConfig);
        
        var updatedLayout = layout with 
        { 
            GridConfig = gridConfig,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedLayout);
        return updatedLayout;
    }

    /// <summary>
    /// Updates the responsive breakpoints for this layout.
    /// </summary>
    /// <param name="breakpoints">New responsive breakpoint configuration</param>
    /// <returns>The updated layout component</returns>
    /// <exception cref="InvalidOperationException">Thrown when layout is not found</exception>
    public async Task<UIStudioLayout> UpdateResponsiveBreakpoints(Dictionary<string, object> breakpoints)
    {
        var layout = await Get() ?? throw new InvalidOperationException("Layout not found");
        
        var updatedLayout = layout with 
        { 
            ResponsiveBreakpoints = breakpoints,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedLayout);
        return updatedLayout;
    }

    /// <summary>
    /// Restores a layout from snapshot data.
    /// </summary>
    /// <param name="snapshotData">The snapshot data to restore from</param>
    /// <param name="restoredByEntityId">Entity ID of the user restoring the layout</param>
    /// <returns>The restored layout component</returns>
    public async Task<UIStudioLayout> RestoreFromSnapshot(object snapshotData, Guid restoredByEntityId)
    {
        // Convert snapshot data to layout (would need proper deserialization logic)
        var restoredLayout = snapshotData as UIStudioLayout ?? 
            throw new InvalidOperationException("Invalid snapshot data for layout");
        
        restoredLayout = restoredLayout with 
        { 
            LastUpdated = DateTime.UtcNow,
            Version = null // Reset version for new commit
        };
        
        await DataContext.Commit(restoredLayout);
        return restoredLayout;
    }

    /// <summary>
    /// Validates the layout configuration.
    /// </summary>
    /// <param name="layout">Layout to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    private static void ValidateLayoutConfiguration(UIStudioLayout layout)
    {
        if (string.IsNullOrWhiteSpace(layout.LayoutName))
        {
            throw new ArgumentException("Layout name is required");
        }

        if (string.IsNullOrWhiteSpace(layout.LayoutType))
        {
            throw new ArgumentException("Layout type is required");
        }

        var validLayoutTypes = new[] { "bento", "grid", "flex", "absolute" };
        if (!validLayoutTypes.Contains(layout.LayoutType.ToLowerInvariant()))
        {
            throw new ArgumentException($"Layout type must be one of: {string.Join(", ", validLayoutTypes)}");
        }

        if (layout.GridConfig != null)
        {
            ValidateGridConfiguration(layout.GridConfig);
        }
    }

    /// <summary>
    /// Validates grid configuration.
    /// </summary>
    /// <param name="gridConfig">Grid configuration to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    private static void ValidateGridConfiguration(Dictionary<string, object> gridConfig)
    {
        // Add specific grid validation logic here
        // For example, validate required fields, value ranges, etc.
        
        if (gridConfig.ContainsKey("columns") && gridConfig["columns"] is int columns && columns <= 0)
        {
            throw new ArgumentException("Grid columns must be greater than 0");
        }

        if (gridConfig.ContainsKey("rows") && gridConfig["rows"] is int rows && rows <= 0)
        {
            throw new ArgumentException("Grid rows must be greater than 0");
        }
    }
}