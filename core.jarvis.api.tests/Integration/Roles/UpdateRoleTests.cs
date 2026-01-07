using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.tests.Helpers;
using core.jarvis.Data;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

using Account = core.jarvis.api.Models.Account;

namespace core.jarvis.api.tests.Integration.Roles;

/// <summary>
/// INTENT: Test PUT /security/roles/{id} endpoint for role modification.
/// PURPOSE: Ensure roles can be updated through the API with proper authorization.
/// BUSINESS CONTEXT: Administrators need to modify role names, descriptions, and permissions.
/// WHY IMPORTANT: Role updates are critical for evolving RBAC configurations over time.
/// ARCHITECTURAL SIGNIFICANCE: Tests the full HTTP request pipeline including validation and authorization.
/// FUTURE RESILIENCE: Prevents regression in role update functionality after refactoring.
/// </summary>
public class UpdateRoleTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify authenticated user with admin permissions can update a role.
    /// PURPOSE: Test the happy path for role update via HTTP endpoint.
    /// BUSINESS CONTEXT: Administrators must be able to modify role definitions.
    /// WHY IMPORTANT: Core CRUD functionality for role management.
    /// ARCHITECTURAL SIGNIFICANCE: Tests FastEndpoints endpoint, validator, and handler integration.
    /// FUTURE RESILIENCE: Catches breaks in the update pipeline.
    /// </summary>
    [Fact]
    public async Task UpdateRole_WithValidDataAndAuthorization_ReturnsUpdatedRole()
    {
        // Arrange - Create admin user with required permission
        var adminEmail = $"admin_update_{Guid.NewGuid()}@example.com";
        var adminPassword = "AdminPassword123!";
        var adminEntityId = Guid.NewGuid();
        TrackEntity(adminEntityId);

        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(adminPassword);

        // Create admin account
        var adminAccount = new Account
        {
            OwnerEntityId = adminEntityId,
            Email = adminEmail,
            PasswordHash = hashedPassword,
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(adminAccount);

        // Create admin permission for roles
        var adminPermissionId = Guid.NewGuid();
        var adminPermission = new Permission
        {
            OwnerEntityId = adminPermissionId,
            Resource = "admin.roles",
            Actions = new[] { "write", "read" }
        };
        await TestDataContext().Commit(adminPermission);
        TrackEntity(adminPermissionId);

        // Create admin role with the permission
        var adminRoleId = Guid.NewGuid();
        var adminRole = new Role
        {
            OwnerEntityId = adminRoleId,
            Name = "RoleAdmin",
            Description = "Role administrator",
            PermissionIds = new[] { adminPermissionId.ToString() }
        };
        await TestDataContext().Commit(adminRole);
        TrackEntity(adminRoleId);

        // Create admin profile with the role
        var adminProfile = new SecurityProfile
        {
            OwnerEntityId = adminEntityId,
            Name = "Admin User",
            RoleIds = new[] { adminRoleId.ToString() },
            Preferences = "{}"
        };
        await TestDataContext().Commit(adminProfile);

        // Create a role to be updated
        var targetRoleId = Guid.NewGuid();
        var targetRole = new Role
        {
            OwnerEntityId = targetRoleId,
            Name = "OriginalRoleName",
            Description = "Original description",
            PermissionIds = Array.Empty<string>()
        };
        await TestDataContext().Commit(targetRole);
        TrackEntity(targetRoleId);

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Authenticate to get access token
        var authBody = JsonSerializer.Serialize(new { Email = adminEmail, Password = adminPassword });
        var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");
        var authResponse = await client.PostAsync("/api/security/auth", authContent);
        authResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var authResponseBody = await authResponse.Content.ReadAsStringAsync();
        var authToken = JsonSerializer.Deserialize<AuthToken>(authResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Set authorization header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken!.AccessToken);

        // Act - Update the role
        var updateRequest = new
        {
            Name = "UpdatedRoleName",
            Description = "Updated description",
            PermissionIds = Array.Empty<string>()
        };
        var updateBody = JsonSerializer.Serialize(updateRequest);
        var updateContent = new StringContent(updateBody, Encoding.UTF8, "application/json");
        var updateResponse = await client.PutAsync($"/api/security/roles/{targetRoleId}", updateContent);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var responseBody = await updateResponse.Content.ReadAsStringAsync();
        var updatedRole = JsonSerializer.Deserialize<Role>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        updatedRole.ShouldNotBeNull();
        updatedRole.Name.ShouldBe("UpdatedRoleName");
        updatedRole.Description.ShouldBe("Updated description");
        updatedRole.OwnerEntityId.ShouldBe(targetRoleId);
    }

    /// <summary>
    /// INTENT: Verify unauthenticated requests are rejected with 401.
    /// PURPOSE: Test security boundary for role update endpoint.
    /// BUSINESS CONTEXT: Only authenticated users should access role management.
    /// WHY IMPORTANT: Prevents unauthorized access to security-critical operations.
    /// ARCHITECTURAL SIGNIFICANCE: Tests JWT authentication middleware integration.
    /// FUTURE RESILIENCE: Ensures authentication requirement is never bypassed.
    /// </summary>
    [Fact]
    public async Task UpdateRole_WithoutAuthentication_Returns401()
    {
        // Arrange - Create a role to attempt updating
        var targetRoleId = Guid.NewGuid();
        var targetRole = new Role
        {
            OwnerEntityId = targetRoleId,
            Name = "RoleToUpdate",
            Description = "Will attempt to update without auth",
            PermissionIds = Array.Empty<string>()
        };
        await TestDataContext().Commit(targetRole);
        TrackEntity(targetRoleId);

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act - Attempt update without authentication
        var updateRequest = new
        {
            Name = "UnauthorizedUpdate",
            Description = "Should not succeed",
            PermissionIds = Array.Empty<string>()
        };
        var updateBody = JsonSerializer.Serialize(updateRequest);
        var updateContent = new StringContent(updateBody, Encoding.UTF8, "application/json");
        var updateResponse = await client.PutAsync($"/api/security/roles/{targetRoleId}", updateContent);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized,
            "Unauthenticated request should return 401 Unauthorized");
    }

    /// <summary>
    /// INTENT: Verify update request for non-existent role returns 404.
    /// PURPOSE: Test proper error handling when role does not exist.
    /// BUSINESS CONTEXT: Administrators need clear feedback when referencing invalid roles.
    /// WHY IMPORTANT: Prevents confusion and guides correct API usage.
    /// ARCHITECTURAL SIGNIFICANCE: Tests endpoint's existence check before update.
    /// FUTURE RESILIENCE: Ensures 404 behavior is consistent after refactoring.
    /// </summary>
    [Fact]
    public async Task UpdateRole_WithNonExistentRoleId_Returns404()
    {
        // Arrange - Create admin user with required permission
        var adminEmail = $"admin_404_{Guid.NewGuid()}@example.com";
        var adminPassword = "AdminPassword123!";
        var adminEntityId = Guid.NewGuid();
        TrackEntity(adminEntityId);

        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(adminPassword);

        // Create admin account
        var adminAccount = new Account
        {
            OwnerEntityId = adminEntityId,
            Email = adminEmail,
            PasswordHash = hashedPassword,
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        await TestDataContext().Commit(adminAccount);

        // Create admin permission for roles
        var adminPermissionId = Guid.NewGuid();
        var adminPermission = new Permission
        {
            OwnerEntityId = adminPermissionId,
            Resource = "admin.roles",
            Actions = new[] { "write", "read" }
        };
        await TestDataContext().Commit(adminPermission);
        TrackEntity(adminPermissionId);

        // Create admin role with the permission
        var adminRoleId = Guid.NewGuid();
        var adminRole = new Role
        {
            OwnerEntityId = adminRoleId,
            Name = "RoleAdmin404",
            Description = "Role administrator for 404 test",
            PermissionIds = new[] { adminPermissionId.ToString() }
        };
        await TestDataContext().Commit(adminRole);
        TrackEntity(adminRoleId);

        // Create admin profile with the role
        var adminProfile = new SecurityProfile
        {
            OwnerEntityId = adminEntityId,
            Name = "Admin User 404",
            RoleIds = new[] { adminRoleId.ToString() },
            Preferences = "{}"
        };
        await TestDataContext().Commit(adminProfile);

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Authenticate to get access token
        var authBody = JsonSerializer.Serialize(new { Email = adminEmail, Password = adminPassword });
        var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");
        var authResponse = await client.PostAsync("/api/security/auth", authContent);
        authResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var authResponseBody = await authResponse.Content.ReadAsStringAsync();
        var authToken = JsonSerializer.Deserialize<AuthToken>(authResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Set authorization header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken!.AccessToken);

        // Act - Attempt to update non-existent role
        var nonExistentRoleId = Guid.NewGuid();
        var updateRequest = new
        {
            Name = "NonExistentUpdate",
            Description = "Role does not exist",
            PermissionIds = Array.Empty<string>()
        };
        var updateBody = JsonSerializer.Serialize(updateRequest);
        var updateContent = new StringContent(updateBody, Encoding.UTF8, "application/json");
        var updateResponse = await client.PutAsync($"/api/security/roles/{nonExistentRoleId}", updateContent);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Request for non-existent role should return 404 Not Found");
    }
}
