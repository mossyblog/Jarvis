using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.Systems;
using core.jarvis.api.tests.Helpers;
using core.jarvis.Data;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration;

/// <summary>
/// INTENT: Validate the complete permission flow works without denormalized PermissionIds
/// PURPOSE: Ensure end-to-end permission resolution through Role -> Permission chain
/// BUSINESS CONTEXT: Users need proper access control based on role assignments
/// WHY IMPORTANT: Permission system is the foundation of all authorization
/// ARCHITECTURAL SIGNIFICANCE: Validates jarvis-15 ADR - no denormalized PermissionIds on SecurityProfile
/// FUTURE RESILIENCE: Ensures permission changes through roles propagate correctly
/// </summary>
public class PermissionFlowE2ETests : ApiIntegrationTestBase
{
    private readonly IPermissionService _permissionService;
    private readonly RegistrationSystem _registrationSystem;

    public PermissionFlowE2ETests()
    {
        _permissionService = _serviceProvider.GetRequiredService<IPermissionService>();
        _registrationSystem = _serviceProvider.GetRequiredService<RegistrationSystem>();
    }

    #region Scenario 1: Full Registration -> Role -> Permission Flow

    /// <summary>
    /// INTENT: Verify complete flow from user registration to role assignment
    /// PURPOSE: Ensure new users get a default role assigned via CreateWithDefaults
    /// BUSINESS CONTEXT: First-time users must have proper default role access
    /// WHY IMPORTANT: Core onboarding flow must work correctly
    /// ARCHITECTURAL SIGNIFICANCE: Tests RegistrationSystem -> AccountProfileHandler -> PermissionService chain
    /// FUTURE RESILIENCE: Ensures registration flow doesn't break permission resolution
    /// </summary>
    [Fact]
    public async Task Registration_Should_Assign_Default_Role()
    {
        // Act - Register a new user (relies on existing "default" role in the database)
        var email = $"e2e_reg_{Guid.NewGuid()}@example.com";
        var requestBody = System.Text.Json.JsonSerializer.Serialize(new
        {
            email = email,
            password = "SecurePassword123!",
            fullName = "E2E Test User"
        });

        var components = await _registrationSystem.RegisterUser(requestBody, "127.0.0.1");
        var account = components.OfType<Account>().First();
        TrackEntity(account.OwnerEntityId);

        // Assert - Verify user has a profile with at least one role assigned
        var profile = await _permissionService.GetSecurityProfileAsync(account.OwnerEntityId);
        profile.ShouldNotBeNull();

        // The registration flow should have assigned the default role
        // (The actual role ID depends on database state, so we check that a role was assigned)
        profile.RoleIds.Length.ShouldBeGreaterThan(0, "User should have at least one role assigned after registration");

        // Verify the assigned role is a valid GUID
        Guid.TryParse(profile.RoleIds[0], out var assignedRoleId).ShouldBeTrue("Assigned role should be a valid GUID");
    }

    /// <summary>
    /// INTENT: Verify newly created user with assigned role can access permissions
    /// PURPOSE: Ensure full E2E flow from user creation to permission access
    /// BUSINESS CONTEXT: Users must be able to access resources after role assignment
    /// WHY IMPORTANT: Core authorization flow must work end-to-end
    /// ARCHITECTURAL SIGNIFICANCE: Tests complete User -> Role -> Permission chain
    /// FUTURE RESILIENCE: Ensures permission lookups work after registration
    /// </summary>
    [Fact]
    public async Task NewUser_With_Role_Should_Access_Permissions()
    {
        // Arrange - Create a custom role with permissions
        var customRoleId = Guid.NewGuid();
        var customPermissionId = Guid.NewGuid();

        var customPermission = new Permission
        {
            OwnerEntityId = customPermissionId,
            Resource = "dashboard",
            Actions = new[] { "read", "view" }
        };
        await TestDataContext().Commit(customPermission);
        TrackEntity(customPermissionId);

        var customRole = new Role
        {
            OwnerEntityId = customRoleId,
            Name = $"CustomRole_{Guid.NewGuid()}",
            Description = "Custom role for testing",
            PermissionIds = new[] { customPermissionId.ToString() }
        };
        await TestDataContext().Commit(customRole);
        TrackEntity(customRoleId);

        // Create user with profile
        var email = $"e2e_perm_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        // Assign the custom role
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.AssignRole(customRoleId);

        // Assert - Verify permission access
        var hasDashboardRead = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "dashboard.read");
        var hasDashboardView = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "dashboard.view");

        hasDashboardRead.ShouldBeTrue("User should have dashboard.read from assigned role");
        hasDashboardView.ShouldBeTrue("User should have dashboard.view from assigned role");
    }

    /// <summary>
    /// INTENT: Verify hierarchical permissions work from assigned role
    /// PURPOSE: Ensure users with parent permission can access child resources
    /// BUSINESS CONTEXT: Users with resource permission should access sub-resources
    /// WHY IMPORTANT: Hierarchical permission model must work end-to-end
    /// ARCHITECTURAL SIGNIFICANCE: Tests permission hierarchy via role assignment
    /// FUTURE RESILIENCE: Ensures hierarchical model works with role assignments
    /// </summary>
    [Fact]
    public async Task RoleAssignment_Should_Grant_Hierarchical_Access()
    {
        // Arrange - Create user without roles
        var email = $"e2e_hier_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        // Create hierarchical permission (base resource without specific actions)
        var hierarchicalPermissionId = Guid.NewGuid();
        var hierarchicalPermission = new Permission
        {
            OwnerEntityId = hierarchicalPermissionId,
            Resource = "reports",
            Actions = Array.Empty<string>()
        };
        await TestDataContext().Commit(hierarchicalPermission);
        TrackEntity(hierarchicalPermissionId);

        // Create role with the hierarchical permission
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = "ReportsRole",
            Description = "Role with hierarchical reports permission",
            PermissionIds = new[] { hierarchicalPermissionId.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Act - Assign role to user
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.AssignRole(roleId);

        // Assert - Hierarchical access should work
        var hasReports = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "reports");
        var hasReportsView = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "reports.view");
        var hasReportsExport = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "reports.export");
        var hasReportsDeepNested = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "reports.analytics.monthly");

        hasReports.ShouldBeTrue("User should have reports permission");
        hasReportsView.ShouldBeTrue("User should have reports.view via hierarchy");
        hasReportsExport.ShouldBeTrue("User should have reports.export via hierarchy");
        hasReportsDeepNested.ShouldBeTrue("User should have reports.analytics.monthly via hierarchy");
    }

    #endregion

    #region Scenario 2: Role Assignment Flow

    /// <summary>
    /// INTENT: Verify role assignment grants proper permissions
    /// PURPOSE: Ensure users receive permissions when roles are assigned
    /// BUSINESS CONTEXT: Admins assign roles to grant access to users
    /// WHY IMPORTANT: Role assignment is core access control mechanism
    /// ARCHITECTURAL SIGNIFICANCE: Tests AccountProfileHandler.AssignRole -> PermissionService chain
    /// FUTURE RESILIENCE: Ensures role assignment doesn't break permission propagation
    /// </summary>
    [Fact]
    public async Task RoleAssignment_Should_Grant_Permissions_From_Assigned_Role()
    {
        // Arrange - Create user without custom roles
        var email = $"e2e_assign_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        // Create custom role with specific permissions
        var customRoleId = Guid.NewGuid();
        var customPermissionId = Guid.NewGuid();

        var customPermission = new Permission
        {
            OwnerEntityId = customPermissionId,
            Resource = "custom.module",
            Actions = new[] { "read", "write", "delete" }
        };
        await TestDataContext().Commit(customPermission);
        TrackEntity(customPermissionId);

        var customRole = new Role
        {
            OwnerEntityId = customRoleId,
            Name = "CustomRole",
            Description = "Custom role with specific permissions",
            PermissionIds = new[] { customPermissionId.ToString() }
        };
        await TestDataContext().Commit(customRole);
        TrackEntity(customRoleId);

        // Verify user does NOT have permission before assignment
        var hasPermissionBefore = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "custom.module.read");
        hasPermissionBefore.ShouldBeFalse("User should not have permission before role assignment");

        // Act - Assign role to user
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.AssignRole(customRoleId);

        // Assert - User now has permissions
        var hasPermissionAfter = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "custom.module.read");
        var hasWriteAfter = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "custom.module.write");
        var hasDeleteAfter = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "custom.module.delete");

        hasPermissionAfter.ShouldBeTrue("User should have custom.module.read after role assignment");
        hasWriteAfter.ShouldBeTrue("User should have custom.module.write after role assignment");
        hasDeleteAfter.ShouldBeTrue("User should have custom.module.delete after role assignment");
    }

    /// <summary>
    /// INTENT: Verify role removal revokes permissions
    /// PURPOSE: Ensure users lose permissions when roles are removed
    /// BUSINESS CONTEXT: Access revocation must work immediately
    /// WHY IMPORTANT: Security depends on proper permission revocation
    /// ARCHITECTURAL SIGNIFICANCE: Tests AccountProfileHandler.RemoveRole -> cache invalidation
    /// FUTURE RESILIENCE: Ensures role removal properly revokes access
    /// </summary>
    [Fact]
    public async Task RoleRemoval_Should_Revoke_Permissions_From_Removed_Role()
    {
        // Arrange - Create user and role
        var email = $"e2e_remove_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var permission = new Permission
        {
            OwnerEntityId = permissionId,
            Resource = "removable.feature",
            Actions = new[] { "access" }
        };
        await TestDataContext().Commit(permission);
        TrackEntity(permissionId);

        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = "RemovableRole",
            Description = "Role to be removed",
            PermissionIds = new[] { permissionId.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Assign role
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.AssignRole(roleId);

        // Verify user has permission
        var hasPermissionWithRole = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "removable.feature.access");
        hasPermissionWithRole.ShouldBeTrue("User should have permission with assigned role");

        // Act - Remove role
        await profileHandler.RemoveRole(roleId);

        // Assert - User no longer has permission
        var hasPermissionAfterRemoval = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "removable.feature.access");
        hasPermissionAfterRemoval.ShouldBeFalse("User should not have permission after role removal");
    }

    #endregion

    #region Scenario 3: Multi-Role Permission Union

    /// <summary>
    /// INTENT: Verify users with multiple roles get union of all permissions
    /// PURPOSE: Ensure permission aggregation works correctly across roles
    /// BUSINESS CONTEXT: Users often have multiple roles with overlapping permissions
    /// WHY IMPORTANT: Permission union is fundamental to RBAC
    /// ARCHITECTURAL SIGNIFICANCE: Tests PermissionService.GetUserPermissionsAsync union logic
    /// FUTURE RESILIENCE: Ensures multi-role users have correct access
    /// </summary>
    [Fact]
    public async Task MultiRole_Should_Grant_Union_Of_All_Role_Permissions()
    {
        // Arrange
        var email = $"e2e_multi_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        // Create Permission P1
        var p1Id = Guid.NewGuid();
        var p1 = new Permission
        {
            OwnerEntityId = p1Id,
            Resource = "module.alpha",
            Actions = new[] { "read" }
        };
        await TestDataContext().Commit(p1);
        TrackEntity(p1Id);

        // Create Permission P2
        var p2Id = Guid.NewGuid();
        var p2 = new Permission
        {
            OwnerEntityId = p2Id,
            Resource = "module.beta",
            Actions = new[] { "read" }
        };
        await TestDataContext().Commit(p2);
        TrackEntity(p2Id);

        // Create Permission P3
        var p3Id = Guid.NewGuid();
        var p3 = new Permission
        {
            OwnerEntityId = p3Id,
            Resource = "module.gamma",
            Actions = new[] { "read" }
        };
        await TestDataContext().Commit(p3);
        TrackEntity(p3Id);

        // Create Role A with P1, P2
        var roleAId = Guid.NewGuid();
        var roleA = new Role
        {
            OwnerEntityId = roleAId,
            Name = "RoleA",
            Description = "Role with P1 and P2",
            PermissionIds = new[] { p1Id.ToString(), p2Id.ToString() }
        };
        await TestDataContext().Commit(roleA);
        TrackEntity(roleAId);

        // Create Role B with P2, P3 (P2 overlaps)
        var roleBId = Guid.NewGuid();
        var roleB = new Role
        {
            OwnerEntityId = roleBId,
            Name = "RoleB",
            Description = "Role with P2 and P3",
            PermissionIds = new[] { p2Id.ToString(), p3Id.ToString() }
        };
        await TestDataContext().Commit(roleB);
        TrackEntity(roleBId);

        // Assign both roles to user
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.AssignRole(roleAId);
        await profileHandler.AssignRole(roleBId);

        // Act - Get all permissions for user
        var permissions = await _permissionService.GetUserPermissionsAsync(account.OwnerEntityId);

        // Assert - Should have union: P1, P2, P3 (no duplicates)
        permissions.ShouldContain("module.alpha");
        permissions.ShouldContain("module.alpha.read");
        permissions.ShouldContain("module.beta");
        permissions.ShouldContain("module.beta.read");
        permissions.ShouldContain("module.gamma");
        permissions.ShouldContain("module.gamma.read");

        // Verify via HasPermission as well
        var hasP1 = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "module.alpha.read");
        var hasP2 = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "module.beta.read");
        var hasP3 = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "module.gamma.read");

        hasP1.ShouldBeTrue("User should have P1 from Role A");
        hasP2.ShouldBeTrue("User should have P2 from both roles (union)");
        hasP3.ShouldBeTrue("User should have P3 from Role B");
    }

    /// <summary>
    /// INTENT: Verify duplicate permissions across roles don't cause issues
    /// PURPOSE: Ensure HashSet union handles duplicates correctly
    /// BUSINESS CONTEXT: Overlapping role permissions are common
    /// WHY IMPORTANT: Prevents permission duplication bugs
    /// ARCHITECTURAL SIGNIFICANCE: Tests HashSet behavior in PermissionService
    /// FUTURE RESILIENCE: Ensures permission counting/listing is accurate
    /// </summary>
    [Fact]
    public async Task MultiRole_Should_Not_Duplicate_Overlapping_Permissions()
    {
        // Arrange
        var email = $"e2e_dup_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        // Create shared permission
        var sharedPermId = Guid.NewGuid();
        var sharedPerm = new Permission
        {
            OwnerEntityId = sharedPermId,
            Resource = "shared.resource",
            Actions = new[] { "read", "write" }
        };
        await TestDataContext().Commit(sharedPerm);
        TrackEntity(sharedPermId);

        // Create two roles with the same permission
        var role1Id = Guid.NewGuid();
        var role1 = new Role
        {
            OwnerEntityId = role1Id,
            Name = "SharedRole1",
            Description = "First role with shared permission",
            PermissionIds = new[] { sharedPermId.ToString() }
        };
        await TestDataContext().Commit(role1);
        TrackEntity(role1Id);

        var role2Id = Guid.NewGuid();
        var role2 = new Role
        {
            OwnerEntityId = role2Id,
            Name = "SharedRole2",
            Description = "Second role with shared permission",
            PermissionIds = new[] { sharedPermId.ToString() }
        };
        await TestDataContext().Commit(role2);
        TrackEntity(role2Id);

        // Assign both roles
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.AssignRole(role1Id);
        await profileHandler.AssignRole(role2Id);

        // Act
        var permissions = await _permissionService.GetUserPermissionsAsync(account.OwnerEntityId);

        // Assert - No duplicates (HashSet guarantees this, but verify count)
        var sharedResourceCount = permissions.Count(p => p.StartsWith("shared.resource"));
        sharedResourceCount.ShouldBe(3); // shared.resource, shared.resource.read, shared.resource.write
    }

    #endregion

    #region Scenario 4: Hierarchical Permission Check

    /// <summary>
    /// INTENT: Verify hierarchical permissions work (parent grants child access)
    /// PURPOSE: Ensure "admin" permission grants access to "admin.users.read"
    /// BUSINESS CONTEXT: Admin users need access to all sub-permissions
    /// WHY IMPORTANT: Hierarchical model simplifies permission management
    /// ARCHITECTURAL SIGNIFICANCE: Tests HasPermissionInternal hierarchy logic
    /// FUTURE RESILIENCE: Ensures permission inheritance works correctly
    /// </summary>
    [Fact]
    public async Task HierarchicalPermission_Should_Grant_Access_To_Child_Permissions()
    {
        // Arrange
        var email = $"e2e_hier_check_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        // Create admin permission (parent)
        var adminPermId = Guid.NewGuid();
        var adminPerm = new Permission
        {
            OwnerEntityId = adminPermId,
            Resource = "admin",
            Actions = Array.Empty<string>()
        };
        await TestDataContext().Commit(adminPerm);
        TrackEntity(adminPermId);

        // Create admin role
        var adminRoleId = Guid.NewGuid();
        var adminRole = new Role
        {
            OwnerEntityId = adminRoleId,
            Name = "AdminRole",
            Description = "Administrator role with parent permission",
            PermissionIds = new[] { adminPermId.ToString() }
        };
        await TestDataContext().Commit(adminRole);
        TrackEntity(adminRoleId);

        // Assign admin role
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.AssignRole(adminRoleId);

        // Act & Assert - Check various child permissions
        var hasAdmin = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin");
        var hasAdminUsers = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.users");
        var hasAdminUsersRead = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.users.read");
        var hasAdminUsersWrite = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.users.write");
        var hasAdminRoles = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.roles");
        var hasAdminSettings = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "admin.settings.security");

        hasAdmin.ShouldBeTrue("User should have admin permission");
        hasAdminUsers.ShouldBeTrue("User should have admin.users via hierarchy");
        hasAdminUsersRead.ShouldBeTrue("User should have admin.users.read via hierarchy");
        hasAdminUsersWrite.ShouldBeTrue("User should have admin.users.write via hierarchy");
        hasAdminRoles.ShouldBeTrue("User should have admin.roles via hierarchy");
        hasAdminSettings.ShouldBeTrue("User should have admin.settings.security via hierarchy");

        // Should NOT have unrelated permissions
        var hasUnrelated = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "billing.invoices");
        hasUnrelated.ShouldBeFalse("User should not have unrelated billing permission");
    }

    /// <summary>
    /// INTENT: Verify wildcard permissions work correctly
    /// PURPOSE: Ensure "admin.users.*" grants access to all user operations
    /// BUSINESS CONTEXT: Some roles need all operations on a specific resource
    /// WHY IMPORTANT: Wildcard simplifies broad access grants
    /// ARCHITECTURAL SIGNIFICANCE: Tests wildcard matching in HasPermissionInternal
    /// FUTURE RESILIENCE: Ensures wildcard pattern matching works
    /// </summary>
    [Fact]
    public async Task WildcardPermission_Should_Grant_Access_To_Matching_Resources()
    {
        // Arrange
        var email = $"e2e_wild_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        // Create wildcard permission
        var wildcardPermId = Guid.NewGuid();
        var wildcardPerm = new Permission
        {
            OwnerEntityId = wildcardPermId,
            Resource = "users.*",
            Actions = Array.Empty<string>()
        };
        await TestDataContext().Commit(wildcardPerm);
        TrackEntity(wildcardPermId);

        // Create role with wildcard
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = "UserManagerRole",
            Description = "Role with wildcard user permissions",
            PermissionIds = new[] { wildcardPermId.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Assign role
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.AssignRole(roleId);

        // Act & Assert
        var hasUsersRead = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "users.read");
        var hasUsersWrite = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "users.write");
        var hasUsersDelete = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "users.delete");
        var hasUsersCreate = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "users.create");

        hasUsersRead.ShouldBeTrue("Wildcard should grant users.read");
        hasUsersWrite.ShouldBeTrue("Wildcard should grant users.write");
        hasUsersDelete.ShouldBeTrue("Wildcard should grant users.delete");
        hasUsersCreate.ShouldBeTrue("Wildcard should grant users.create");

        // Should NOT match different root
        var hasRolesRead = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "roles.read");
        hasRolesRead.ShouldBeFalse("Wildcard should not grant access to different resource");
    }

    #endregion

    #region Scenario 5: Cache Invalidation Flow

    /// <summary>
    /// INTENT: Verify cache invalidation propagates permission changes
    /// PURPOSE: Ensure role updates are reflected immediately after cache clear
    /// BUSINESS CONTEXT: Permission changes must take effect immediately
    /// WHY IMPORTANT: Stale cache could cause security issues
    /// ARCHITECTURAL SIGNIFICANCE: Tests RoleHandler -> InvalidateCacheForRoleAsync flow
    /// FUTURE RESILIENCE: Ensures cache invalidation works with role updates
    /// </summary>
    [Fact]
    public async Task CacheInvalidation_Should_Reflect_Role_Permission_Changes()
    {
        // Arrange
        var email = $"e2e_cache_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        // Create initial permission P1
        var p1Id = Guid.NewGuid();
        var p1 = new Permission
        {
            OwnerEntityId = p1Id,
            Resource = "feature.one",
            Actions = new[] { "access" }
        };
        await TestDataContext().Commit(p1);
        TrackEntity(p1Id);

        // Create permission P2 (to be added later)
        var p2Id = Guid.NewGuid();
        var p2 = new Permission
        {
            OwnerEntityId = p2Id,
            Resource = "feature.two",
            Actions = new[] { "access" }
        };
        await TestDataContext().Commit(p2);
        TrackEntity(p2Id);

        // Create role with only P1
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = "EvolvingRole",
            Description = "Role that will get more permissions",
            PermissionIds = new[] { p1Id.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Assign role to user
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.AssignRole(roleId);

        // Verify initial state - has P1, not P2
        var hasP1Before = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "feature.one.access");
        var hasP2Before = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "feature.two.access");
        hasP1Before.ShouldBeTrue("User should have P1 initially");
        hasP2Before.ShouldBeFalse("User should not have P2 initially");

        // Act - Update role to add P2 (this should trigger cache invalidation)
        var roleHandler = TestDataContext().For<RoleHandler>(roleId);
        await roleHandler.GrantPermission(p2Id);

        // Assert - User now has both P1 and P2 (cache was invalidated)
        var hasP1After = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "feature.one.access");
        var hasP2After = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "feature.two.access");

        hasP1After.ShouldBeTrue("User should still have P1 after update");
        hasP2After.ShouldBeTrue("User should now have P2 after role update (cache invalidated)");
    }

    /// <summary>
    /// INTENT: Verify cache invalidation works when permissions are revoked
    /// PURPOSE: Ensure permission revocation takes effect immediately
    /// BUSINESS CONTEXT: Security requires immediate access revocation
    /// WHY IMPORTANT: Delayed revocation is a security vulnerability
    /// ARCHITECTURAL SIGNIFICANCE: Tests RevokePermission -> cache invalidation
    /// FUTURE RESILIENCE: Ensures revocation is not delayed by caching
    /// </summary>
    [Fact]
    public async Task CacheInvalidation_Should_Reflect_Permission_Revocation()
    {
        // Arrange
        var email = $"e2e_revoke_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        // Create permissions
        var permToKeepId = Guid.NewGuid();
        var permToKeep = new Permission
        {
            OwnerEntityId = permToKeepId,
            Resource = "keep.this",
            Actions = new[] { "access" }
        };
        await TestDataContext().Commit(permToKeep);
        TrackEntity(permToKeepId);

        var permToRevokeId = Guid.NewGuid();
        var permToRevoke = new Permission
        {
            OwnerEntityId = permToRevokeId,
            Resource = "revoke.this",
            Actions = new[] { "access" }
        };
        await TestDataContext().Commit(permToRevoke);
        TrackEntity(permToRevokeId);

        // Create role with both permissions
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = "RevokableRole",
            Description = "Role with permissions to revoke",
            PermissionIds = new[] { permToKeepId.ToString(), permToRevokeId.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Assign role
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.AssignRole(roleId);

        // Verify initial state
        var hasBothBefore = await _permissionService.HasAllPermissionsAsync(
            account.OwnerEntityId,
            "keep.this.access",
            "revoke.this.access");
        hasBothBefore.ShouldBeTrue("User should have both permissions initially");

        // Act - Revoke one permission from role
        var roleHandler = TestDataContext().For<RoleHandler>(roleId);
        await roleHandler.RevokePermission(permToRevokeId);

        // Assert - User still has kept permission, lost revoked one
        var hasKeptAfter = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "keep.this.access");
        var hasRevokedAfter = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "revoke.this.access");

        hasKeptAfter.ShouldBeTrue("User should still have kept permission");
        hasRevokedAfter.ShouldBeFalse("User should no longer have revoked permission");
    }

    /// <summary>
    /// INTENT: Verify direct user cache invalidation works
    /// PURPOSE: Ensure profile updates properly invalidate cache
    /// BUSINESS CONTEXT: Direct profile changes need cache refresh
    /// WHY IMPORTANT: Prevents stale profile data in permission checks
    /// ARCHITECTURAL SIGNIFICANCE: Tests InvalidateCacheAsync direct call
    /// FUTURE RESILIENCE: Ensures manual cache invalidation works
    /// </summary>
    [Fact]
    public async Task DirectCacheInvalidation_Should_Force_Fresh_Permission_Lookup()
    {
        // Arrange
        var email = $"e2e_direct_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithProfile(email);

        // Create permission and role
        var permId = Guid.NewGuid();
        var perm = new Permission
        {
            OwnerEntityId = permId,
            Resource = "direct.test",
            Actions = new[] { "access" }
        };
        await TestDataContext().Commit(perm);
        TrackEntity(permId);

        var roleId = Guid.NewGuid();
        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = "DirectTestRole",
            Description = "Role for direct cache test",
            PermissionIds = new[] { permId.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Get cached profile (no role yet)
        var profileBefore = await _permissionService.GetSecurityProfileAsync(account.OwnerEntityId);
        profileBefore.ShouldNotBeNull();

        // Manually update profile in database (bypassing handler for test purposes)
        var updatedProfile = profileBefore with
        {
            RoleIds = new[] { roleId.ToString() }
        };
        await TestDataContext().Commit(updatedProfile);

        // Without invalidation, cache still has old data
        var cachedProfile = await _permissionService.GetSecurityProfileAsync(account.OwnerEntityId);
        cachedProfile!.RoleIds.ShouldNotContain(roleId.ToString(), "Cached profile should not have new role yet");

        // Act - Invalidate cache
        await _permissionService.InvalidateCacheAsync(account.OwnerEntityId);

        // Assert - Fresh lookup gets new data
        var freshProfile = await _permissionService.GetSecurityProfileAsync(account.OwnerEntityId);
        freshProfile!.RoleIds.ShouldContain(roleId.ToString(), "Fresh profile should have new role");

        var hasPermission = await _permissionService.HasPermissionAsync(account.OwnerEntityId, "direct.test.access");
        hasPermission.ShouldBeTrue("User should have permission after cache invalidation");
    }

    #endregion

    #region Helper Methods

    private async Task<Account> CreateTestAccountWithProfile(string email)
    {
        var entityId = Guid.NewGuid();
        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword("TestPassword123!");

        var account = new Account
        {
            OwnerEntityId = entityId,
            Email = email,
            PasswordHash = hashedPassword,
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(account);
        TrackEntity(entityId);

        // Create basic profile without default role assignment
        var profile = new SecurityProfile
        {
            OwnerEntityId = entityId,
            Name = email.Split('@')[0],
            RoleIds = Array.Empty<string>(),
            Preferences = "{}"
        };
        await TestDataContext().Commit(profile);

        return account;
    }

    #endregion
}
