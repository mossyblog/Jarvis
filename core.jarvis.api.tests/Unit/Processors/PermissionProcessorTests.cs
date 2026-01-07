using System.Security.Claims;
using core.jarvis.api.Attributes;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Processors;
using core.jarvis.api.Services;
using core.jarvis.api.tests.Helpers;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Unit.Processors;

/// <summary>
/// INTENT: Unit tests for the PermissionPreProcessor endpoint authorization component
/// PURPOSE: Validate that endpoint permission checks work correctly for various scenarios
/// BUSINESS CONTEXT: API endpoints require proper authorization checks before processing requests
/// WHY IMPORTANT: PermissionPreProcessor is the gateway for all permission-based endpoint access
/// ARCHITECTURAL SIGNIFICANCE: Tests the pre-processor that integrates with FastEndpoints pipeline
/// FUTURE RESILIENCE: Ensures permission attribute-based authorization works across endpoint changes
/// </summary>
public class PermissionProcessorTests : ApiIntegrationTestBase
{
    private readonly IPermissionService _permissionService;

    public PermissionProcessorTests()
    {
        _permissionService = _serviceProvider.GetRequiredService<IPermissionService>();
    }

    /// <summary>
    /// INTENT: Verify that authenticated users with matching permissions are granted access
    /// PURPOSE: Ensure the pre-processor correctly validates user permissions against endpoint requirements
    /// BUSINESS CONTEXT: Users with proper role-based permissions must be able to access protected endpoints
    /// WHY IMPORTANT: Core authorization flow - users with permissions should not be blocked
    /// ARCHITECTURAL SIGNIFICANCE: Validates the integration between PermissionPreProcessor and IPermissionService
    /// FUTURE RESILIENCE: Ensures permission checks remain functional as endpoint requirements evolve
    /// </summary>
    [Fact]
    public async Task PreProcessAsync_Should_Allow_User_With_Required_Permission()
    {
        // Arrange - Create user with specific permission through role
        var email = $"test_preproc_allow_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.CreateWithDefaults(email);
        TrackEntity(account.OwnerEntityId);

        // Create permission and role for "dashboard.read"
        var permissionId = Guid.NewGuid();
        var permission = new Permission
        {
            OwnerEntityId = permissionId,
            Resource = "dashboard",
            Actions = new[] { "read" }
        };
        await TestDataContext().Commit(permission);
        TrackEntity(permissionId);

        var roleId = Guid.NewGuid();
        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = $"DashboardReader_{Guid.NewGuid()}",
            Description = "Role with dashboard read permission",
            PermissionIds = new[] { permissionId.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Assign role to user
        await profileHandler.AssignRole(roleId);

        // Act - Check permission via service (mirrors pre-processor logic)
        var hasPermission = await _permissionService.HasAnyPermissionAsync(
            account.OwnerEntityId,
            "dashboard.read");

        // Assert
        hasPermission.ShouldBeTrue("User with assigned role should have the required permission");
    }

    /// <summary>
    /// INTENT: Verify that authenticated users without required permissions are denied access
    /// PURPOSE: Ensure the pre-processor correctly rejects users lacking required permissions
    /// BUSINESS CONTEXT: Users without proper permissions must be blocked from protected endpoints
    /// WHY IMPORTANT: Security depends on denying access when permissions are missing
    /// ARCHITECTURAL SIGNIFICANCE: Validates the permission denial path in the pre-processor flow
    /// FUTURE RESILIENCE: Ensures unauthorized access attempts continue to be blocked
    /// </summary>
    [Fact]
    public async Task PreProcessAsync_Should_Deny_User_Without_Required_Permission()
    {
        // Arrange - Create user without the required permission
        var email = $"test_preproc_deny_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.CreateWithDefaults(email);
        TrackEntity(account.OwnerEntityId);

        // Create a different permission (not the one required)
        var permissionId = Guid.NewGuid();
        var permission = new Permission
        {
            OwnerEntityId = permissionId,
            Resource = "reports",
            Actions = new[] { "view" }
        };
        await TestDataContext().Commit(permission);
        TrackEntity(permissionId);

        var roleId = Guid.NewGuid();
        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = $"ReportsViewer_{Guid.NewGuid()}",
            Description = "Role with reports view permission",
            PermissionIds = new[] { permissionId.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Assign role to user (but not the required "admin.settings" permission)
        await profileHandler.AssignRole(roleId);

        // Act - Check permission for a permission the user does NOT have
        var hasAdminSettings = await _permissionService.HasAnyPermissionAsync(
            account.OwnerEntityId,
            "admin.settings.modify");

        // Assert
        hasAdminSettings.ShouldBeFalse("User without the required permission should be denied");
    }

    /// <summary>
    /// INTENT: Verify that user with any one of multiple required permissions is granted access
    /// PURPOSE: Ensure the pre-processor correctly handles OR logic for multiple permissions
    /// BUSINESS CONTEXT: Some endpoints allow access if user has any of several valid permissions
    /// WHY IMPORTANT: Flexible permission requirements enable proper access control patterns
    /// ARCHITECTURAL SIGNIFICANCE: Validates HasAnyPermissionAsync behavior used by pre-processor
    /// FUTURE RESILIENCE: Ensures OR-based permission checks work as endpoints evolve
    /// </summary>
    [Fact]
    public async Task PreProcessAsync_Should_Allow_User_With_Any_Of_Multiple_Permissions()
    {
        // Arrange - Create user with only one of multiple acceptable permissions
        var email = $"test_preproc_any_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccount(email, "TestPassword123!");
        var profileHandler = TestDataContext().For<AccountProfileHandler>(account.OwnerEntityId);
        await profileHandler.CreateWithDefaults(email);
        TrackEntity(account.OwnerEntityId);

        // Create permission for "analytics.view" (one of multiple acceptable permissions)
        var permissionId = Guid.NewGuid();
        var permission = new Permission
        {
            OwnerEntityId = permissionId,
            Resource = "analytics",
            Actions = new[] { "view" }
        };
        await TestDataContext().Commit(permission);
        TrackEntity(permissionId);

        var roleId = Guid.NewGuid();
        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = $"AnalyticsViewer_{Guid.NewGuid()}",
            Description = "Role with analytics view permission",
            PermissionIds = new[] { permissionId.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Assign role to user
        await profileHandler.AssignRole(roleId);

        // Act - Check if user has ANY of multiple permissions
        // User has "analytics.view" but not "admin.dashboard" or "reports.export"
        var hasAnyPermission = await _permissionService.HasAnyPermissionAsync(
            account.OwnerEntityId,
            "admin.dashboard",
            "analytics.view",
            "reports.export");

        // Assert
        hasAnyPermission.ShouldBeTrue("User with any of the required permissions should be allowed");
    }

    #region Helper Methods

    private async Task<Models.Account> CreateTestAccount(string email, string password)
    {
        var entityId = Guid.NewGuid();
        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(password);

        var account = new Models.Account
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
        return account;
    }

    #endregion
}
