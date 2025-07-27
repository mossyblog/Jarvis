using core.jarvis.Data;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing SecurityProfile components.
/// </summary>
public class AccountProfileHandler : ComponentHandler<SecurityProfile>
{
    private readonly IServiceProvider _serviceProvider;
    
    public AccountProfileHandler(
        IDataContext dataContext,
        ILogger<AccountProfileHandler> logger,
        IServiceProvider serviceProvider)
        : base(dataContext, logger)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a SecurityProfile with default roles and permissions.
    /// Returns existing profile if already exists.
    /// </summary>
    public async Task<SecurityProfile> CreateWithDefaults(string email, string? fullName = null)
    {
        var existingProfile = await GetOrDefault();
        if (existingProfile != null)
        {
            Logger.LogInformation("SecurityProfile already exists for ID: {UserId}", OwnerEntityId);
            return existingProfile;
        }

        Logger.LogInformation("Creating SecurityProfile for first-time user: {UserId}", OwnerEntityId);

        // Get default role - fetch all roles and filter in memory
        var allRoles = await DataContext.Query()
            .WithAll<Role>(r => true)
            .ToEntityComponents();

        var defaultRoleEntity = allRoles
            .FirstOrDefault(kvp => 
            {
                var role = kvp.Value.Get<Role>();
                return role != null && role.Name == "default";
            });
            
        var defaultRoleId = defaultRoleEntity.Key;

        // Get permissions from default role
        var permissionIds = new List<string>();
        if (defaultRoleId != Guid.Empty)
        {
            var roleHandler = DataContext.For<RoleHandler>(defaultRoleId);
            var defaultRole = await roleHandler.Get();
            if (defaultRole != null)
            {
                permissionIds.AddRange(defaultRole.PermissionIds);
            }
        }

        // Create user profile with default role and permissions
        var userName = !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : email.Split('@')[0]; // Use provided name or email prefix as default
        var userProfile = new SecurityProfile
        {
            OwnerEntityId = OwnerEntityId,
            Name = userName,
            RoleIds = defaultRoleId != Guid.Empty ? new[] { defaultRoleId.ToString() } : Array.Empty<string>(),
            PermissionIds = permissionIds.ToArray(),
            Avatar = null,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(userProfile);
        Logger.LogInformation("Created SecurityProfile with default role for user: {UserId}", OwnerEntityId);
        return userProfile;
    }

    /// <summary>
    /// Assigns a role to this user and recalculates permissions.
    /// </summary>
    public async Task<SecurityProfile> AssignRole(Guid roleId)
    {
        var profile = await GetOrDefault() ?? throw new InvalidOperationException("SecurityProfile component not found");
        
        var roleIds = profile.RoleIds.ToList();
        var roleIdStr = roleId.ToString();
        
        if (roleIds.Contains(roleIdStr))
        {
            Logger.LogInformation("User {UserId} already has role {RoleId}", OwnerEntityId, roleId);
            return profile;
        }

        roleIds.Add(roleIdStr);
        
        // Recalculate permissions based on all roles
        var allPermissionIds = new HashSet<string>();
        foreach (var rid in roleIds)
        {
            if (!Guid.TryParse(rid, out var parsedRoleId))
                continue;
                
            // Get role's permissions - use handler's Get method
            var roleHandler = DataContext.For<RoleHandler>(parsedRoleId);
            Role? role = null;
            try
            {
                role = await roleHandler.Get();
            }
            catch
            {
                // Role not found
                // TODO: Should throw an error and not be caught silently.
            }
                
            if (role != null)
            {
                foreach (var permId in role.PermissionIds)
                {
                    allPermissionIds.Add(permId);
                }
            }
        }
        
        var updated = profile with 
        { 
            RoleIds = roleIds.ToArray(),
            PermissionIds = allPermissionIds.ToArray(),
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updated);
        Logger.LogInformation("Assigned role {RoleId} to user {UserId}", roleId, OwnerEntityId);
        
        // Invalidate permission cache since permissions have changed
        var permissionService = _serviceProvider.GetService<IPermissionService>();
        if (permissionService != null)
        {
            await permissionService.InvalidateCacheAsync(OwnerEntityId);
        }
        
        return updated;
    }

    /// <summary>
    /// Removes a role from this user and recalculates permissions.
    /// </summary>
    public async Task<SecurityProfile> RemoveRole(Guid roleId)
    {
        var profile = await GetOrDefault() ?? throw new InvalidOperationException("SecurityProfile component not found");
        
        var roleIds = profile.RoleIds.ToList();
        var roleIdStr = roleId.ToString();
        
        if (!roleIds.Contains(roleIdStr))
        {
            Logger.LogInformation("User {UserId} doesn't have role {RoleId}", OwnerEntityId, roleId);
            return profile;
        }

        roleIds.Remove(roleIdStr);
        
        // Recalculate permissions based on remaining roles
        var allPermissionIds = new HashSet<string>();
        foreach (var rid in roleIds)
        {
            if (!Guid.TryParse(rid, out var parsedRoleId))
                continue;
                
            // Get role's permissions - use handler's Get method
            var roleHandler = DataContext.For<RoleHandler>(parsedRoleId);
            Role? role = null;
            try
            {
                role = await roleHandler.Get();
            }
            catch
            {
                // Role not found
                // TODO: Should throw an error and not be caught silently.
            }
                
            if (role != null)
            {
                foreach (var permId in role.PermissionIds)
                {
                    allPermissionIds.Add(permId);
                }
            }
        }
        
        var updated = profile with 
        { 
            RoleIds = roleIds.ToArray(),
            PermissionIds = allPermissionIds.ToArray(),
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updated);
        Logger.LogInformation("Removed role {RoleId} from user {UserId}", roleId, OwnerEntityId);
        
        // Invalidate permission cache since permissions have changed
        var permissionService = _serviceProvider.GetService<IPermissionService>();
        if (permissionService != null)
        {
            await permissionService.InvalidateCacheAsync(OwnerEntityId);
        }
        
        return updated;
    }

    /// <summary>
    /// Gets navigation items filtered by user permissions.
    /// </summary>
    public async Task<List<NavigationItem>> GetUserNavigation()
    {
        var profile = await GetOrDefault();
        if (profile == null)
        {
            Logger.LogWarning("No SecurityProfile found for user {UserId}", OwnerEntityId);
            return new List<NavigationItem>();
        }

        // Get all navigation items
        var allNavItems = await DataContext.Query()
            .WithAll<NavigationItem>()
            .ToEntityComponents();
            
        var userPermissions = profile.PermissionIds;
        var navigation = new List<NavigationItem>();
        
        foreach (var entity in allNavItems)
        {
            var navItem = entity.Value.Get<NavigationItem>();
            if (navItem != null && 
                (!navItem.RequiredPermissionId.HasValue || 
                 userPermissions.Contains(navItem.RequiredPermissionId.Value.ToString())))
            {
                navigation.Add(navItem);
            }
        }

        Logger.LogInformation("Retrieved {Count} navigation items for user {UserId}", navigation.Count, OwnerEntityId);
        return navigation;
    }

    /// <summary>
    /// Updates the user profile with new data.
    /// </summary>
    public async Task<SecurityProfile> UpdateProfile(SecurityProfile updateRequest)
    {
        var profile = await GetOrDefault();
        if (profile == null)
        {
            throw new InvalidOperationException($"SecurityProfile not found for user {OwnerEntityId}");
        }

        var updated = profile with
        {
            Name = !string.IsNullOrEmpty(updateRequest.Name) ? updateRequest.Name : profile.Name,
            Avatar = updateRequest.Avatar, // Allow null to clear avatar
            Preferences = updateRequest.Preferences ?? profile.Preferences,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updated);
        Logger.LogInformation("Updated profile for user {UserId}", OwnerEntityId);
        
        return updated;
    }

}