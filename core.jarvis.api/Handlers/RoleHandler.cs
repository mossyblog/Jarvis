using core.jarvis.Data;
using core.jarvis.api.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing Role components.
/// ALL role data operations MUST go through this handler - NO direct commits!
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

        await DataContext.TryCommit(updated); // Use TryCommit - NEVER DataContext.Commit directly!
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

        await DataContext.TryCommit(updated); // Use TryCommit - NEVER DataContext.Commit directly!
        Logger.LogInformation("Revoked permission {PermissionId} from role {RoleId}", permissionId, OwnerEntityId);
        return updated;
    }

    /// <summary>
    /// Creates a new role with the specified properties.
    /// </summary>
    public async Task<Role> CreateRole(Role roleData)
    {
        // Business validation
        Ensure(roleData != null, "Role data required");
        Ensure(!string.IsNullOrWhiteSpace(roleData.Name), "Role name required");
        
        var role = new Role
        {
            Id = roleData.Id != Guid.Empty ? roleData.Id : Guid.NewGuid(),
            OwnerEntityId = OwnerEntityId,
            Name = roleData.Name.Trim(),
            Description = roleData.Description?.Trim(),
            PermissionIds = roleData.PermissionIds ?? Array.Empty<string>(),
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.TryCommit(role);
        Logger.LogInformation("Created role {RoleName} with ID {RoleId}", role.Name, role.Id);
        return role;
    }

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    public async Task<Role> UpdateRole(Role updateData)
    {
        // Get existing role
        var role = await GetOrDefault() ?? throw new InvalidOperationException("Role not found");
        
        // Business rules
        Ensure(updateData != null, "Update data required");
        Ensure(!string.IsNullOrWhiteSpace(updateData.Name), "Role name required");
        
        // Update using immutable pattern
        var updated = role with
        {
            Name = updateData.Name.Trim(),
            Description = updateData.Description?.Trim(),
            PermissionIds = updateData.PermissionIds ?? role.PermissionIds,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.TryCommit(updated);
        Logger.LogInformation("Updated role {RoleId}", OwnerEntityId);
        return updated;
    }

    /// <summary>
    /// Deletes a role from the system.
    /// </summary>
    public async Task<bool> DeleteRole()
    {
        var role = await GetOrDefault() ?? throw new InvalidOperationException("Role not found");
        
        // Actually remove the role
        await DataContext.Remove<Role>(OwnerEntityId);
        Logger.LogInformation("Deleted role {RoleId}", OwnerEntityId);
        return true;
    }
}