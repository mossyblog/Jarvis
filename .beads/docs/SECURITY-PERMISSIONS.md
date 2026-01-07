# Security Permissions System

Reference documentation for the Jarvis permission system (jarvis-12/jarvis-15).

## Permission Architecture

### Hierarchy

```
Permission -> Role -> User (SecurityProfile)
```

- **Permission**: Resource + Actions (e.g., `tables` with `[read, write, delete]`)
- **Role**: Collection of Permission IDs (e.g., `superuser`, `dev`, `default`)
- **SecurityProfile**: Collection of Role IDs assigned to a user

### Key Design Decision

**No denormalized PermissionIds in SecurityProfile.** Permissions are calculated at runtime from RoleIds.

```csharp
// SecurityProfile only stores RoleIds
public string[] RoleIds { get; init; } = Array.Empty<string>();

// Permissions resolved via PermissionService.GetUserPermissionsAsync()
```

## Permission Flow

### Resolution Process

1. `PermissionService.HasPermissionAsync(userId, permission)` called
2. Fetch `SecurityProfile` from cache or database
3. For each `RoleId` in profile, fetch `Role` and its `PermissionIds`
4. For each `PermissionId`, fetch `Permission` and expand to permission strings
5. Check if required permission matches (direct, wildcard, or hierarchical)

### Hierarchical Matching

```
admin           -> grants admin.users.read, admin.users.write, etc.
admin.*         -> grants admin.users.read, admin.settings.write, etc.
*               -> grants everything
```

### Cache Invalidation

| Trigger | Method | Scope |
|---------|--------|-------|
| Role assigned/removed | `InvalidateCacheAsync(userId)` | Single user |
| Profile updated | `InvalidateCacheAsync(userId)` | Single user |
| Permission granted/revoked on role | `InvalidateCacheForRoleAsync(roleId)` | All users with role |
| Role updated/deleted | `InvalidateCacheForRoleAsync(roleId)` | All users with role |

Cache TTL: Configurable via `Jwt:PermissionCacheTTLMinutes` (default: 5 minutes)

## Security Guarantees

1. **Single Source of Truth**: Roles define permissions. No duplicate permission storage.
2. **No Stale Data**: Cache invalidation on every permission-affecting change.
3. **Immediate Effect**: Changes apply immediately after cache invalidation.
4. **Immutable Updates**: All mutations use `record with {}` pattern.

## API Reference

### IPermissionService

```csharp
// Get cached security profile
Task<SecurityProfile?> GetSecurityProfileAsync(Guid userId);

// Permission checks
Task<bool> HasPermissionAsync(Guid userId, string permission);
Task<bool> HasAnyPermissionAsync(Guid userId, params string[] permissions);
Task<bool> HasAllPermissionsAsync(Guid userId, params string[] permissions);

// Get all resolved permissions
Task<HashSet<string>> GetUserPermissionsAsync(Guid userId);

// Cache invalidation
Task InvalidateCacheAsync(Guid userId);
Task InvalidateCacheForRoleAsync(Guid roleId);
```

### AccountProfileHandler

```csharp
// Role management (auto-invalidates cache)
Task<SecurityProfile> AssignRole(Guid roleId);
Task<SecurityProfile> RemoveRole(Guid roleId);

// Profile management
Task<SecurityProfile> CreateWithDefaults(string email, string? fullName = null);
Task<SecurityProfile> UpdateProfile(SecurityProfile updateRequest);
```

### RoleHandler

```csharp
// Permission management (auto-invalidates cache for all users with role)
Task<Role> GrantPermission(Guid permissionId);
Task<Role> RevokePermission(Guid permissionId);

// Role CRUD
Task<Role> CreateRole(Role roleData);
Task<Role> UpdateRole(Role updateData);
Task<bool> DeleteRole();
```

## Data Models

### SecurityProfile
```csharp
public record SecurityProfile : IComponent
{
    public string[] RoleIds { get; init; }  // Only stores role references
    public string Name { get; init; }
    public string? Avatar { get; init; }
    public string Preferences { get; init; }
}
```

### Role
```csharp
public record Role : IComponent
{
    public string Name { get; init; }
    public string Description { get; init; }
    public string[] PermissionIds { get; init; }  // References to Permission entities
}
```

### Permission
```csharp
public record Permission : IComponent
{
    public string Resource { get; init; }      // e.g., "tables", "users"
    public string[] Actions { get; init; }     // e.g., ["read", "write", "delete"]
}
```

Permission strings generated: `{Resource}` and `{Resource}.{Action}` for each action.
