using core.jarvis.Data;
using core.jarvis.api.Models;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for navigation business logic following ECS ComponentHandler pattern.
/// Manages navigation filtering, permissions, and business rules.
/// </summary>
public class NavigationHandler : ComponentHandler<NavigationItem>
{
    public NavigationHandler(IDataContext dataContext, ILogger<NavigationHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Gets navigation items filtered by user permissions.
    /// This is the core navigation business logic that determines what navigation
    /// items a user can see based on their permission IDs.
    /// </summary>
    /// <param name="userPermissionIds">Array of permission IDs the user has access to</param>
    /// <returns>List of NavigationItem components the user can access, ordered by SortOrder</returns>
    public async Task<List<NavigationItem>> GetUserNavigation(string[] userPermissionIds)
    {
        // Get all navigation items
        var allNavItems = await DataContext.Query()
            .WithAll<NavigationItem>()
            .ToEntityComponents();
            
        var navigation = new List<NavigationItem>();
        
        foreach (var entity in allNavItems)
        {
            var navItem = entity.Value.Get<NavigationItem>();
            if (navItem != null && CanUserAccessNavigation(navItem, userPermissionIds))
            {
                navigation.Add(navItem);
            }
        }

        Logger.LogInformation("Retrieved {Count} navigation items for user with {PermCount} permissions", 
                              navigation.Count, userPermissionIds.Length);
        
        return navigation.OrderBy(n => n.SortOrder).ToList();
    }

    /// <summary>
    /// Gets all navigation items without permission filtering.
    /// Used for administrative functions where all navigation items need to be displayed.
    /// </summary>
    /// <returns>List of all NavigationItem components, ordered by SortOrder</returns>
    public async Task<List<NavigationItem>> GetAllNavigation()
    {
        var allNavItems = await DataContext.Query()
            .WithAll<NavigationItem>()
            .ToEntityComponents();
            
        var navigation = new List<NavigationItem>();
        
        foreach (var entity in allNavItems)
        {
            var navItem = entity.Value.Get<NavigationItem>();
            if (navItem != null)
            {
                navigation.Add(navItem);
            }
        }

        Logger.LogInformation("Retrieved {Count} total navigation items", navigation.Count);
        
        return navigation.OrderBy(n => n.SortOrder).ToList();
    }

    /// <summary>
    /// Checks if a user can access a specific navigation item by MenuId.
    /// Used for programmatic access control checks.
    /// </summary>
    /// <param name="menuId">The MenuId of the navigation item to check</param>
    /// <param name="userPermissionIds">Array of permission IDs the user has access to</param>
    /// <returns>True if user can access the navigation item, false otherwise</returns>
    public async Task<bool> CanAccessNavigation(string menuId, string[] userPermissionIds)
    {
        var allNavItems = await DataContext.Query()
            .WithAll<NavigationItem>(n => n.MenuId == menuId)
            .ToEntityComponents();
            
        var navItem = allNavItems
            .Select(kvp => kvp.Value.Get<NavigationItem>())
            .FirstOrDefault(item => item != null && item.MenuId == menuId);
            
        if (navItem == null)
        {
            Logger.LogWarning("Navigation item with MenuId {MenuId} not found", menuId);
            return false;
        }
        
        return CanUserAccessNavigation(navItem, userPermissionIds);
    }

    /// <summary>
    /// Gets navigation items by scope/category.
    /// Allows filtering navigation items by a specific scope or category.
    /// </summary>
    /// <param name="scope">The scope to filter by (e.g., "admin", "user", "global")</param>
    /// <param name="userPermissionIds">Array of permission IDs the user has access to</param>
    /// <returns>List of NavigationItem components in the specified scope that user can access</returns>
    public async Task<List<NavigationItem>> GetNavigationByScope(string scope, string[] userPermissionIds)
    {
        // Note: Current NavigationItem model doesn't have Scope field
        // This method is prepared for future scope functionality
        // For now, return all user navigation items
        Logger.LogInformation("Scope filtering requested for '{Scope}' but not yet implemented", scope);
        return await GetUserNavigation(userPermissionIds);
    }

    /// <summary>
    /// Creates a new navigation item with proper validation.
    /// Ensures the navigation item follows business rules and doesn't conflict with existing items.
    /// </summary>
    /// <param name="navigationItem">The NavigationItem to create</param>
    /// <returns>The created NavigationItem with proper OwnerEntityId and timestamps</returns>
    public async Task<NavigationItem> CreateNavigation(NavigationItem navigationItem)
    {
        // Validate MenuId uniqueness
        var existingItems = await DataContext.Query()
            .WithAll<NavigationItem>(n => n.MenuId == navigationItem.MenuId)
            .ToEntityComponents();
            
        if (existingItems.Any())
        {
            throw new InvalidOperationException($"Navigation item with MenuId '{navigationItem.MenuId}' already exists");
        }
        
        // Set proper entity ownership and timestamp
        var newNavItem = navigationItem with 
        { 
            OwnerEntityId = OwnerEntityId,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.TryCommit(newNavItem);
        Logger.LogInformation("Created navigation item {MenuId} for entity {EntityId}", 
                              newNavItem.MenuId, OwnerEntityId);
        
        return newNavItem;
    }

    /// <summary>
    /// Updates an existing navigation item.
    /// Maintains proper ECS update patterns with timestamps.
    /// </summary>
    /// <param name="updatedNavigationItem">The updated NavigationItem data</param>
    /// <returns>The updated NavigationItem</returns>
    public async Task<NavigationItem> UpdateNavigation(NavigationItem updatedNavigationItem)
    {
        var existing = await GetOrDefault();
        if (existing == null)
        {
            throw new InvalidOperationException("NavigationItem component not found");
        }
        
        var updated = updatedNavigationItem with 
        { 
            OwnerEntityId = OwnerEntityId,
            LastUpdated = DateTime.UtcNow
        };
        
        await DataContext.TryCommit(updated);
        Logger.LogInformation("Updated navigation item {MenuId} for entity {EntityId}", 
                              updated.MenuId, OwnerEntityId);
        
        return updated;
    }

    /// <summary>
    /// Private helper method to determine if a user can access a specific navigation item.
    /// Encapsulates the permission checking logic.
    /// </summary>
    /// <param name="navItem">The NavigationItem to check</param>
    /// <param name="userPermissionIds">Array of permission IDs the user has access to</param>
    /// <returns>True if user can access the navigation item, false otherwise</returns>
    private static bool CanUserAccessNavigation(NavigationItem navItem, string[] userPermissionIds)
    {
        // If no permission required, user can access
        if (!navItem.RequiredPermissionId.HasValue)
        {
            return navItem.IsActive;
        }
        
        // Check if user has the required permission and item is active
        return navItem.IsActive && 
               userPermissionIds.Contains(navItem.RequiredPermissionId.Value.ToString());
    }
}