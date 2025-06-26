using core.jarvis.Data;
using core.jarvis.api.Models;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing SecurityProfile components.
/// </summary>
public class AccountProfileHandler : ComponentHandler<SecurityProfile>
{
    public AccountProfileHandler(
        IDataContext dataContext,
        ILogger<AccountProfileHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Creates a SecurityProfile with default roles and permissions.
    /// Returns existing profile if already exists.
    /// </summary>
    public async Task<SecurityProfile> CreateWithDefaults(string email)
    {
        var existingProfile = await GetOrDefault();
        if (existingProfile != null)
        {
            Logger.LogInformation("SecurityProfile already exists for ID: {UserId}", OwnerEntityId);
            return existingProfile;
        }

        Logger.LogInformation("Creating SecurityProfile for first-time user: {UserId}", OwnerEntityId);

        // Get default role
        var defaultRoleEntities = await DataContext.Query()
            .WithAll<Role>(r => r.Name == "default")
            .ToEntityComponents();

        var defaultRoleId = defaultRoleEntities.FirstOrDefault().Key;

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
        var userName = email.Split('@')[0]; // Use email prefix as default name
        var userProfile = new SecurityProfile
        {
            OwnerEntityId = OwnerEntityId,
            Name = userName,
            RoleIds = defaultRoleId != Guid.Empty ? new[] { defaultRoleId.ToString() } : Array.Empty<string>(),
            PermissionIds = permissionIds.ToArray(),
            Avatar = null,
            UpdatedAt = DateTime.UtcNow
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
                
            // Get role's permissions
            var roleEntities = await DataContext.Query()
                .WithAll<Role>(r => r.OwnerEntityId == parsedRoleId)
                .ToEntityComponents();
                
            var roleEntity = roleEntities.FirstOrDefault();
            if (roleEntity.Value != null)
            {
                var role = roleEntity.Value.Get<Role>();
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
            UpdatedAt = DateTime.UtcNow
        };

        await DataContext.Commit(updated);
        Logger.LogInformation("Assigned role {RoleId} to user {UserId}", roleId, OwnerEntityId);
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
                
            // Get role's permissions
            var roleEntities = await DataContext.Query()
                .WithAll<Role>(r => r.OwnerEntityId == parsedRoleId)
                .ToEntityComponents();
                
            var roleEntity = roleEntities.FirstOrDefault();
            if (roleEntity.Value != null)
            {
                var role = roleEntity.Value.Get<Role>();
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
            UpdatedAt = DateTime.UtcNow
        };

        await DataContext.Commit(updated);
        Logger.LogInformation("Removed role {RoleId} from user {UserId}", roleId, OwnerEntityId);
        return updated;
    }

}