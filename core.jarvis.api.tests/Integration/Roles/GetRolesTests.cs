using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration.Roles;

/// <summary>
/// INTENT: Test the GET /api/security/roles endpoint for listing all roles
/// PURPOSE: Verify that authenticated users with proper permissions can retrieve all roles
/// BUSINESS CONTEXT: Administrators need to view available roles for user management
/// WHY IMPORTANT: Role visibility is essential for RBAC administration workflows
/// ARCHITECTURAL SIGNIFICANCE: Tests the complete flow from HTTP request through authentication, authorization, and data retrieval
/// FUTURE RESILIENCE: Ensures role listing endpoint remains secure and functional
/// </summary>
public class GetRolesTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify that authenticated users with admin.roles.read permission can list all roles
    /// PURPOSE: Ensure the happy path works correctly for authorized users
    /// BUSINESS CONTEXT: Administrators managing user roles need to see available roles
    /// WHY IMPORTANT: Core functionality for role-based access control administration
    /// ARCHITECTURAL SIGNIFICANCE: Tests authentication, permission check, and data retrieval layers
    /// FUTURE RESILIENCE: Catches regressions in permission enforcement or endpoint functionality
    /// </summary>
    [Fact]
    public async Task GetRoles_WithAdminRolesReadPermission_ReturnsRolesList()
    {
        // Arrange - Create user with admin.roles.read permission
        var email = $"get_roles_success_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithAdminRolesPermission(email, "admin.roles.read");

        // Generate valid JWT token for the user
        var token = TokenService.AccessToken(account.OwnerEntityId, email);

        // Act - Call the endpoint
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/security/roles");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var roles = JsonSerializer.Deserialize<List<Role>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        roles.ShouldNotBeNull();
        // The response should contain at least the role we created for this test
        roles.Count.ShouldBeGreaterThan(0, "Expected at least one role in the response");
    }

    /// <summary>
    /// INTENT: Verify that unauthenticated requests are rejected with 401 Unauthorized
    /// PURPOSE: Ensure the endpoint enforces authentication before authorization
    /// BUSINESS CONTEXT: Role data must only be accessible to authenticated users
    /// WHY IMPORTANT: Security requirement - prevents anonymous access to sensitive role information
    /// ARCHITECTURAL SIGNIFICANCE: Tests the authentication middleware intercepts unauthenticated requests
    /// FUTURE RESILIENCE: Ensures authentication enforcement is not accidentally removed
    /// </summary>
    [Fact]
    public async Task GetRoles_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No authentication header
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act - Call the endpoint without any Authorization header
        var response = await client.GetAsync("/api/security/roles");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized,
            "Endpoint should return 401 Unauthorized when no authentication is provided");
    }

    /// <summary>
    /// INTENT: Verify that authenticated users without admin.roles.read permission are denied access
    /// PURPOSE: Ensure the RequirePermission attribute enforces proper authorization
    /// BUSINESS CONTEXT: Regular users without admin permissions should not see role configurations
    /// WHY IMPORTANT: Principle of least privilege - only authorized users access admin features
    /// ARCHITECTURAL SIGNIFICANCE: Tests the PermissionPreProcessor correctly denies access for missing permissions
    /// FUTURE RESILIENCE: Ensures permission checks remain enforced on the roles endpoint
    /// </summary>
    [Fact]
    public async Task GetRoles_WithoutAdminRolesReadPermission_ReturnsForbidden()
    {
        // Arrange - Create user without admin.roles.read permission
        var email = $"get_roles_no_perm_{Guid.NewGuid()}@example.com";
        var account = await CreateTestAccountWithoutAdminPermission(email);

        // Generate valid JWT token for the user
        var token = TokenService.AccessToken(account.OwnerEntityId, email);

        // Act - Call the endpoint
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/security/roles");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden,
            "Endpoint should return 403 Forbidden when user lacks admin.roles.read permission");
    }

    #region Helper Methods

    /// <summary>
    /// Creates a test account with a role that has the specified admin permission.
    /// </summary>
    private async Task<core.jarvis.api.Models.Account> CreateTestAccountWithAdminRolesPermission(string email, string permissionResource)
    {
        var entityId = Guid.NewGuid();
        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword("TestPassword123!");

        // Create the account
        var account = new core.jarvis.api.Models.Account
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

        // Create permission for admin.roles.read
        var permissionId = Guid.NewGuid();
        var permission = new Permission
        {
            OwnerEntityId = permissionId,
            Resource = permissionResource,
            Actions = Array.Empty<string>()
        };
        await TestDataContext().Commit(permission);
        TrackEntity(permissionId);

        // Create role with the permission
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = $"AdminRolesRole_{Guid.NewGuid()}",
            Description = "Role with admin.roles.read permission for testing",
            PermissionIds = new[] { permissionId.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Create security profile with the role
        var profile = new SecurityProfile
        {
            OwnerEntityId = entityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { roleId.ToString() },
            Preferences = "{}"
        };
        await TestDataContext().Commit(profile);

        return account;
    }

    /// <summary>
    /// Creates a test account with a role that does NOT have admin permissions.
    /// </summary>
    private async Task<core.jarvis.api.Models.Account> CreateTestAccountWithoutAdminPermission(string email)
    {
        var entityId = Guid.NewGuid();
        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword("TestPassword123!");

        // Create the account
        var account = new core.jarvis.api.Models.Account
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

        // Create a non-admin permission (e.g., basic read access)
        var permissionId = Guid.NewGuid();
        var permission = new Permission
        {
            OwnerEntityId = permissionId,
            Resource = "basic.access",
            Actions = new[] { "read" }
        };
        await TestDataContext().Commit(permission);
        TrackEntity(permissionId);

        // Create role with the non-admin permission
        var roleId = Guid.NewGuid();
        var role = new Role
        {
            OwnerEntityId = roleId,
            Name = $"BasicRole_{Guid.NewGuid()}",
            Description = "Role without admin permissions for testing",
            PermissionIds = new[] { permissionId.ToString() }
        };
        await TestDataContext().Commit(role);
        TrackEntity(roleId);

        // Create security profile with the role
        var profile = new SecurityProfile
        {
            OwnerEntityId = entityId,
            Name = email.Split('@')[0],
            RoleIds = new[] { roleId.ToString() },
            Preferences = "{}"
        };
        await TestDataContext().Commit(profile);

        return account;
    }

    #endregion
}
