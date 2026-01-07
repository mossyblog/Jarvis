using core.jarvis.Data;
using core.jarvis.Data.Query;
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
            .WithAll<Role>(Filter<Role>.Eq(r => r.Name, "default"))
            .ToEntityComponents();

        var defaultRoleEntity = allRoles.FirstOrDefault();

        var defaultRoleId = defaultRoleEntity.Key;

        // Create user profile with default role (permissions calculated at runtime by PermissionService)
        var userName = !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : email.Split('@')[0];
        var userProfile = new SecurityProfile
        {
            OwnerEntityId = OwnerEntityId,
            Name = userName,
            RoleIds = defaultRoleId != Guid.Empty ? new[] { defaultRoleId.ToString() } : Array.Empty<string>(),
            Avatar = null,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.TryCommit(userProfile);
        Logger.LogInformation("Created SecurityProfile with default role for user: {UserId}", OwnerEntityId);
        return userProfile;
    }

    /// <summary>
    /// Assigns a role to this user.
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

        var updated = profile with
        {
            RoleIds = roleIds.ToArray(),
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
    /// Removes a role from this user.
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

        var updated = profile with
        {
            RoleIds = roleIds.ToArray(),
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
