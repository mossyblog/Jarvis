using core.jarvis.api.Models;

namespace core.jarvis.api.Services;

/// <summary>
/// Service for resolving and caching user permissions.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets the security profile for a user, using cache when available.
    /// </summary>
    Task<SecurityProfile?> GetSecurityProfileAsync(Guid userId);
    
    /// <summary>
    /// Checks if a user has a specific permission.
    /// Supports hierarchical permissions (e.g., "admin" includes "admin.users.read").
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permission);
    
    /// <summary>
    /// Checks if a user has any of the specified permissions.
    /// </summary>
    Task<bool> HasAnyPermissionAsync(Guid userId, params string[] permissions);
    
    /// <summary>
    /// Checks if a user has all of the specified permissions.
    /// </summary>
    Task<bool> HasAllPermissionsAsync(Guid userId, params string[] permissions);
    
    /// <summary>
    /// Invalidates the cached security profile for a user.
    /// Should be called when permissions or roles change.
    /// </summary>
    Task InvalidateCacheAsync(Guid userId);

    /// <summary>
    /// Invalidates the cached security profiles for all users with a specific role.
    /// Should be called when role permissions change.
    /// </summary>
    Task InvalidateCacheForRoleAsync(Guid roleId);

    /// <summary>
    /// Gets all permissions for a user, expanded from roles.
    /// </summary>
    Task<HashSet<string>> GetUserPermissionsAsync(Guid userId);
}