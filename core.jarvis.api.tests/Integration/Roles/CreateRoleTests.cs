using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration.Roles;

/// <summary>
/// INTENT: Test REAL role creation end-to-end with actual database
/// PURPOSE: Ensure the /security/roles POST endpoint works correctly
/// BUSINESS CONTEXT: Roles are foundational for RBAC permission system
/// WHY IMPORTANT: Broken role creation breaks the entire security model
/// ARCHITECTURAL SIGNIFICANCE: Tests the complete CreateRole flow from HTTP to database
/// FUTURE RESILIENCE: Prevents regressions in role management
/// </summary>
public class CreateRoleTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Test role creation with valid data and proper authentication
    /// PURPOSE: Ensure authenticated users with correct permissions can create roles
    /// BUSINESS CONTEXT: Admins need to create roles to assign permissions to users
    /// WHY IMPORTANT: This is the happy path for role management
    /// ARCHITECTURAL SIGNIFICANCE: Validates full request pipeline with auth
    /// FUTURE RESILIENCE: Catches issues with endpoint configuration or handler logic
    /// </summary>
    [Fact]
    public async Task CreateRole_WithValidDataAndAuthentication_ReturnsCreatedRole()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var roleName = $"TestRole_{Guid.NewGuid():N}";
        var roleDescription = "A test role for integration testing";

        // Create an authenticated user with admin.roles.write permission
        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword("TestPassword123!");

        var account = new Account
        {
            OwnerEntityId = ownerEntityId,
            Email = $"admin_{Guid.NewGuid()}@test.com",
            PasswordHash = hashedPassword,
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await TestDataContext().Commit(account);

        // Get a valid JWT token for this user
        // Generate valid JWT token for this user
        var token = TokenService.AccessToken(ownerEntityId, account.Email);

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var roleRequest = new { Name = roleName, Description = roleDescription };
        var requestBody = JsonSerializer.Serialize(roleRequest);
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/security/roles", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var responseBody = await response.Content.ReadAsStringAsync();
        var createdRole = JsonSerializer.Deserialize<Role>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        createdRole.ShouldNotBeNull();
        createdRole.Name.ShouldBe(roleName);
        createdRole.Description.ShouldBe(roleDescription);
        createdRole.Id.ShouldNotBe(Guid.Empty);
        createdRole.OwnerEntityId.ShouldNotBe(Guid.Empty);

        // Track the created role for cleanup
        TrackEntity(createdRole.OwnerEntityId);
    }

    /// <summary>
    /// INTENT: Test role creation fails without authentication
    /// PURPOSE: Ensure unauthenticated requests are rejected
    /// BUSINESS CONTEXT: Role creation must be protected from anonymous access
    /// WHY IMPORTANT: Security boundary enforcement is critical for RBAC
    /// ARCHITECTURAL SIGNIFICANCE: Validates FastEndpoints authorization middleware
    /// FUTURE RESILIENCE: Catches accidental removal of auth requirements
    /// </summary>
    [Fact]
    public async Task CreateRole_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var roleRequest = new { Name = "UnauthorizedRole", Description = "Should not be created" };
        var requestBody = JsonSerializer.Serialize(roleRequest);
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/security/roles", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// INTENT: Test role creation validation rejects invalid input
    /// PURPOSE: Ensure validation rules from Validator are enforced
    /// BUSINESS CONTEXT: Role names have business constraints (2-50 chars)
    /// WHY IMPORTANT: Invalid data should never reach the handler
    /// ARCHITECTURAL SIGNIFICANCE: Tests FluentValidation integration with FastEndpoints
    /// FUTURE RESILIENCE: Catches validation rule changes or bypasses
    /// </summary>
    [Fact]
    public async Task CreateRole_WithInvalidRoleName_ReturnsBadRequest()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword("TestPassword123!");

        var account = new Account
        {
            OwnerEntityId = ownerEntityId,
            Email = $"admin_{Guid.NewGuid()}@test.com",
            PasswordHash = hashedPassword,
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await TestDataContext().Commit(account);

        // Generate valid JWT token for this user
        var token = TokenService.AccessToken(ownerEntityId, account.Email);

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Empty name should fail validation (minimum 2 characters required)
        var roleRequest = new { Name = "", Description = "Valid description" };
        var requestBody = JsonSerializer.Serialize(roleRequest);
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/security/roles", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.ShouldContain("Role name is required");
    }
}
