using core.jarvis.Data;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing SecurityProfile components.
/// </summary>
public class AccountProfileHandler : ComponentHandler<SecurityProfile>
{
    private readonly IPermissionService? _permissionService;

    public AccountProfileHandler(
        IDataContext dataContext,
        ILogger<AccountProfileHandler> logger,
        IPermissionService? permissionService = null)
        : base(dataContext, logger)
    {
        _permissionService = permissionService;
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

        await DataContext.TryCommit(userProfile);
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
                
            // Query role directly instead of handler-to-handler call
            var roleResults = await DataContext.Query()
                .With<Role>(r => r.OwnerEntityId == parsedRoleId)
                .ToEntityComponents();

            if (roleResults.TryGetValue(parsedRoleId, out var roleComponents))
            {
                var role = roleComponents.Get<Role>();
                if (role != null)
                {
                    foreach (var permId in role.PermissionIds)
                    {
                        allPermissionIds.Add(permId);
                    }
                }
            }
        }
        
        var updated = profile with 
        { 
            RoleIds = roleIds.ToArray(),
            PermissionIds = allPermissionIds.ToArray(),
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.TryCommit(updated);
        Logger.LogInformation("Assigned role {RoleId} to user {UserId}", roleId, OwnerEntityId);
        
        // Invalidate permission cache since permissions have changed
        if (_permissionService != null)
        {
            await _permissionService.InvalidateCacheAsync(OwnerEntityId);
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
                
            // Query role directly instead of handler-to-handler call
            var roleResults = await DataContext.Query()
                .With<Role>(r => r.OwnerEntityId == parsedRoleId)
                .ToEntityComponents();

            if (roleResults.TryGetValue(parsedRoleId, out var roleComponents))
            {
                var role = roleComponents.Get<Role>();
                if (role != null)
                {
                    foreach (var permId in role.PermissionIds)
                    {
                        allPermissionIds.Add(permId);
                    }
                }
            }
        }
        
        var updated = profile with 
        { 
            RoleIds = roleIds.ToArray(),
            PermissionIds = allPermissionIds.ToArray(),
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.TryCommit(updated);
        Logger.LogInformation("Removed role {RoleId} from user {UserId}", roleId, OwnerEntityId);
        
        // Invalidate permission cache since permissions have changed
        if (_permissionService != null)
        {
            await _permissionService.InvalidateCacheAsync(OwnerEntityId);
        }
        
        return updated;
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

        await DataContext.TryCommit(updated);
        Logger.LogInformation("Updated profile for user {UserId}", OwnerEntityId);

        // Invalidate permission cache since profile may affect permissions
        if (_permissionService != null)
        {
            await _permissionService.InvalidateCacheAsync(OwnerEntityId);
        }

        return updated;
    }

}