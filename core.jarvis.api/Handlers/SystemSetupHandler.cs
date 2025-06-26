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
    /// Saves the system setup component.
    /// </summary>
    public async Task<SystemSetup> Save()
    {
        var setup = await GetOrDefault() ?? throw new InvalidOperationException("SystemSetup component not found");
        
        setup = setup with { OwnerEntityId = OwnerEntityId, UpdatedAt = DateTime.UtcNow };
        await DataContext.Commit(setup);
        return setup;
    }

    /// <summary>
    /// Ensures default navigation items exist in the system.
    /// </summary>
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

        var createdItems = new List<NavigationItem>();

        foreach (var navItem in defaultNavItems)
        {
            // Check if navigation item already exists
            var existingItems = await DataContext.Query()
                .WithAll<NavigationItem>(n => n.MenuId == navItem.MenuId)
                .ToEntityComponents();

            if (!existingItems.Any())
            {
                // Create new navigation item
                var navId = Guid.NewGuid();
                var newNavItem = navItem with { OwnerEntityId = navId };
                await DataContext.Commit(newNavItem);
                createdItems.Add(newNavItem);
            }
            else
            {
                // Get existing item
                var existingItem = existingItems.First().Value.Get<NavigationItem>();
                if (existingItem != null)
                {
                    createdItems.Add(existingItem);
                }
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
            .WithAll<Role>(r => true)
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

        var createdRoles = new List<Role>();

        foreach (var role in defaultRoles)
        {
            // Check if role already exists
            var existingRoles = await DataContext.Query()
                .WithAll<Role>(r => r.Name == role.Name)
                .ToEntityComponents();

            if (!existingRoles.Any())
            {
                // Create new role
                var roleId = Guid.NewGuid();
                var newRole = role with { OwnerEntityId = roleId };
                await DataContext.Commit(newRole);
                createdRoles.Add(newRole);
            }
            else
            {
                // Get existing role
                var existingRole = existingRoles.First().Value.Get<Role>();
                if (existingRole != null)
                {
                    createdRoles.Add(existingRole);
                }
            }
        }

        return createdRoles;
    }
}