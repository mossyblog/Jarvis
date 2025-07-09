using System.Collections.Concurrent;
using core.jarvis.api.Models;
using core.jarvis.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Services;

/// <summary>
/// Implementation of permission resolution with caching.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IDataContext _dataContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;
    private readonly TimeSpan _cacheExpiration;
    
    // Cache for expanded permissions (permission -> child permissions)
    private static readonly ConcurrentDictionary<string, HashSet<string>> _permissionHierarchy = new();
    
    public PermissionService(
        IDataContext dataContext,
        IMemoryCache cache,
        ILogger<PermissionService> logger,
        IConfiguration configuration)
    {
        _dataContext = dataContext;
        _cache = cache;
        _logger = logger;
        
        // Default 5 minutes, configurable
        var cacheMinutes = configuration.GetValue<int>("Jwt:PermissionCacheTTLMinutes", 5);
        _cacheExpiration = TimeSpan.FromMinutes(cacheMinutes);
    }
    
    public async Task<SecurityProfile?> GetSecurityProfileAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);
        
        // Try to get from cache
        if (_cache.TryGetValue<SecurityProfile>(cacheKey, out var cached))
        {
            _logger.LogDebug("Security profile cache hit for user {UserId}", userId);
            return cached;
        }
        
        // Load from database
        _logger.LogDebug("Loading security profile from database for user {UserId}", userId);
        
        var profiles = await _dataContext.Query()
            .WithAll<SecurityProfile>(p => p.OwnerEntityId == userId)
            .ToEntityComponents();
            
        var profile = profiles.FirstOrDefault().Value?.Get<SecurityProfile>();
        
        if (profile != null)
        {
            // Cache the profile
            _cache.Set(cacheKey, profile, _cacheExpiration);
            _logger.LogDebug("Cached security profile for user {UserId}", userId);
        }
        else
        {
            _logger.LogWarning("No security profile found for user {UserId}", userId);
        }
        
        return profile;
    }
    
    public async Task<bool> HasPermissionAsync(Guid userId, string permission)
    {
        var userPermissions = await GetUserPermissionsAsync(userId);
        return HasPermissionInternal(userPermissions, permission);
    }
    
    public async Task<bool> HasAnyPermissionAsync(Guid userId, params string[] permissions)
    {
        var userPermissions = await GetUserPermissionsAsync(userId);
        return permissions.Any(p => HasPermissionInternal(userPermissions, p));
    }
    
    public async Task<bool> HasAllPermissionsAsync(Guid userId, params string[] permissions)
    {
        var userPermissions = await GetUserPermissionsAsync(userId);
        return permissions.All(p => HasPermissionInternal(userPermissions, p));
    }
    
    public async Task InvalidateCacheAsync(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);
        _cache.Remove(cacheKey);
        _logger.LogInformation("Invalidated permission cache for user {UserId}", userId);
    }
    
    public async Task<HashSet<string>> GetUserPermissionsAsync(Guid userId)
    {
        var profile = await GetSecurityProfileAsync(userId);
        if (profile == null)
        {
            return new HashSet<string>();
        }
        
        // Get permissions from all roles
        var allPermissions = new HashSet<string>();
        
        // Add permissions directly assigned to user
        foreach (var permissionId in profile.PermissionIds)
        {
            // Permissions can be stored as either:
            // 1. Direct permission strings (e.g., "admin", "admin.users.*")
            // 2. GUIDs that reference Permission entities
            
            if (Guid.TryParse(permissionId, out var permId))
            {
                // It's a GUID - look up the Permission entity
                var permission = await GetPermissionById(permId);
                if (permission != null)
                {
                    // Add the permission resource and all its actions
                    allPermissions.Add(permission.Resource);
                    foreach (var action in permission.Actions)
                    {
                        allPermissions.Add($"{permission.Resource}.{action}");
                    }
                }
            }
            else
            {
                // It's a direct permission string
                allPermissions.Add(permissionId);
            }
        }
        
        // Get permissions from roles
        foreach (var roleId in profile.RoleIds)
        {
            if (Guid.TryParse(roleId, out var roleGuid))
            {
                var rolePermissions = await GetRolePermissions(roleGuid);
                allPermissions.UnionWith(rolePermissions);
            }
        }
        
        return allPermissions;
    }
    
    private bool HasPermissionInternal(HashSet<string> userPermissions, string requiredPermission)
    {
        // Direct match
        if (userPermissions.Contains(requiredPermission))
        {
            return true;
        }
        
        // Wildcard match (user has "*" permission)
        if (userPermissions.Contains("*"))
        {
            return true;
        }
        
        // Hierarchical match (e.g., "admin" grants "admin.users.read")
        var parts = requiredPermission.Split('.');
        for (int i = 1; i <= parts.Length; i++)
        {
            var parentPermission = string.Join(".", parts.Take(i));
            if (userPermissions.Contains(parentPermission))
            {
                return true;
            }
            
            // Also check with wildcard (e.g., "admin.*" grants "admin.users.read")
            if (i < parts.Length && userPermissions.Contains($"{parentPermission}.*"))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private async Task<Permission?> GetPermissionById(Guid permissionId)
    {
        var permissions = await _dataContext.Query()
            .WithAll<Permission>(p => p.OwnerEntityId == permissionId)
            .ToEntityComponents();
            
        return permissions.FirstOrDefault().Value?.Get<Permission>();
    }
    
    private async Task<HashSet<string>> GetRolePermissions(Guid roleId)
    {
        var permissions = new HashSet<string>();
        
        var roles = await _dataContext.Query()
            .WithAll<Role>(r => r.OwnerEntityId == roleId)
            .ToEntityComponents();
            
        var role = roles.FirstOrDefault().Value?.Get<Role>();
        if (role == null)
        {
            return permissions;
        }
        
        // Process each permission in the role
        foreach (var permissionId in role.PermissionIds)
        {
            if (Guid.TryParse(permissionId, out var permId))
            {
                var permission = await GetPermissionById(permId);
                if (permission != null)
                {
                    // Add the permission resource and all its actions
                    permissions.Add(permission.Resource);
                    foreach (var action in permission.Actions)
                    {
                        permissions.Add($"{permission.Resource}.{action}");
                    }
                }
            }
        }
        
        return permissions;
    }
    
    private string GetCacheKey(Guid userId) => $"security_profile:{userId}";
}