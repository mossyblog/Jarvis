# ADR: Permission System Architecture - Eliminating Aggregation Duplication

## Title
Permission Aggregation Architecture Refactoring

## Status
Proposed

## Context

### Problem Statement
The current permission system stores permission data redundantly in three places:

1. **Permission components** (source of truth) - defines what permissions exist
2. **Role.PermissionIds[]** - which permissions are assigned to each role
3. **SecurityProfile.PermissionIds[]** - denormalized cache of user's permissions

This creates several problems:

### DRY Violations
Permission aggregation logic (iterating roles, collecting permission IDs) is duplicated in:
- `AccountProfileHandler.CreateWithDefaults()` (lines 49-58)
- `AccountProfileHandler.AssignRole()` (lines 96-119)
- `AccountProfileHandler.RemoveRole()` (lines 158-181)
- `PermissionService.GetUserPermissionsAsync()` (lines 121-171)

### Staleness Bugs
When a Role's permissions are updated via `RoleHandler.GrantPermission()` or `RevokePermission()`, only the in-memory cache is invalidated. The persisted `SecurityProfile.PermissionIds[]` remains stale until the next user profile modification.

### Consistency Issues
`PermissionService.GetUserPermissionsAsync()` reads from BOTH the denormalized `SecurityProfile.PermissionIds` AND recalculates from roles, creating redundant computation and potential inconsistencies.

### Cache Confusion
The system maintains two caching layers:
- In-memory cache (IMemoryCache in PermissionService with 5-minute TTL)
- Persistent denormalization (SecurityProfile.PermissionIds in database)

This dual-caching approach adds complexity without clear benefit.

## Decision Options Evaluated

### Option A: Remove Denormalization
**Delete `SecurityProfile.PermissionIds`, always calculate permissions from roles at runtime.**

**Pros:**
- Eliminates staleness bugs entirely
- Single source of truth (Role -> Permission)
- Simplifies data model
- Removes DRY violations in write paths

**Cons:**
- Increased query complexity (must join User -> Roles -> Permissions)
- Higher database load for frequent permission checks
- Requires careful caching strategy

### Option B: Event-Driven Recalculation
**Emit domain events on Role changes, recalculate affected SecurityProfiles asynchronously.**

**Pros:**
- Keeps fast permission lookups via denormalization
- Eventual consistency with clear propagation path
- Leverages existing MediatR infrastructure
- Can use existing `IEventEmitter` pattern

**Cons:**
- Adds eventual consistency complexity
- Requires tracking which users have which roles
- Race conditions possible during propagation
- More complex debugging

### Option C: Extract PermissionAggregator Service
**Single service responsible for ALL permission calculation, called from all write paths.**

**Pros:**
- Eliminates DRY violations
- Centralized business logic
- Easy to test in isolation
- Clear ownership of aggregation logic

**Cons:**
- Still requires manual calls from each write location
- Doesn't solve staleness if call is forgotten
- Adds another service layer

### Option D: Treat as Read-Only Cache
**Keep denormalized data but NEVER read it for auth checks. Use only for UI/display.**

**Pros:**
- Minimal code changes
- Denormalized data available for reporting/UI
- Auth always uses fresh calculation

**Cons:**
- Wastes storage on unused data
- Confusing to maintain
- Doesn't eliminate DRY violations in write paths

## Decision

**RECOMMENDED: Hybrid approach combining Options A and C**

1. **Remove `SecurityProfile.PermissionIds[]`** from the data model entirely
2. **Extract `IPermissionAggregator` service** that encapsulates all permission calculation logic
3. **Rely solely on in-memory caching** (existing IMemoryCache with appropriate TTL)
4. **Emit MediatR events** on Role changes for cache invalidation (not data propagation)

### Rationale

The current `SecurityProfile.PermissionIds` serves no useful purpose because:
1. `PermissionService.GetUserPermissionsAsync()` already recalculates from roles anyway (lines 160-168)
2. The denormalized data becomes stale when roles change
3. The memory cache provides sufficient performance optimization

By removing the persistent denormalization and centralizing aggregation logic, we:
- Eliminate the staleness bug entirely
- Remove the DRY violations
- Simplify the data model
- Reduce confusion about which data is authoritative

## Consequences

### Positive
- **Single source of truth**: Permissions are ONLY defined in Permission entities and Role.PermissionIds
- **No staleness**: Permission changes immediately visible after cache invalidation
- **DRY compliance**: Aggregation logic exists in exactly one place
- **Simpler debugging**: No need to compare denormalized vs calculated values
- **Reduced storage**: No longer storing redundant permission arrays per user

### Negative
- **Schema migration required**: Must remove `PermissionIds` column from SecurityProfile table
- **Slight performance increase on cache miss**: Must query roles and permissions (mitigated by caching)
- **Breaking change**: Any code directly reading `SecurityProfile.PermissionIds` must be updated

### Neutral
- Memory cache behavior unchanged (already in place)
- No changes to Permission or Role models
- Existing tests for hierarchical/wildcard permissions remain valid

## Implementation Plan

### Phase 1: Extract PermissionAggregator (Non-Breaking)
1. Create `IPermissionAggregator` interface
2. Implement `PermissionAggregator` service with single `AggregatePermissionsForUser(Guid userId)` method
3. Refactor `PermissionService.GetUserPermissionsAsync()` to use the new aggregator
4. Add unit tests for the aggregator

### Phase 2: Remove Denormalization from Write Paths (Non-Breaking)
1. Modify `AccountProfileHandler.CreateWithDefaults()` to NOT populate PermissionIds
2. Modify `AccountProfileHandler.AssignRole()` to NOT recalculate PermissionIds
3. Modify `AccountProfileHandler.RemoveRole()` to NOT recalculate PermissionIds
4. Verify existing tests still pass

### Phase 3: Remove PermissionIds from SecurityProfile (Breaking)
1. Create database migration to remove `permission_ids` column
2. Update `SecurityProfile` model to remove `PermissionIds` property
3. Update any remaining code that references `SecurityProfile.PermissionIds`
4. Run full test suite

### Phase 4: Optimize Cache Invalidation
1. Ensure `RoleHandler` operations properly invalidate affected user caches
2. Consider adding MediatR event handlers for cleaner cache invalidation
3. Add telemetry for cache hit/miss rates

## Critical Files for Implementation

| File | Change |
|------|--------|
| `core.jarvis.api/Models/SecurityProfile.cs` | Remove PermissionIds property |
| `core.jarvis.api/Services/PermissionService.cs` | Refactor to use aggregator |
| `core.jarvis.api/Handlers/AccountProfileHandler.cs` | Remove permission aggregation logic |
| `core.jarvis.api/Services/IPermissionAggregator.cs` | New interface |
| `core.jarvis.api/Services/PermissionAggregator.cs` | New implementation |
| `core.jarvis.api/Handlers/RoleHandler.cs` | Verify cache invalidation |
