using core.jarvis.Data;
using core.jarvis.api.Models;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing SystemSetup components.
/// </summary>
public class SystemSetupHandler : ComponentHandler<SystemSetup>
{
    public SystemSetupHandler(IDataContext dataContext, ILogger<SystemSetupHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Saves the system setup component to the database.
    /// Updates the OwnerEntityId and LastUpdated timestamp before committing.
    /// </summary>
    /// <returns>The saved SystemSetup component with updated metadata</returns>
    /// <exception cref="InvalidOperationException">Thrown when the SystemSetup component is not found</exception>
    public async Task<SystemSetup> Save()
    {
        var setup = await GetOrDefault() ?? throw new InvalidOperationException("SystemSetup component not found");
        
        setup = setup with { OwnerEntityId = OwnerEntityId, LastUpdated = DateTime.UtcNow };
        await DataContext.TryCommit(setup);
        return setup;
    }

    /// <summary>
    /// Ensures default navigation items exist in the system.
    /// Creates standard navigation menu items (Dashboard, Accounts, Roles, Permissions, Security)
    /// if they don't already exist in the database.
    /// </summary>
    /// <returns>A list of all NavigationItem objects that were created or already existed</returns>
    /// <remarks>
    /// This method is typically called during system initialization to ensure
    /// the basic navigation structure is available for users.
    /// </remarks>
    public async Task<List<NavigationItem>> EnsureDefaultNavigation()
    {
        var defaultNavItems = new List<NavigationItem>
        {
            new NavigationItem
            {
                MenuId = "dashboard",
                Label = "Dashboard",
                Href = "/",
                Icon = "dashboard",
                SortOrder = 1,
                IsActive = true,
                RequiredPermissionId = null
            },
            new NavigationItem
            {
                MenuId = "accounts",
                Label = "Accounts",
                Href = "/accounts",
                Icon = "accounts",
                SortOrder = 2,
                IsActive = true,
                RequiredPermissionId = null // Will be set to actual permission ID later
            },
            new NavigationItem
            {
                MenuId = "roles",
                Label = "Roles",
                Href = "/roles",
                Icon = "shield",
                SortOrder = 3,
                IsActive = true,
                RequiredPermissionId = null // Will be set to actual permission ID later
            },
            new NavigationItem
            {
                MenuId = "settings",
                Label = "Settings",
                Href = "/settings",
                Icon = "settings",
                SortOrder = 4,
                IsActive = true,
                RequiredPermissionId = null // Will be set to actual permission ID later
            }
        };

        // Fetch all navigation items ONCE before the loop
        var allItems = await DataContext.Query()
            .WithAll<NavigationItem>()
            .ToEntityComponents();

        var existingByMenuId = allItems
            .Select(kvp => kvp.Value.Get<NavigationItem>())
            .Where(item => item != null)
            .GroupBy(item => item!.MenuId)
            .ToDictionary(g => g.Key, g => g.First()!);

        var createdItems = new List<NavigationItem>();

        foreach (var navItem in defaultNavItems)
        {
            if (existingByMenuId.TryGetValue(navItem.MenuId, out var existingItem))
            {
                createdItems.Add(existingItem);
            }
            else
            {
                // Create new navigation item
                var navId = Guid.NewGuid();
                var newNavItem = navItem with { OwnerEntityId = navId };
                await DataContext.TryCommit(newNavItem);
                createdItems.Add(newNavItem);
            }
        }

        return createdItems;
    }

    /// <summary>
    /// Gets all roles in the system.
    /// </summary>
    public async Task<List<Role>> GetAllRoles()
    {
        var roleEntities = await DataContext.Query()
            .WithAll<Role>()
            .ToEntityComponents();

        var roles = new List<Role>();
        foreach (var kvp in roleEntities)
        {
            var role = kvp.Value.Get<Role>();
            if (role != null)
            {
                roles.Add(role);
            }
        }

        return roles.OrderBy(r => r.Name).ToList();
    }

    /// <summary>
    /// Ensures default roles exist in the system.
    /// </summary>
    public async Task<List<Role>> EnsureDefaultRoles()
    {
        var defaultRoles = new List<Role>
        {
            new Role
            {
                Name = "Administrator",
                Description = "Full system access",
                PermissionIds = new string[] { } // Will be populated with actual permission IDs
            },
            new Role
            {
                Name = "User",
                Description = "Standard user access",
                PermissionIds = new string[] { } // Will be populated with actual permission IDs
            },
            new Role
            {
                Name = "Guest",
                Description = "Limited read-only access",
                PermissionIds = new string[] { }
            }
        };

        // Fetch all roles ONCE before the loop
        var allRoles = await DataContext.Query()
            .WithAll<Role>()
            .ToEntityComponents();

        var existingByName = allRoles
            .Select(kvp => kvp.Value.Get<Role>())
            .Where(r => r != null)
            .GroupBy(r => r!.Name)
            .ToDictionary(g => g.Key, g => g.First()!);

        var createdRoles = new List<Role>();

        foreach (var role in defaultRoles)
        {
            if (existingByName.TryGetValue(role.Name, out var existingRole))
            {
                createdRoles.Add(existingRole);
            }
            else
            {
                // Create new role
                var roleId = Guid.NewGuid();
                var newRole = role with { OwnerEntityId = roleId };
                await DataContext.TryCommit(newRole);
                createdRoles.Add(newRole);
            }
        }

        return createdRoles;
    }
}