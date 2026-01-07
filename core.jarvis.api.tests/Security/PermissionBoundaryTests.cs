using core.jarvis.api.Services;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Security;

/// <summary>
/// INTENT: Penetration tests for permission boundary enforcement
/// PURPOSE: Verify that the permission system cannot be bypassed after denormalization removal
/// BUSINESS CONTEXT: Security-critical validation that permissions are properly enforced
/// WHY IMPORTANT: Prevents unauthorized access through permission manipulation
/// ARCHITECTURAL SIGNIFICANCE: Validates the jarvis-15 ADR implementation (no denormalized PermissionIds)
/// FUTURE RESILIENCE: Ensures permission boundaries remain intact as system evolves
/// </summary>
public class PermissionBoundaryTests : ApiIntegrationTestBase
{
    private IPermissionService _permissionService = null!;
    private IPasswordPolicyService _passwordService = null!;

    public PermissionBoundaryTests()
    {
        _permissionService = _serviceProvider.GetRequiredService<IPermissionService>();
        _passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
    }

    #region Test 1: User cannot access resources without required permissions

    /// <summary>
    /// INTENT: Verify that a user with NO roles has NO permissions
    /// PURPOSE: Ensure permission checks return false for users without any role assignments
    /// BUSINESS CONTEXT: New users or stripped users must have zero privileges
    /// WHY IMPORTANT: Prevents privilege escalation for users without proper role assignment
    /// ARCHITECTURAL SIGNIFICANCE: Validates the base case for permission enforcement
    /// FUTURE RESILIENCE: Ensures default-deny security posture
    /// </summary>
    [Fact]
    public async Task User_WithNoRoles_HasNoPermissions()
    {
        // Arrange - Create user with no roles
        var email = $"no_roles_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        // Create profile with EMPTY role array (no roles assigned)
        var emptyRolesProfile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = Array.Empty<string>(), // Explicitly no roles
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(emptyRolesProfile);
        TrackEntity(account.OwnerEntityId);

        // Invalidate cache to ensure fresh data
        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Act - Check various permission types
        var hasAdminPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin");
        var hasUsersRead = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "users.read");
        var hasApiAccess = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "api.access");
        var hasWildcard = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "*");
        var hasRandomPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "some.random.permission");

        // Assert - ALL permission checks must return false
        hasAdminPermission.ShouldBeFalse("User with no roles must not have admin permission");
        hasUsersRead.ShouldBeFalse("User with no roles must not have users.read permission");
        hasApiAccess.ShouldBeFalse("User with no roles must not have api.access permission");
        hasWildcard.ShouldBeFalse("User with no roles must not have wildcard permission");
        hasRandomPermission.ShouldBeFalse("User with no roles must not have any random permission");
    }

    /// <summary>
    /// INTENT: Verify that GetUserPermissionsAsync returns empty set for roleless user
    /// PURPOSE: Ensure the full permission resolution returns nothing for users without roles
    /// BUSINESS CONTEXT: Validates that permission aggregation works correctly at the boundary case
    /// WHY IMPORTANT: Confirms no phantom permissions leak through the system
    /// ARCHITECTURAL SIGNIFICANCE: Tests the permission resolution path end-to-end
    /// FUTURE RESILIENCE: Ensures clean slate for new user permission checks
    /// </summary>
    [Fact]
    public async Task User_WithNoRoles_GetUserPermissionsAsync_ReturnsEmptySet()
    {
        // Arrange - Create user with no roles
        var email = $"empty_perms_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        var emptyRolesProfile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = Array.Empty<string>(),
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(emptyRolesProfile);
        TrackEntity(account.OwnerEntityId);

        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Act
        var permissions = await _permissionService.GetUserPermissionsAsync(account.OwnerEntityId);

        // Assert
        permissions.ShouldNotBeNull("Permissions set should never be null");
        permissions.Count.ShouldBe(0, "User with no roles should have exactly zero permissions");
    }

    #endregion

    #region Test 2: Permissions only come from assigned roles

    /// <summary>
    /// INTENT: Verify that user only has permissions from their assigned role
    /// PURPOSE: Ensure permissions are properly scoped to assigned roles
    /// BUSINESS CONTEXT: Role-based access control must grant exactly the right permissions
    /// WHY IMPORTANT: Prevents cross-role permission leakage
    /// ARCHITECTURAL SIGNIFICANCE: Validates the core RBAC implementation
    /// FUTURE RESILIENCE: Ensures role assignments remain the single source of truth
    /// </summary>
    [Fact]
    public async Task User_WithRoleA_HasOnlyRoleAPermissions()
    {
        // Arrange
        var email = $"role_a_user_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        // Create Permission P1 (documents.read, documents.write)
        var permissionP1 = new Permission
        {
            OwnerEntityId = Guid.NewGuid(),
            Resource = "documents",
            Actions = new[] { "read", "write" }
        };
        await TestDataContext().Commit(permissionP1);

        // Create Permission P2 (reports.view)
        var permissionP2 = new Permission
        {
            OwnerEntityId = Guid.NewGuid(),
            Resource = "reports",
            Actions = new[] { "view" }
        };
        await TestDataContext().Commit(permissionP2);

        // Create Role A with permissions P1 and P2
        var roleA = new Role
        {
            OwnerEntityId = Guid.NewGuid(),
            Name = $"RoleA_{Guid.NewGuid()}",
            Description = "Test role A with P1 and P2",
            PermissionIds = new[]
            {
                permissionP1.OwnerEntityId.ToString(),
                permissionP2.OwnerEntityId.ToString()
            }
        };
        await TestDataContext().Commit(roleA);

        // Create user profile with Role A
        var profile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { roleA.OwnerEntityId.ToString() },
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(profile);
        TrackEntity(account.OwnerEntityId);

        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Act - Check permissions the user SHOULD have (from Role A)
        var hasDocumentsRead = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "documents.read");
        var hasDocumentsWrite = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "documents.write");
        var hasReportsView = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "reports.view");

        // Check permissions the user should NOT have (P3 never created or assigned)
        var hasAdminPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin");
        var hasUsersDelete = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "users.delete");
        var hasSystemConfig = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "system.config");

        // Assert - User has exactly the permissions from Role A
        hasDocumentsRead.ShouldBeTrue("User with Role A should have documents.read (from P1)");
        hasDocumentsWrite.ShouldBeTrue("User with Role A should have documents.write (from P1)");
        hasReportsView.ShouldBeTrue("User with Role A should have reports.view (from P2)");

        // Assert - User does NOT have permissions not in Role A
        hasAdminPermission.ShouldBeFalse("User should not have admin permission (not in Role A)");
        hasUsersDelete.ShouldBeFalse("User should not have users.delete permission (not in Role A)");
        hasSystemConfig.ShouldBeFalse("User should not have system.config permission (not in Role A)");
    }

    /// <summary>
    /// INTENT: Verify HasAllPermissionsAsync correctly validates multiple permissions
    /// PURPOSE: Ensure AND logic works correctly for permission subsets
    /// BUSINESS CONTEXT: Some operations require multiple permissions simultaneously
    /// WHY IMPORTANT: Validates combined permission checks don't grant extra privileges
    /// ARCHITECTURAL SIGNIFICANCE: Tests the AND permission logic implementation
    /// FUTURE RESILIENCE: Ensures multi-permission checks remain accurate
    /// NOTE: When a Permission grants a Resource, it hierarchically grants all Resource.* actions.
    ///       To test AND logic, we must use COMPLETELY DIFFERENT domains (e.g., files vs reporting).
    /// </summary>
    [Fact]
    public async Task User_WithPartialPermissions_FailsHasAllPermissionsCheck()
    {
        // Arrange
        var email = $"partial_perms_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        // Create permission for files domain (grants files.read)
        var filesPermission = new Permission
        {
            OwnerEntityId = Guid.NewGuid(),
            Resource = "fileaccess",  // Using specific resource to avoid hierarchical expansion
            Actions = new[] { "read" }
        };
        await TestDataContext().Commit(filesPermission);

        // NOTE: We do NOT create a permission for "reporting" domain
        // User should have fileaccess.read but NOT reporting.analyze

        // Create role with only the files permission
        var readerRole = new Role
        {
            OwnerEntityId = Guid.NewGuid(),
            Name = $"ReaderRole_{Guid.NewGuid()}",
            Description = "Files read-only role",
            PermissionIds = new[] { filesPermission.OwnerEntityId.ToString() }
        };
        await TestDataContext().Commit(readerRole);

        var profile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { readerRole.OwnerEntityId.ToString() },
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(profile);
        TrackEntity(account.OwnerEntityId);

        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Act - User has fileaccess.read but NOT reporting.analyze (completely different domain)
        var hasAllBothDomains = await _permissionService.HasAllPermissionsAsync(
            account.OwnerEntityId,
            "fileaccess.read",      // User HAS this
            "reporting.analyze"     // User does NOT have this - different domain entirely
        );
        var hasAllFileAccessOnly = await _permissionService.HasAllPermissionsAsync(
            account.OwnerEntityId,
            "fileaccess.read"
        );

        // Assert
        hasAllBothDomains.ShouldBeFalse("HasAllPermissionsAsync must fail when user lacks ANY of the required permissions (reporting.analyze not granted)");
        hasAllFileAccessOnly.ShouldBeTrue("HasAllPermissionsAsync should pass when user has all required permissions");
    }

    #endregion

    #region Test 3: Role removal immediately removes permissions

    /// <summary>
    /// INTENT: Verify that removing a role immediately removes its permissions (after cache invalidation)
    /// PURPOSE: Ensure role removal is effective and not cached improperly
    /// BUSINESS CONTEXT: Security revocation must be immediate when admin removes a role
    /// WHY IMPORTANT: Prevents continued access after role removal
    /// ARCHITECTURAL SIGNIFICANCE: Validates the role-permission binding is properly broken
    /// FUTURE RESILIENCE: Ensures permission revocation propagates correctly
    /// </summary>
    [Fact]
    public async Task RoleRemoval_ImmediatelyRemovesPermissions_AfterCacheInvalidation()
    {
        // Arrange
        var email = $"role_removal_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        // Create a permission
        var permission = new Permission
        {
            OwnerEntityId = Guid.NewGuid(),
            Resource = "sensitive",
            Actions = new[] { "access" }
        };
        await TestDataContext().Commit(permission);

        // Create a role with that permission
        var role = new Role
        {
            OwnerEntityId = Guid.NewGuid(),
            Name = $"SensitiveRole_{Guid.NewGuid()}",
            Description = "Role with sensitive access",
            PermissionIds = new[] { permission.OwnerEntityId.ToString() }
        };
        await TestDataContext().Commit(role);

        // Create user profile with the role
        var profile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { role.OwnerEntityId.ToString() },
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(profile);
        TrackEntity(account.OwnerEntityId);

        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Verify user HAS the permission initially
        var hasPermissionBefore = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "sensitive.access");
        hasPermissionBefore.ShouldBeTrue("User should have sensitive.access BEFORE role removal");

        // Act - Remove the role from the user using handler
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.RemoveRole(role.OwnerEntityId);

        // Note: RemoveRole already calls InvalidateCacheAsync internally
        // But we call it again to be explicit about the test requirement
        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Assert - Verify permission is GONE after role removal
        var hasPermissionAfter = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "sensitive.access");
        hasPermissionAfter.ShouldBeFalse("User should NOT have sensitive.access AFTER role removal and cache invalidation");

        // Double-check by getting all permissions
        var allPermissions = await _permissionService.GetUserPermissionsAsync(account.OwnerEntityId);
        allPermissions.ShouldNotContain("sensitive.access", "Permission should not appear in user's permission set after role removal");
    }

    /// <summary>
    /// INTENT: Verify cached permissions become stale without invalidation but correct after invalidation
    /// PURPOSE: Ensure cache invalidation is necessary for permission changes to take effect
    /// BUSINESS CONTEXT: Administrators must understand that cache invalidation is required
    /// WHY IMPORTANT: Documents the expected behavior of the caching system
    /// ARCHITECTURAL SIGNIFICANCE: Validates cache-then-invalidate pattern
    /// FUTURE RESILIENCE: Ensures caching behavior is well-understood and tested
    /// </summary>
    [Fact]
    public async Task Cache_MustBeInvalidated_ForRoleChangesToTakeEffect()
    {
        // Arrange
        var email = $"cache_test_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        var permission = new Permission
        {
            OwnerEntityId = Guid.NewGuid(),
            Resource = "cacheable",
            Actions = new[] { "test" }
        };
        await TestDataContext().Commit(permission);

        var role = new Role
        {
            OwnerEntityId = Guid.NewGuid(),
            Name = $"CacheableRole_{Guid.NewGuid()}",
            Description = "Role for cache testing",
            PermissionIds = new[] { permission.OwnerEntityId.ToString() }
        };
        await TestDataContext().Commit(role);

        var profile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { role.OwnerEntityId.ToString() },
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(profile);
        TrackEntity(account.OwnerEntityId);

        // Prime the cache
        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);
        var initialPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "cacheable.test");
        initialPermission.ShouldBeTrue("Initial permission check should return true");

        // Act - Modify profile directly in database WITHOUT cache invalidation
        var updatedProfile = profile with
        {
            RoleIds = Array.Empty<string>(), // Remove all roles
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(updatedProfile);

        // WITHOUT invalidation - cache should still have old value
        var cachedPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "cacheable.test");
        cachedPermission.ShouldBeTrue("Cached permission should still return true before invalidation");

        // NOW invalidate the cache
        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Assert - After invalidation, permission should be gone
        var freshPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "cacheable.test");
        freshPermission.ShouldBeFalse("After cache invalidation, permission should return false");
    }

    #endregion

    #region Test 4: Role permission changes propagate correctly

    /// <summary>
    /// INTENT: Verify that adding a permission to a role propagates to users after cache invalidation
    /// PURPOSE: Ensure role modifications correctly affect user permissions
    /// BUSINESS CONTEXT: Administrators adding permissions to roles expect users to gain access
    /// WHY IMPORTANT: Validates the role-to-user permission propagation chain
    /// ARCHITECTURAL SIGNIFICANCE: Tests the live permission resolution (no denormalized PermissionIds)
    /// FUTURE RESILIENCE: Ensures role modifications work as expected in the new architecture
    /// </summary>
    [Fact]
    public async Task RolePermissionChange_PropagatesAfterCacheInvalidation()
    {
        // Arrange
        var email = $"role_change_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        // Create initial permission P1
        var permissionP1 = new Permission
        {
            OwnerEntityId = Guid.NewGuid(),
            Resource = "initial",
            Actions = new[] { "access" }
        };
        await TestDataContext().Commit(permissionP1);

        // Create new permission P3 (to be added later)
        var permissionP3 = new Permission
        {
            OwnerEntityId = Guid.NewGuid(),
            Resource = "newfeature",
            Actions = new[] { "use" }
        };
        await TestDataContext().Commit(permissionP3);

        // Create role with only P1 initially
        var role = new Role
        {
            OwnerEntityId = Guid.NewGuid(),
            Name = $"EvolvingRole_{Guid.NewGuid()}",
            Description = "Role that will gain P3",
            PermissionIds = new[] { permissionP1.OwnerEntityId.ToString() }
        };
        await TestDataContext().Commit(role);

        // Create user with this role
        var profile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { role.OwnerEntityId.ToString() },
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(profile);
        TrackEntity(account.OwnerEntityId);

        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Verify initial state - has P1, not P3
        var hasP1Before = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "initial.access");
        var hasP3Before = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "newfeature.use");
        hasP1Before.ShouldBeTrue("User should have initial.access from P1");
        hasP3Before.ShouldBeFalse("User should NOT have newfeature.use before P3 is added to role");

        // Act - Add permission P3 to the role using RoleHandler
        var roleHandler = TestDataContext().For<RoleHandler>(role.OwnerEntityId);
        await roleHandler.GrantPermission(permissionP3.OwnerEntityId);

        // RoleHandler.GrantPermission already invalidates cache for the role
        // But we explicitly invalidate for the user to ensure fresh data
        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Assert - User now has P3
        var hasP3After = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "newfeature.use");
        hasP3After.ShouldBeTrue("User should now have newfeature.use after P3 was added to role and cache invalidated");

        // Verify P1 is still there
        var hasP1After = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "initial.access");
        hasP1After.ShouldBeTrue("User should still have initial.access from P1");
    }

    /// <summary>
    /// INTENT: Verify that revoking a permission from a role propagates to users
    /// PURPOSE: Ensure permission revocation at the role level affects all users with that role
    /// BUSINESS CONTEXT: Security teams revoking permissions expect immediate effect
    /// WHY IMPORTANT: Prevents continued access to revoked permissions
    /// ARCHITECTURAL SIGNIFICANCE: Tests the inverse operation of permission granting
    /// FUTURE RESILIENCE: Ensures permission revocation path works correctly
    /// </summary>
    [Fact]
    public async Task RolePermissionRevocation_PropagatesAfterCacheInvalidation()
    {
        // Arrange
        var email = $"revoke_test_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        var permission = new Permission
        {
            OwnerEntityId = Guid.NewGuid(),
            Resource = "revocable",
            Actions = new[] { "access" }
        };
        await TestDataContext().Commit(permission);

        var role = new Role
        {
            OwnerEntityId = Guid.NewGuid(),
            Name = $"RevocableRole_{Guid.NewGuid()}",
            Description = "Role that will lose permission",
            PermissionIds = new[] { permission.OwnerEntityId.ToString() }
        };
        await TestDataContext().Commit(role);

        var profile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { role.OwnerEntityId.ToString() },
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(profile);
        TrackEntity(account.OwnerEntityId);

        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Verify user has permission initially
        var hasBefore = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "revocable.access");
        hasBefore.ShouldBeTrue("User should have revocable.access before revocation");

        // Act - Revoke the permission from the role
        var roleHandler = TestDataContext().For<RoleHandler>(role.OwnerEntityId);
        await roleHandler.RevokePermission(permission.OwnerEntityId);

        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Assert - User no longer has permission
        var hasAfter = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "revocable.access");
        hasAfter.ShouldBeFalse("User should NOT have revocable.access after permission was revoked from role");
    }

    #endregion

    #region Test 5: Cannot bypass permission checks with direct DB manipulation

    /// <summary>
    /// INTENT: Verify that SecurityProfile has NO PermissionIds field to manipulate
    /// PURPOSE: Ensure denormalization was properly removed per jarvis-15 ADR
    /// BUSINESS CONTEXT: Attackers cannot inject permissions directly on user profiles
    /// WHY IMPORTANT: Validates the architectural decision to remove denormalized permissions
    /// ARCHITECTURAL SIGNIFICANCE: Confirms jarvis-15 implementation is correct
    /// FUTURE RESILIENCE: Prevents future re-introduction of denormalized permission fields
    /// </summary>
    [Fact]
    public void SecurityProfile_HasNoPermissionIdsField()
    {
        // Arrange
        var profileType = typeof(SecurityProfile);

        // Act - Check for properties that could be used to bypass permission checks
        var permissionIdsProperty = profileType.GetProperty("PermissionIds");
        var permissionsProperty = profileType.GetProperty("Permissions");
        var directPermissionsProperty = profileType.GetProperty("DirectPermissions");

        // Assert - None of these should exist
        permissionIdsProperty.ShouldBeNull("SecurityProfile must NOT have a PermissionIds property - permissions come only from roles");
        permissionsProperty.ShouldBeNull("SecurityProfile must NOT have a Permissions property");
        directPermissionsProperty.ShouldBeNull("SecurityProfile must NOT have a DirectPermissions property");

        // Verify RoleIds exists (this IS the correct field)
        var roleIdsProperty = profileType.GetProperty("RoleIds");
        roleIdsProperty.ShouldNotBeNull("SecurityProfile must have RoleIds property for role-based permissions");
    }

    /// <summary>
    /// INTENT: Verify permissions are computed from roles, not stored on profile
    /// PURPOSE: Ensure the permission resolution always goes through the role lookup
    /// BUSINESS CONTEXT: Permissions must be dynamic based on current role definitions
    /// WHY IMPORTANT: Confirms single source of truth for permissions (roles)
    /// ARCHITECTURAL SIGNIFICANCE: Validates the runtime permission computation model
    /// FUTURE RESILIENCE: Ensures permissions cannot be cached in the wrong place
    /// </summary>
    [Fact]
    public async Task Permissions_AreComputedFromRoles_NotStoredOnProfile()
    {
        // Arrange
        var email = $"computed_test_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        var permission = new Permission
        {
            OwnerEntityId = Guid.NewGuid(),
            Resource = "computed",
            Actions = new[] { "verify" }
        };
        await TestDataContext().Commit(permission);

        var role = new Role
        {
            OwnerEntityId = Guid.NewGuid(),
            Name = $"ComputedRole_{Guid.NewGuid()}",
            Description = "Role for computed permission test",
            PermissionIds = new[] { permission.OwnerEntityId.ToString() }
        };
        await TestDataContext().Commit(role);

        var profile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { role.OwnerEntityId.ToString() },
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(profile);
        TrackEntity(account.OwnerEntityId);

        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Act - Get the security profile and verify it has no permissions stored on it
        var fetchedProfile = await _permissionService.GetSecurityProfileAsync(account.OwnerEntityId);

        // Also get the computed permissions
        var computedPermissions = await _permissionService.GetUserPermissionsAsync(account.OwnerEntityId);

        // Assert
        fetchedProfile.ShouldNotBeNull();

        // Verify profile only has RoleIds, not any permission storage
        fetchedProfile.RoleIds.Length.ShouldBeGreaterThan(0, "Profile should have role IDs");

        // Verify permissions ARE computed correctly
        computedPermissions.ShouldContain("computed.verify", "Computed permissions should include role permissions");

        // This is the key assertion - permissions come from computation, not storage
        var hasPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "computed.verify");
        hasPermission.ShouldBeTrue("Permission should be granted via role lookup, not profile storage");
    }

    /// <summary>
    /// INTENT: Verify that adding a fake role ID to profile does not grant permissions
    /// PURPOSE: Ensure invalid/non-existent role IDs are safely ignored
    /// BUSINESS CONTEXT: Attackers injecting fake role IDs should not gain access
    /// WHY IMPORTANT: Prevents privilege escalation via role ID injection
    /// ARCHITECTURAL SIGNIFICANCE: Validates defensive programming in permission resolution
    /// FUTURE RESILIENCE: Ensures system handles invalid data gracefully
    /// </summary>
    [Fact]
    public async Task FakeRoleId_DoesNotGrantPermissions()
    {
        // Arrange
        var email = $"fake_role_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        // Create profile with a FAKE role ID (role that doesn't exist)
        var fakeRoleId = Guid.NewGuid();
        var profile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { fakeRoleId.ToString() }, // This role doesn't exist!
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(profile);
        TrackEntity(account.OwnerEntityId);

        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Act - Try to get permissions
        var permissions = await _permissionService.GetUserPermissionsAsync(account.OwnerEntityId);
        var hasAnyPermission = await _permissionService.HasAnyPermissionAsync(
            account.OwnerEntityId,
            "admin", "users", "system", "api", "*"
        );

        // Assert - No permissions should be granted from a non-existent role
        permissions.Count.ShouldBe(0, "Fake role ID should not grant any permissions");
        hasAnyPermission.ShouldBeFalse("User with fake role ID should not have any standard permissions");
    }

    /// <summary>
    /// INTENT: Verify that invalid GUID format in RoleIds is safely handled
    /// PURPOSE: Ensure malformed role IDs don't cause crashes or unexpected behavior
    /// BUSINESS CONTEXT: Data corruption or injection attempts should be handled gracefully
    /// WHY IMPORTANT: System stability under adverse conditions
    /// ARCHITECTURAL SIGNIFICANCE: Validates input sanitization in permission resolution
    /// FUTURE RESILIENCE: Ensures robust handling of malformed data
    /// </summary>
    [Fact]
    public async Task MalformedRoleId_IsHandledGracefully()
    {
        // Arrange
        var email = $"malformed_role_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email);

        // Create profile with malformed role IDs
        var profile = new SecurityProfile
        {
            OwnerEntityId = account.OwnerEntityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { "not-a-valid-guid", "12345", "", "   " }, // All invalid GUIDs
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(profile);
        TrackEntity(account.OwnerEntityId);

        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Act - This should NOT throw, should return empty permissions
        var permissions = await _permissionService.GetUserPermissionsAsync(account.OwnerEntityId);
        var hasPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "anything");

        // Assert - System should handle gracefully without granting permissions
        permissions.Count.ShouldBe(0, "Malformed role IDs should not grant any permissions");
        hasPermission.ShouldBeFalse("User with malformed role IDs should not have any permissions");
    }

    #endregion

    #region Helper Methods

    private async Task<Account> CreateTestAccount(string email)
    {
        var entityId = Guid.NewGuid();
        var hashedPassword = _passwordService.HashPassword("SecurePassword123!");

        var account = new Account
        {
            OwnerEntityId = entityId,
            Email = email,
            PasswordHash = hashedPassword,
            Password = "", // Not persisted
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(account);
        return account;
    }

    #endregion
}
