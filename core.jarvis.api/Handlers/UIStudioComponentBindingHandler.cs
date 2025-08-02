using core.jarvis.Data;
using core.jarvis.api.Models;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing UIStudioComponentBinding components.
/// Provides operations for creating, updating, and managing component-to-ECS bindings.
/// </summary>
public class UIStudioComponentBindingHandler : ComponentHandler<UIStudioComponentBinding>
{
    public UIStudioComponentBindingHandler(
        IDataContext dataContext,
        ILogger<UIStudioComponentBindingHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Creates a new component binding with the specified configuration.
    /// </summary>
    /// <param name="binding">The component binding to create</param>
    /// <returns>The created component binding</returns>
    /// <exception cref="ArgumentException">Thrown when binding configuration is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when component instance ID already exists on the page</exception>
    public async Task<UIStudioComponentBinding> CreateBinding(UIStudioComponentBinding binding)
    {
        ValidateBindingConfiguration(binding);
        
        // Check for duplicate component instance ID on the same page
        var existingBinding = await FindByComponentInstanceId(binding.PageSlug, binding.ComponentInstanceId);
        if (existingBinding != null)
        {
            throw new InvalidOperationException($"Component instance ID '{binding.ComponentInstanceId}' already exists on this page");
        }

        await DataContext.Commit(binding);
        return binding;
    }

    /// <summary>
    /// Updates an existing component binding configuration.
    /// </summary>
    /// <param name="binding">The updated component binding</param>
    /// <returns>The updated component binding</returns>
    /// <exception cref="InvalidOperationException">Thrown when binding is not found</exception>
    /// <exception cref="ArgumentException">Thrown when binding configuration is invalid</exception>
    public async Task<UIStudioComponentBinding> UpdateBinding(UIStudioComponentBinding binding)
    {
        var existingBinding = await Get() ?? throw new InvalidOperationException("Component binding not found");
        
        ValidateBindingConfiguration(binding);
        
        // Check for duplicate component instance ID if it changed
        if (binding.ComponentInstanceId != existingBinding.ComponentInstanceId)
        {
            var conflictingBinding = await FindByComponentInstanceId(binding.PageSlug, binding.ComponentInstanceId);
            if (conflictingBinding != null && conflictingBinding.Id != binding.Id)
            {
                throw new InvalidOperationException($"Component instance ID '{binding.ComponentInstanceId}' already exists on this page");
            }
        }

        await DataContext.Commit(binding);
        return binding;
    }

    /// <summary>
    /// Updates the field mappings for this component binding.
    /// </summary>
    /// <param name="fieldMappings">New field mapping configuration</param>
    /// <returns>The updated component binding</returns>
    /// <exception cref="InvalidOperationException">Thrown when binding is not found</exception>
    public async Task<UIStudioComponentBinding> UpdateFieldMappings(Dictionary<string, object> fieldMappings)
    {
        var binding = await Get() ?? throw new InvalidOperationException("Component binding not found");
        
        var updatedBinding = binding with 
        { 
            FieldMappings = fieldMappings,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedBinding);
        return updatedBinding;
    }

    /// <summary>
    /// Updates the position configuration for this component binding.
    /// </summary>
    /// <param name="positionConfig">New position configuration</param>
    /// <returns>The updated component binding</returns>
    /// <exception cref="InvalidOperationException">Thrown when binding is not found</exception>
    public async Task<UIStudioComponentBinding> UpdatePosition(Dictionary<string, object> positionConfig)
    {
        var binding = await Get() ?? throw new InvalidOperationException("Component binding not found");
        
        var updatedBinding = binding with 
        { 
            PositionConfig = positionConfig,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedBinding);
        return updatedBinding;
    }

    /// <summary>
    /// Updates the style configuration for this component binding.
    /// </summary>
    /// <param name="styleConfig">New style configuration</param>
    /// <returns>The updated component binding</returns>
    /// <exception cref="InvalidOperationException">Thrown when binding is not found</exception>
    public async Task<UIStudioComponentBinding> UpdateStyle(Dictionary<string, object> styleConfig)
    {
        var binding = await Get() ?? throw new InvalidOperationException("Component binding not found");
        
        var updatedBinding = binding with 
        { 
            StyleConfig = styleConfig,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedBinding);
        return updatedBinding;
    }

    /// <summary>
    /// Sets the visibility of this component binding.
    /// </summary>
    /// <param name="isVisible">Whether the component should be visible</param>
    /// <returns>The updated component binding</returns>
    /// <exception cref="InvalidOperationException">Thrown when binding is not found</exception>
    public async Task<UIStudioComponentBinding> SetVisibility(bool isVisible)
    {
        var binding = await Get() ?? throw new InvalidOperationException("Component binding not found");
        
        var updatedBinding = binding with 
        { 
            IsVisible = isVisible,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedBinding);
        return updatedBinding;
    }

    /// <summary>
    /// Sets the enabled state of this component binding.
    /// </summary>
    /// <param name="isEnabled">Whether the component should be enabled</param>
    /// <returns>The updated component binding</returns>
    /// <exception cref="InvalidOperationException">Thrown when binding is not found</exception>
    public async Task<UIStudioComponentBinding> SetEnabled(bool isEnabled)
    {
        var binding = await Get() ?? throw new InvalidOperationException("Component binding not found");
        
        var updatedBinding = binding with 
        { 
            IsEnabled = isEnabled,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedBinding);
        return updatedBinding;
    }

    /// <summary>
    /// Gets all component bindings for a specific page.
    /// </summary>
    /// <param name="pageSlug">Slug of the page</param>
    /// <returns>List of component bindings for the page</returns>
    public async Task<List<UIStudioComponentBinding>> GetByPageSlug(string pageSlug)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioComponentBinding>(b => b.PageSlug == pageSlug);

        var results = await query.ToEntityComponents();
        var bindings = new List<UIStudioComponentBinding>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioComponentBindingHandler>(result.Key);
            var binding = await handler.Get();
            if (binding != null)
            {
                bindings.Add(binding);
            }
        }

        return bindings.OrderBy(b => b.SortOrder).ThenBy(b => b.ComponentInstanceId).ToList();
    }

    /// <summary>
    /// Gets component bindings for a specific page, optionally filtered by component type.
    /// </summary>
    /// <param name="pageSlug">Slug of the page</param>
    /// <param name="componentType">Optional component type filter</param>
    /// <returns>List of component bindings for the page</returns>
    public async Task<List<UIStudioComponentBinding>> GetByPageSlug(string pageSlug, string? componentType)
    {
        var query = DataContext.Query();
        
        if (string.IsNullOrEmpty(componentType))
        {
            query = query.WithAll<UIStudioComponentBinding>(b => b.PageSlug == pageSlug);
        }
        else
        {
            query = query.WithAll<UIStudioComponentBinding>(b => b.PageSlug == pageSlug && b.ComponentType == componentType);
        }

        var results = await query.ToEntityComponents();
        var bindings = new List<UIStudioComponentBinding>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioComponentBindingHandler>(result.Key);
            var binding = await handler.Get();
            if (binding != null)
            {
                bindings.Add(binding);
            }
        }

        return bindings.OrderBy(b => b.SortOrder).ThenBy(b => b.ComponentInstanceId).ToList();
    }

    /// <summary>
    /// Updates a component binding with dictionary updates.
    /// </summary>
    /// <param name="updates">Dictionary of updates to apply</param>
    /// <returns>Updated component binding</returns>
    public async Task<UIStudioComponentBinding> UpdateBinding(Dictionary<string, object> updates)
    {
        var binding = await Get() ?? throw new InvalidOperationException("Component binding not found");
        
        var updatedBinding = binding;
        
        foreach (var update in updates)
        {
            switch (update.Key.ToLower())
            {
                // GridArea property not available in UIStudioComponentBinding model
                case "fieldmappings":
                    if (update.Value is Dictionary<string, object> fieldMappings)
                        updatedBinding = updatedBinding with { FieldMappings = fieldMappings };
                    break;
                case "positionconfig":
                    if (update.Value is Dictionary<string, object> positionConfig)
                        updatedBinding = updatedBinding with { PositionConfig = positionConfig };
                    break;
                case "styleconfig":
                    if (update.Value is Dictionary<string, object> styleConfig)
                        updatedBinding = updatedBinding with { StyleConfig = styleConfig };
                    break;
                case "isvisible":
                    if (update.Value is bool isVisible)
                        updatedBinding = updatedBinding with { IsVisible = isVisible };
                    break;
                case "isenabled":
                    if (update.Value is bool isEnabled)
                        updatedBinding = updatedBinding with { IsEnabled = isEnabled };
                    break;
                case "sortorder":
                    if (update.Value is int sortOrder)
                        updatedBinding = updatedBinding with { SortOrder = sortOrder };
                    break;
            }
        }

        updatedBinding = updatedBinding with { LastUpdated = DateTime.UtcNow };
        await DataContext.Commit(updatedBinding);
        return updatedBinding;
    }

    /// <summary>
    /// Gets all component bindings of a specific type.
    /// </summary>
    /// <param name="componentType">Type of component to filter by</param>
    /// <returns>List of component bindings of the specified type</returns>
    public async Task<List<UIStudioComponentBinding>> GetByComponentType(string componentType)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioComponentBinding>(b => b.ComponentType == componentType);

        var results = await query.ToEntityComponents();
        var bindings = new List<UIStudioComponentBinding>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioComponentBindingHandler>(result.Key);
            var binding = await handler.Get();
            if (binding != null)
            {
                bindings.Add(binding);
            }
        }

        return bindings.OrderBy(b => b.SortOrder).ToList();
    }

    /// <summary>
    /// Gets all component bindings bound to a specific ECS component type.
    /// </summary>
    /// <param name="boundComponentType">ECS component type to filter by</param>
    /// <returns>List of component bindings using the specified ECS component</returns>
    public async Task<List<UIStudioComponentBinding>> GetByBoundComponentType(string boundComponentType)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioComponentBinding>(b => b.BoundComponentType == boundComponentType);

        var results = await query.ToEntityComponents();
        var bindings = new List<UIStudioComponentBinding>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioComponentBindingHandler>(result.Key);
            var binding = await handler.Get();
            if (binding != null)
            {
                bindings.Add(binding);
            }
        }

        return bindings.OrderBy(b => b.SortOrder).ToList();
    }

    /// <summary>
    /// Finds a component binding by component instance ID within a page.
    /// </summary>
    /// <param name="pageSlug">Slug of the page</param>
    /// <param name="componentInstanceId">Component instance ID to search for</param>
    /// <returns>The component binding if found, null otherwise</returns>
    public async Task<UIStudioComponentBinding?> FindByComponentInstanceId(string pageSlug, string componentInstanceId)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioComponentBinding>(b => b.PageSlug == pageSlug && b.ComponentInstanceId == componentInstanceId);

        var results = await query.ToEntityComponents();
        var bindingEntity = results.FirstOrDefault();
        
        if (bindingEntity.Equals(default(KeyValuePair<Guid, EntityComponents>))) return null;

        var handler = DataContext.For<UIStudioComponentBindingHandler>(bindingEntity.Key);
        return await handler.Get();
    }

    /// <summary>
    /// Validates the component binding configuration.
    /// </summary>
    /// <param name="binding">Component binding to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    private static void ValidateBindingConfiguration(UIStudioComponentBinding binding)
    {
        if (string.IsNullOrWhiteSpace(binding.PageSlug))
        {
            throw new ArgumentException("Page slug is required");
        }

        if (string.IsNullOrWhiteSpace(binding.ComponentType))
        {
            throw new ArgumentException("Component type is required");
        }

        if (string.IsNullOrWhiteSpace(binding.ComponentInstanceId))
        {
            throw new ArgumentException("Component instance ID is required");
        }

        if (string.IsNullOrWhiteSpace(binding.BoundComponentType))
        {
            throw new ArgumentException("Bound component type is required");
        }

        if (binding.CreatedByEntityId == Guid.Empty)
        {
            throw new ArgumentException("Created by entity ID is required");
        }
    }
}