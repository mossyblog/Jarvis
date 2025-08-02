using core.jarvis.Data;
using core.jarvis.api.Models;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing UIStudioPermission components.
/// Provides operations for creating, updating, and managing access control for UIStudio resources.
/// </summary>
public class UIStudioPermissionHandler : ComponentHandler<UIStudioPermission>
{
    public UIStudioPermissionHandler(
        IDataContext dataContext,
        ILogger<UIStudioPermissionHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Grants permission to a grantee for a specific resource.
    /// </summary>
    /// <param name="permission">The permission configuration to create</param>
    /// <returns>The created permission component</returns>
    /// <exception cref="ArgumentException">Thrown when permission configuration is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when permission already exists</exception>
    public async Task<UIStudioPermission> GrantPermission(UIStudioPermission permission)
    {
        ValidatePermissionConfiguration(permission);
        
        // Check if permission already exists
        var existingPermission = await FindPermission(
            permission.ResourceEntityId, 
            permission.GranteeEntityId, 
            permission.GranteeType);
            
        if (existingPermission != null)
        {
            throw new InvalidOperationException("Permission already exists for this grantee and resource");
        }

        await DataContext.Commit(permission);
        return permission;
    }

    /// <summary>
    /// Updates an existing permission configuration.
    /// </summary>
    /// <param name="permission">The updated permission configuration</param>
    /// <returns>The updated permission component</returns>
    /// <exception cref="InvalidOperationException">Thrown when permission is not found</exception>
    /// <exception cref="ArgumentException">Thrown when permission configuration is invalid</exception>
    public async Task<UIStudioPermission> UpdatePermission(UIStudioPermission permission)
    {
        var existingPermission = await Get() ?? throw new InvalidOperationException("Permission not found");
        
        ValidatePermissionConfiguration(permission);
        await DataContext.Commit(permission);
        return permission;
    }

    /// <summary>
    /// Revokes a permission by marking it as inactive.
    /// </summary>
    /// <returns>The revoked permission component</returns>
    /// <exception cref="InvalidOperationException">Thrown when permission is not found</exception>
    public async Task<UIStudioPermission> RevokePermission()
    {
        var permission = await Get() ?? throw new InvalidOperationException("Permission not found");
        
        var revokedPermission = permission with 
        { 
            IsActive = false,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(revokedPermission);
        return revokedPermission;
    }

    /// <summary>
    /// Activates a previously revoked permission.
    /// </summary>
    /// <returns>The activated permission component</returns>
    /// <exception cref="InvalidOperationException">Thrown when permission is not found</exception>
    public async Task<UIStudioPermission> ActivatePermission()
    {
        var permission = await Get() ?? throw new InvalidOperationException("Permission not found");
        
        var activatedPermission = permission with 
        { 
            IsActive = true,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(activatedPermission);
        return activatedPermission;
    }

    /// <summary>
    /// Updates the permission level for this permission.
    /// </summary>
    /// <param name="permissionLevel">New permission level</param>
    /// <returns>The updated permission component</returns>
    /// <exception cref="InvalidOperationException">Thrown when permission is not found</exception>
    /// <exception cref="ArgumentException">Thrown when permission level is invalid</exception>
    public async Task<UIStudioPermission> UpdatePermissionLevel(string permissionLevel)
    {
        var permission = await Get() ?? throw new InvalidOperationException("Permission not found");
        
        ValidatePermissionLevel(permissionLevel);
        
        var updatedPermission = permission with 
        { 
            PermissionLevel = permissionLevel,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedPermission);
        return updatedPermission;
    }

    /// <summary>
    /// Sets an expiration date for this permission.
    /// </summary>
    /// <param name="expiresAt">When the permission should expire</param>
    /// <returns>The updated permission component</returns>
    /// <exception cref="InvalidOperationException">Thrown when permission is not found</exception>
    /// <exception cref="ArgumentException">Thrown when expiration date is in the past</exception>
    public async Task<UIStudioPermission> SetExpiration(DateTime expiresAt)
    {
        var permission = await Get() ?? throw new InvalidOperationException("Permission not found");
        
        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiration date must be in the future");
        }
        
        var updatedPermission = permission with 
        { 
            ExpiresAt = expiresAt,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(updatedPermission);
        return updatedPermission;
    }

    /// <summary>
    /// Gets all permissions for a specific resource by entity ID and type.
    /// </summary>
    /// <param name="resourceEntityId">Entity ID of the resource</param>
    /// <param name="resourceType">Type of the resource</param>
    /// <param name="includeInactive">Whether to include inactive permissions</param>
    /// <returns>List of permissions for the resource</returns>
    public async Task<List<UIStudioPermission>> GetByResource(Guid resourceEntityId, string resourceType, bool includeInactive = false)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPermission>(p => p.ResourceEntityId == resourceEntityId && p.ResourceType == resourceType);

        if (!includeInactive)
        {
            query = query.WithAll<UIStudioPermission>(p => p.IsActive);
        }

        var results = await query.ToEntityComponents();
        var permissions = new List<UIStudioPermission>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioPermissionHandler>(result.Key);
            var permission = await handler.Get();
            if (permission != null)
            {
                permissions.Add(permission);
            }
        }

        return permissions.OrderBy(p => p.PermissionLevel).ThenBy(p => p.GrantedAt).ToList();
    }

    /// <summary>
    /// Gets all permissions for a specific resource.
    /// </summary>
    /// <param name="resourceEntityId">Entity ID of the resource</param>
    /// <param name="includeInactive">Whether to include inactive permissions</param>
    /// <returns>List of permissions for the resource</returns>
    public async Task<List<UIStudioPermission>> GetResourcePermissions(Guid resourceEntityId, bool includeInactive = false)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPermission>(p => p.ResourceEntityId == resourceEntityId);

        if (!includeInactive)
        {
            query = query.WithAll<UIStudioPermission>(p => p.IsActive);
        }

        var results = await query.ToEntityComponents();
        var permissions = new List<UIStudioPermission>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioPermissionHandler>(result.Key);
            var permission = await handler.Get();
            if (permission != null)
            {
                permissions.Add(permission);
            }
        }

        return permissions.OrderBy(p => p.PermissionLevel).ThenBy(p => p.GrantedAt).ToList();
    }

    /// <summary>
    /// Gets all permissions granted to a specific grantee.
    /// </summary>
    /// <param name="granteeEntityId">Entity ID of the grantee</param>
    /// <param name="granteeType">Type of grantee</param>
    /// <param name="includeInactive">Whether to include inactive permissions</param>
    /// <returns>List of permissions granted to the grantee</returns>
    public async Task<List<UIStudioPermission>> GetGranteePermissions(Guid granteeEntityId, string granteeType, bool includeInactive = false)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPermission>(p => p.GranteeEntityId == granteeEntityId && p.GranteeType == granteeType);

        if (!includeInactive)
        {
            query = query.WithAll<UIStudioPermission>(p => p.IsActive);
        }

        var results = await query.ToEntityComponents();
        var permissions = new List<UIStudioPermission>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioPermissionHandler>(result.Key);
            var permission = await handler.Get();
            if (permission != null)
            {
                permissions.Add(permission);
            }
        }

        return permissions.OrderBy(p => p.ResourceType).ThenBy(p => p.PermissionLevel).ToList();
    }

    /// <summary>
    /// Checks if a grantee has a specific permission level or higher for a resource.
    /// </summary>
    /// <param name="resourceEntityId">Entity ID of the resource</param>
    /// <param name="granteeEntityId">Entity ID of the grantee</param>
    /// <param name="granteeType">Type of grantee</param>
    /// <param name="requiredLevel">Required permission level</param>
    /// <returns>True if the grantee has the required permission level or higher</returns>
    public async Task<bool> HasPermission(Guid resourceEntityId, Guid granteeEntityId, string granteeType, string requiredLevel)
    {
        var permission = await FindPermission(resourceEntityId, granteeEntityId, granteeType);
        
        if (permission == null || !permission.IsActive)
        {
            return false;
        }

        // Check if permission has expired
        if (permission.ExpiresAt.HasValue && permission.ExpiresAt.Value <= DateTime.UtcNow)
        {
            return false;
        }

        return ComparePermissionLevels(permission.PermissionLevel, requiredLevel) >= 0;
    }

    /// <summary>
    /// Gets all expired permissions.
    /// </summary>
    /// <returns>List of expired permissions</returns>
    public async Task<List<UIStudioPermission>> GetExpiredPermissions()
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPermission>(p => p.ExpiresAt != null && p.ExpiresAt <= DateTime.UtcNow && p.IsActive);

        var results = await query.ToEntityComponents();
        var permissions = new List<UIStudioPermission>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioPermissionHandler>(result.Key);
            var permission = await handler.Get();
            if (permission != null)
            {
                permissions.Add(permission);
            }
        }

        return permissions.OrderBy(p => p.ExpiresAt).ToList();
    }

    /// <summary>
    /// Finds a specific permission for a resource and grantee.
    /// </summary>
    /// <param name="resourceEntityId">Entity ID of the resource</param>
    /// <param name="granteeEntityId">Entity ID of the grantee</param>
    /// <param name="granteeType">Type of grantee</param>
    /// <returns>The permission if found, null otherwise</returns>
    public async Task<UIStudioPermission?> FindPermission(Guid resourceEntityId, Guid granteeEntityId, string granteeType)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioPermission>(p => 
                p.ResourceEntityId == resourceEntityId && 
                p.GranteeEntityId == granteeEntityId && 
                p.GranteeType == granteeType);

        var results = await query.ToEntityComponents();
        var permissionEntity = results.FirstOrDefault();
        
        if (permissionEntity.Key == Guid.Empty) return null;

        var entityId = permissionEntity.Key;
        var handler = DataContext.For<UIStudioPermissionHandler>(entityId);
        return await handler.Get();
    }

    /// <summary>
    /// Validates the permission configuration.
    /// </summary>
    /// <param name="permission">Permission to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    private static void ValidatePermissionConfiguration(UIStudioPermission permission)
    {
        if (permission.ResourceEntityId == Guid.Empty)
        {
            throw new ArgumentException("Resource entity ID is required");
        }

        if (string.IsNullOrWhiteSpace(permission.ResourceType))
        {
            throw new ArgumentException("Resource type is required");
        }

        if (permission.GranteeEntityId == Guid.Empty)
        {
            throw new ArgumentException("Grantee entity ID is required");
        }

        if (string.IsNullOrWhiteSpace(permission.GranteeType))
        {
            throw new ArgumentException("Grantee type is required");
        }

        ValidatePermissionLevel(permission.PermissionLevel);

        if (permission.GrantedByEntityId == Guid.Empty)
        {
            throw new ArgumentException("Granted by entity ID is required");
        }

        if (permission.ExpiresAt.HasValue && permission.ExpiresAt.Value <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiration date must be in the future");
        }
    }

    /// <summary>
    /// Validates the permission level.
    /// </summary>
    /// <param name="permissionLevel">Permission level to validate</param>
    /// <exception cref="ArgumentException">Thrown when permission level is invalid</exception>
    private static void ValidatePermissionLevel(string permissionLevel)
    {
        var validLevels = new[] { "view", "edit", "admin", "owner" };
        if (!validLevels.Contains(permissionLevel.ToLowerInvariant()))
        {
            throw new ArgumentException($"Permission level must be one of: {string.Join(", ", validLevels)}");
        }
    }

    /// <summary>
    /// Compares two permission levels and returns their relative ordering.
    /// </summary>
    /// <param name="level1">First permission level</param>
    /// <param name="level2">Second permission level</param>
    /// <returns>Negative if level1 less than level2, 0 if equal, positive if level1 greater than level2</returns>
    private static int ComparePermissionLevels(string level1, string level2)
    {
        var levelHierarchy = new Dictionary<string, int>
        {
            { "view", 1 },
            { "edit", 2 },
            { "admin", 3 },
            { "owner", 4 }
        };

        var value1 = levelHierarchy.GetValueOrDefault(level1.ToLowerInvariant(), 0);
        var value2 = levelHierarchy.GetValueOrDefault(level2.ToLowerInvariant(), 0);

        return value1.CompareTo(value2);
    }
}