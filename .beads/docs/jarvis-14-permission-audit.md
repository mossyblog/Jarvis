# COMPREHENSIVE PERMISSION DATA FLOWS AUDIT - jarvis-14

## 1. DATA FLOW DIAGRAM

```
┌─────────────────────────────────────────────────────────────────┐
│                    PERMISSION DATA FLOW ARCHITECTURE             │
└─────────────────────────────────────────────────────────────────┘

                         REQUEST ENTRY POINT
                                 │
                                 ▼
                    AuthorizationMiddleware
                    (L: 49-149, 126-144)
                         │
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
    Token Validation  Load from      Check Permission
                    PermissionService  Requirements
                                │
                    ┌───────────┼───────────┐
                    ▼           ▼           ▼
            [CACHE HIT]  [CACHE MISS]  [DB QUERY]
            Return Cached  Load from DB  Load SecurityProfile
            Profile        (PermissionService:
                          GetSecurityProfileAsync)
                                │
                    ┌───────────┼───────────┐
                    ▼           ▼           ▼
            Load from      Expand from    Build Permission
            Database       Role IDs       Set
                                │
                    ┌───────────┴───────────┐
                    ▼                       ▼
            Direct Permissions         Role Permissions
            (PermissionIds array)       (via Role → Permission)
            L: SecurityProfile:33              │
                                    ┌─────────┼─────────┐
                                    ▼         ▼         ▼
                            Query Role    Query Permission  Union
                            (RoleHandler)  (PermissionHandler)
                                        │
                          ┌─────────────┴─────────────┐
                          ▼                           ▼
                    Permission.Resource        Permission.Actions
                    + Permission.Actions       (expanded as strings)

        ┌─────────────────────────────────────────────────┐
        │         CACHE MANAGEMENT LIFECYCLE              │
        └─────────────────────────────────────────────────┘

        INVALIDATION TRIGGERS:
        ├─ AssignRole() → InvalidateCacheAsync
        ├─ RemoveRole() → InvalidateCacheAsync
        ├─ UpdateProfile() → InvalidateCacheAsync
        ├─ GrantPermission() → InvalidateCacheForRoleAsync
        ├─ RevokePermission() → InvalidateCacheForRoleAsync
        ├─ UpdateRole() → InvalidateCacheForRoleAsync
        └─ DeleteRole() → InvalidateCacheForRoleAsync
```

## 2. COMPLETE DATA FLOW TABLE

| Location | File | Line(s) | Operation Type | Data | Direction |
|----------|------|---------|-----------------|------|-----------|
| **STORAGE LOCATIONS** | | | | | |
| SecurityProfile Creation | AccountProfileHandler.cs | 63-71 | STORE | PermissionIds[] (denormalized) | → DB |
| SecurityProfile Creation | RegistrationSystem.cs | 87-88 | STORE | Initial profile with default permissions | → DB |
| SecurityProfile Role Assignment | AccountProfileHandler.cs | 121-126 | STORE | RoleIds[], PermissionIds[] updated | → DB |
| SecurityProfile Role Removal | AccountProfileHandler.cs | 183-188 | STORE | RoleIds[], PermissionIds[] updated | → DB |
| SecurityProfile Update | AccountProfileHandler.cs | 214-221 | STORE | Name, Avatar, Preferences | → DB |
| Role Creation | RoleHandler.cs | 118-126 | STORE | PermissionIds[] array | → DB |
| Role Permission Grant | RoleHandler.cs | 49-55 | STORE | PermissionIds[] with new permission added | → DB |
| Role Permission Revoke | RoleHandler.cs | 89-95 | STORE | PermissionIds[] with permission removed | → DB |
| Role Update | RoleHandler.cs | 146-152 | STORE | PermissionIds[], Name, Description | → DB |
| Permission Creation | PermissionHandler.cs | (implicit via base) | STORE | Resource, Actions[] | → DB |
| Default Role Setup | SystemSetupHandler.cs | 148-168 | STORE | Roles with empty PermissionIds | → DB |
| **READ LOCATIONS** | | | | | |
| Initial Auth Check | AuthorizationMiddleware.cs | 128-129 | READ | SecurityProfile + UserPermissions | ← DB/Cache |
| Permission Check (HasPermission) | PermissionService.cs | 73-77 | READ | UserPermissions HashSet | ← Memory |
| Permission Check (HasAnyPermission) | PermissionService.cs | 79-83 | READ | UserPermissions HashSet | ← Memory |
| Permission Check (HasAllPermissions) | PermissionService.cs | 85-89 | READ | UserPermissions HashSet | ← Memory |
| Get Security Profile | PermissionService.cs | 39-71 | READ | SecurityProfile from cache or DB | ← Cache/DB |
| Get User Permissions | PermissionService.cs | 121-171 | READ | PermissionIds[], RoleIds[] from SecurityProfile | ← Cache/DB |
| Get User Profile | AccountFunction.cs | 62-63 | READ | SecurityProfile component | ← Cache/DB |
| Get User Navigation | AccountFunction.cs | 127-131 | READ | SecurityProfile (for navigation filtering) | ← Cache/DB |
| Get Role Permissions | PermissionService.cs | 216-249 | READ | Role.PermissionIds[] | ← DB |
| Get Permission Details | PermissionService.cs | 207-214 | READ | Permission.Resource, Permission.Actions | ← DB |
| Check Permission by Action | PermissionHandler.cs | 25-29 | READ | Permission.Actions array | ← DB |
| Get All Roles | SystemSetupHandler.cs | 124-141 | READ | All roles | ← DB |
| Get User Navigation Items | NavigationHandler.cs | 26-48 | READ | userPermissionIds[] from SecurityProfile | ← Memory |
| **AGGREGATION LOCATIONS** | | | | | |
| AssignRole - Aggregate Permissions | AccountProfileHandler.cs | 97-119 | AGGREGATE | Union all role permissions into HashSet | Memory |
| RemoveRole - Aggregate Permissions | AccountProfileHandler.cs | 159-181 | AGGREGATE | Union remaining role permissions into HashSet | Memory |
| GetUserPermissions - Direct + Role | PermissionService.cs | 121-171 | AGGREGATE | Union direct permissions + expanded role permissions | Memory |
| GetRolePermissions - Expand Entities | PermissionService.cs | 216-249 | AGGREGATE | Expand Permission entities into strings | Memory |
| HasPermissionInternal - Hierarchical | PermissionService.cs | 173-205 | AGGREGATE | Check direct + wildcard + hierarchical matches | Memory |
| NavigationHandler - Permission Filter | NavigationHandler.cs | 185-196 | AGGREGATE | Filter nav items by user permission IDs | Memory |
| **CACHING LOCATIONS** | | | | | |
| Cache Set - Profile | PermissionService.cs | 62 | CACHE-WRITE | SecurityProfile → IMemoryCache | ← Memory |
| Cache Retrieval - Profile | PermissionService.cs | 44-47 | CACHE-READ | SecurityProfile from IMemoryCache | ← Memory |
| Cache Key Generation | PermissionService.cs | 251 | CACHE-KEY | Format: "security_profile:{userId}" | Memory |
| Cache TTL Configuration | PermissionService.cs | 35-36 | CACHE-CONFIG | Default 5 minutes (from config) | Memory |
| **INVALIDATION LOCATIONS** | | | | | |
| Invalidate User Cache | AccountProfileHandler.cs | 134 | INVALIDATE | Remove security_profile:{userId} from cache | Memory |
| Invalidate User Cache | AccountProfileHandler.cs | 196 | INVALIDATE | Remove security_profile:{userId} from cache | Memory |
| Invalidate User Cache | AccountProfileHandler.cs | 228 | INVALIDATE | Remove security_profile:{userId} from cache | Memory |
| Invalidate Role Cache | RoleHandler.cs | 63 | INVALIDATE | Find all users with role, clear their caches | Memory |
| Invalidate Role Cache | RoleHandler.cs | 103 | INVALIDATE | Find all users with role, clear their caches | Memory |
| Invalidate Role Cache | RoleHandler.cs | 160 | INVALIDATE | Find all users with role, clear their caches | Memory |
| Invalidate Role Cache | RoleHandler.cs | 176 | INVALIDATE | Find all users with role, clear their caches (before delete) | Memory |
| Invalidate Cache for Role | PermissionService.cs | 98-119 | INVALIDATE | Query all profiles, find users with role, remove caches | Memory |
| Manual Cache Removal | PermissionService.cs | 91-96 | INVALIDATE | _cache.Remove(cacheKey) | Memory |

## 3. IDENTIFIED ISSUES AND CONCERNS

### A. REDUNDANCY ISSUES

1. **Denormalized Data in SecurityProfile (HIGH CONCERN)**
   - Location: SecurityProfile.cs:33 - PermissionIds array
   - Issue: Permission IDs are stored in TWO places:
     - Directly in SecurityProfile.PermissionIds
     - Derived from SecurityProfile.RoleIds → Role.PermissionIds
   - Risk: These can become out-of-sync if only one is updated
   - Current Mitigation: Code always recalculates from roles (AccountProfileHandler.cs:97-119, 159-181)
   - Problem: Direct PermissionIds are not being maintained consistently

2. **Duplicate Permission Aggregation Logic**
   - Location: PermissionService.cs:121-171 (GetUserPermissions)
   - vs: AccountProfileHandler.cs:97-119 (AssignRole aggregation)
   - vs: AccountProfileHandler.cs:159-181 (RemoveRole aggregation)
   - Issue: Permission aggregation logic appears in three places
   - Problem: Changes to aggregation logic must be synced in three locations

3. **Duplicate Query Patterns for Roles**
   - Location: AccountProfileHandler.cs:104-106 (AssignRole)
   - Location: AccountProfileHandler.cs:166-168 (RemoveRole)
   - Issue: Identical role lookup pattern repeated verbatim

### B. STALENESS RISKS

1. **Denormalized SecurityProfile.PermissionIds (CRITICAL)**
   - Location: SecurityProfile.cs:33
   - Issue: Comments state "denormalized from roles for fast permission checks" but it's never actually used
   - Current Flow: Always reads from roles (PermissionService.cs:161-168)
   - Risk: Denormalized field becomes stale and misleading
   - Recommendation: Either use it or remove it

2. **Cache Invalidation Completeness (MEDIUM)**
   - Location: InvalidateCacheForRoleAsync (PermissionService.cs:98-119)
   - Issue: Requires full table scan of all SecurityProfiles to find users with a role
   - Risk: If code path that updates role permissions bypasses handler, cache won't be invalidated

### C. DRY VIOLATIONS

1. **Permission Aggregation Logic (3 instances)**
   - Instance 1: AccountProfileHandler.cs:97-119
   - Instance 2: AccountProfileHandler.cs:159-181
   - Instance 3: PermissionService.cs:121-171

2. **Role Query Pattern (2+ instances)**
   - Pattern repeated in AssignRole and RemoveRole
   - Should be extracted to a helper method

## 4. DEPENDENCIES BETWEEN COMPONENTS

```
DEPENDENCY GRAPH:

AuthorizationMiddleware
    │
    ├─► PermissionService.GetSecurityProfileAsync()
    │   └─► IMemoryCache (read)
    │       └─► DataContext.Query<SecurityProfile>()
    │
    ├─► PermissionService.GetUserPermissionsAsync()
    │   ├─► PermissionService.GetSecurityProfileAsync()
    │   ├─► PermissionService.GetRolePermissions()
    │   │   └─► DataContext.Query<Role>()
    │   │       └─► PermissionService.GetPermissionById()
    │   │           └─► DataContext.Query<Permission>()
    │   └─► PermissionService.HasPermissionInternal()
    │
    └─► Attribute-based checks via reflection

AccountProfileHandler.AssignRole()/RemoveRole()/UpdateProfile()
    │
    ├─► DataContext.Query<Role>()
    │   └─► Manual permission aggregation (union logic)
    │
    └─► PermissionService.InvalidateCacheAsync()
        └─► IMemoryCache.Remove()

RoleHandler.GrantPermission()/RevokePermission()/UpdateRole()/DeleteRole()
    │
    └─► PermissionService.InvalidateCacheForRoleAsync()
        └─► DataContext.Query<SecurityProfile>() (find affected users)
            └─► IMemoryCache.Remove() (one per affected user)
```

## 5. SUMMARY FOR jarvis-15

Key tensions:
1. **Denormalization Strategy**: SecurityProfile stores both RoleIds AND PermissionIds, but code only uses RoleIds
2. **Resolution Complexity**: Permission resolution happens at read-time by expanding Roles → Permissions
3. **Invalidation Cost**: Role-level changes require full table scan to find affected users
4. **DRY Violations**: Permission aggregation logic scattered across three locations

Recommended approach: Remove `SecurityProfile.PermissionIds` entirely since it's unused and creates confusion.
