using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using core.jarvis.Data;
using Shouldly;

namespace core.jarvis.api.tests.Integration.Handlers;

/// <summary>
/// Integration tests for RoleHandler component lifecycle operations.
/// Tests verify proper role creation, permission management, and lifecycle operations.
/// These tests run against a real database to ensure data persistence and context behavior.
/// </summary>
public class RoleHandlerTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify that CreateRole successfully creates a new role with the specified properties.
    /// PURPOSE: Ensures the role creation workflow correctly persists role data to the database.
    /// BUSINESS CONTEXT: Creating roles is the foundation of RBAC system initialization.
    /// WHY IMPORTANT: Role creation must correctly store name, description, and permission IDs.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the handler's ability to use TryCommit and persist immutable records.
    /// FUTURE RESILIENCE: Validates role creation continues to work after schema or handler changes.
    /// </summary>
    [Fact]
    public async Task CreateRole_WithValidData_CreatesRoleSuccessfully()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        var roleHandler = TestDataContext().For<RoleHandler>(ownerEntityId);

        var roleName = "TestAdminRole";
        var roleDescription = "Test administrator role with full system access";
        var permissionId = Guid.NewGuid().ToString();

        var newRole = new Role
        {
            Name = roleName,
            Description = roleDescription,
            PermissionIds = new[] { permissionId }
        };

        // Act
        var result = await roleHandler.CreateRole(newRole);

        // Assert - verify role was created with correct properties
        result.ShouldNotBeNull("CreateRole should return the created role");
        result.Name.ShouldBe(roleName, "Role name should match input");
        result.Description.ShouldBe(roleDescription, "Role description should match input");
        result.PermissionIds.ShouldHaveSingleItem("Should have one permission");
        result.PermissionIds[0].ShouldBe(permissionId, "Permission ID should be preserved");
        result.OwnerEntityId.ShouldBe(ownerEntityId, "Owner entity ID should be set to handler context");
        result.LastUpdated.ShouldBeGreaterThan(DateTime.MinValue, "LastUpdated should be set");

        // Cleanup
        await TestDataContext().Remove<Role>(ownerEntityId);
    }

    /// <summary>
    /// INTENT: Verify that GrantPermission adds a new permission to a role and updates the timestamp.
    /// PURPOSE: Ensures the permission granting workflow correctly adds permissions to roles.
    /// BUSINESS CONTEXT: Granting permissions to roles is how RBAC functionality is configured.
    /// WHY IMPORTANT: Permission grants must be idempotent and update the LastUpdated timestamp.
    /// ARCHITECTURAL SIGNIFICANCE: Tests handler's ability to work with immutable role records.
    /// FUTURE RESILIENCE: Validates permission grant workflow continues working after changes.
    /// </summary>
    [Fact]
    public async Task GrantPermission_WithNewPermission_AddsPermissionAndUpdatesTimestamp()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        var roleHandler = TestDataContext().For<RoleHandler>(ownerEntityId);

        var initialPermissionId = Guid.NewGuid().ToString();
        var initialRole = new Role
        {
            Name = "TestRole",
            Description = "Test role for permission granting",
            PermissionIds = new[] { initialPermissionId }
        };

        await roleHandler.CreateRole(initialRole);
        var createdRole = await roleHandler.Get();
        var timestampBefore = createdRole.LastUpdated;

        // Wait a tiny bit to ensure timestamp difference
        await Task.Delay(10);

        var newPermissionId = Guid.NewGuid();

        // Act
        var result = await roleHandler.GrantPermission(newPermissionId);

        // Assert - verify permission was added and timestamp updated
        result.ShouldNotBeNull("GrantPermission should return updated role");
        result.PermissionIds.Length.ShouldBe(2, "Should have two permissions after grant");
        result.PermissionIds.ShouldContain(initialPermissionId, "Should contain original permission");
        result.PermissionIds.ShouldContain(newPermissionId.ToString(), "Should contain new permission");
        result.LastUpdated.ShouldBeGreaterThan(timestampBefore, "LastUpdated should be newer after grant");

        // Cleanup
        await TestDataContext().Remove<Role>(ownerEntityId);
    }

    /// <summary>
    /// INTENT: Verify that GrantPermission is idempotent - granting same permission twice has no effect.
    /// PURPOSE: Ensures permission grants don't create duplicate entries.
    /// BUSINESS CONTEXT: Admin UI may grant permissions without checking current state.
    /// WHY IMPORTANT: Idempotent operations prevent duplicate permission entries.
    /// ARCHITECTURAL SIGNIFICANCE: Tests handler's ability to detect and skip duplicate grants.
    /// FUTURE RESILIENCE: Validates idempotency continues working after handler changes.
    /// </summary>
    [Fact]
    public async Task GrantPermission_WhenPermissionAlreadyGranted_DoesNotCreateDuplicate()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        var roleHandler = TestDataContext().For<RoleHandler>(ownerEntityId);

        var permissionId = Guid.NewGuid().ToString();
        var initialRole = new Role
        {
            Name = "TestRole",
            Description = "Test role for idempotent permission granting",
            PermissionIds = new[] { permissionId }
        };

        await roleHandler.CreateRole(initialRole);

        // Act - grant the same permission twice
        var firstGrant = await roleHandler.GrantPermission(Guid.Parse(permissionId));
        var secondGrant = await roleHandler.GrantPermission(Guid.Parse(permissionId));

        // Assert - should still have exactly one permission, not two
        firstGrant.PermissionIds.Length.ShouldBe(1, "After first grant, should still have one permission");
        secondGrant.PermissionIds.Length.ShouldBe(1, "After second grant, should still have one permission");
        secondGrant.PermissionIds[0].ShouldBe(permissionId, "Permission should be preserved");

        // Cleanup
        await TestDataContext().Remove<Role>(ownerEntityId);
    }

    /// <summary>
    /// INTENT: Verify that RevokePermission removes a permission from a role and updates the timestamp.
    /// PURPOSE: Ensures the permission revocation workflow correctly removes permissions from roles.
    /// BUSINESS CONTEXT: Revoking permissions is how access restrictions are implemented.
    /// WHY IMPORTANT: Permission revokes must be immediate and update the LastUpdated timestamp.
    /// ARCHITECTURAL SIGNIFICANCE: Tests handler's ability to work with immutable role records during revocation.
    /// FUTURE RESILIENCE: Validates permission revocation workflow continues working after changes.
    /// </summary>
    [Fact]
    public async Task RevokePermission_WithExistingPermission_RemovesPermissionAndUpdatesTimestamp()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        var roleHandler = TestDataContext().For<RoleHandler>(ownerEntityId);

        var permissionId1 = Guid.NewGuid().ToString();
        var permissionId2 = Guid.NewGuid().ToString();
        var initialRole = new Role
        {
            Name = "TestRole",
            Description = "Test role for permission revocation",
            PermissionIds = new[] { permissionId1, permissionId2 }
        };

        await roleHandler.CreateRole(initialRole);
        var createdRole = await roleHandler.Get();
        var timestampBefore = createdRole.LastUpdated;

        // Wait a tiny bit to ensure timestamp difference
        await Task.Delay(10);

        // Act
        var result = await roleHandler.RevokePermission(Guid.Parse(permissionId1));

        // Assert - verify permission was removed and timestamp updated
        result.ShouldNotBeNull("RevokePermission should return updated role");
        result.PermissionIds.Length.ShouldBe(1, "Should have one permission after revocation");
        result.PermissionIds.ShouldContain(permissionId2, "Should contain remaining permission");
        result.PermissionIds.ShouldNotContain(permissionId1, "Should not contain revoked permission");
        result.LastUpdated.ShouldBeGreaterThan(timestampBefore, "LastUpdated should be newer after revocation");

        // Cleanup
        await TestDataContext().Remove<Role>(ownerEntityId);
    }

    /// <summary>
    /// INTENT: Verify that UpdateRole modifies name and description while preserving permissions.
    /// PURPOSE: Ensures the role update workflow correctly modifies role properties.
    /// BUSINESS CONTEXT: Updating roles is how administrators maintain role configurations.
    /// WHY IMPORTANT: Role updates must preserve permissions while allowing name/description changes.
    /// ARCHITECTURAL SIGNIFICANCE: Tests handler's ability to use immutable pattern with partial updates.
    /// FUTURE RESILIENCE: Validates role update workflow continues working after handler changes.
    /// </summary>
    [Fact]
    public async Task UpdateRole_WithNewData_UpdatesPropertiesAndPreservesPermissions()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        var roleHandler = TestDataContext().For<RoleHandler>(ownerEntityId);

        var originalPermissionId = Guid.NewGuid().ToString();
        var initialRole = new Role
        {
            Name = "OldRoleName",
            Description = "Old role description",
            PermissionIds = new[] { originalPermissionId }
        };

        await roleHandler.CreateRole(initialRole);
        var createdRole = await roleHandler.Get();
        var timestampBefore = createdRole.LastUpdated;

        // Wait a tiny bit to ensure timestamp difference
        await Task.Delay(10);

        var updatedRole = new Role
        {
            Name = "NewRoleName",
            Description = "New role description",
            PermissionIds = new[] { originalPermissionId }
        };

        // Act
        var result = await roleHandler.UpdateRole(updatedRole);

        // Assert - verify properties updated while permissions preserved
        result.ShouldNotBeNull("UpdateRole should return updated role");
        result.Name.ShouldBe("NewRoleName", "Role name should be updated");
        result.Description.ShouldBe("New role description", "Role description should be updated");
        result.PermissionIds.ShouldHaveSingleItem("Should still have one permission");
        result.PermissionIds[0].ShouldBe(originalPermissionId, "Permission should be preserved");
        result.LastUpdated.ShouldBeGreaterThan(timestampBefore, "LastUpdated should be newer after update");

        // Cleanup
        await TestDataContext().Remove<Role>(ownerEntityId);
    }
}
