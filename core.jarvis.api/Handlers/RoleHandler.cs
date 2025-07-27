using core.jarvis.Data;
using core.jarvis.api.Models;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing Role components.
/// </summary>
public class RoleHandler : ComponentHandler<Role>
{
    public RoleHandler(
        IDataContext dataContext,
        ILogger<RoleHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Grants a permission to this role by adding it to the PermissionIds collection.
    /// If the permission is already granted, no changes are made.
    /// </summary>
    /// <param name="permissionId">The unique identifier of the permission to grant</param>
    /// <returns>The updated Role component with the new permission added</returns>
    /// <exception cref="InvalidOperationException">Thrown when the Role component is not found</exception>
    public async Task<Role> GrantPermission(Guid permissionId)
    {
        var role = await GetOrDefault() ?? throw new InvalidOperationException("Role component not found");
        
        var permissionIds = role.PermissionIds.ToList();
        var permissionIdStr = permissionId.ToString();
        
        if (permissionIds.Contains(permissionIdStr))
        {
            Logger.LogInformation("Role {RoleId} already has permission {PermissionId}", OwnerEntityId, permissionId);
            return role;
        }

        permissionIds.Add(permissionIdStr);
        
        var updated = role with 
        { 
            PermissionIds = permissionIds.ToArray(),
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updated);
        Logger.LogInformation("Granted permission {PermissionId} to role {RoleId}", permissionId, OwnerEntityId);
        return updated;
    }

    /// <summary>
    /// Revokes a permission from this role by removing it from the PermissionIds collection.
    /// If the permission is not currently granted, no changes are made.
    /// </summary>
    /// <param name="permissionId">The unique identifier of the permission to revoke</param>
    /// <returns>The updated Role component with the permission removed</returns>
    /// <exception cref="InvalidOperationException">Thrown when the Role component is not found</exception>
    public async Task<Role> RevokePermission(Guid permissionId)
    {
        var role = await GetOrDefault() ?? throw new InvalidOperationException("Role component not found");
        
        var permissionIds = role.PermissionIds.ToList();
        var permissionIdStr = permissionId.ToString();
        
        if (!permissionIds.Contains(permissionIdStr))
        {
            Logger.LogInformation("Role {RoleId} doesn't have permission {PermissionId}", OwnerEntityId, permissionId);
            return role;
        }

        permissionIds.Remove(permissionIdStr);
        
        var updated = role with 
        { 
            PermissionIds = permissionIds.ToArray(),
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updated);
        Logger.LogInformation("Revoked permission {PermissionId} from role {RoleId}", permissionId, OwnerEntityId);
        return updated;
    }
}