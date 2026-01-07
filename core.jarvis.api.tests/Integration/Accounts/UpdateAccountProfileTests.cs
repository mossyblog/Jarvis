using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using core.jarvis.api.Services;
using core.jarvis.Data;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration.Accounts;

/// <summary>
/// INTENT: Test the PUT /accounts/me endpoint for updating user profiles
/// PURPOSE: Ensure authenticated users can update their own profile information
/// BUSINESS CONTEXT: Users must be able to modify their display name, avatar, and preferences
/// WHY IMPORTANT: Profile management is a core user experience feature
/// ARCHITECTURAL SIGNIFICANCE: Tests authenticated endpoint with SecurityProfile updates
/// FUTURE RESILIENCE: Prevents regressions in profile update functionality
/// </summary>
public class UpdateAccountProfileTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Test successful profile update with valid authentication and data
    /// PURPOSE: Ensure authenticated users can update their profile name successfully
    /// BUSINESS CONTEXT: Users frequently change their display name in profile settings
    /// WHY IMPORTANT: Core functionality for user self-service profile management
    /// ARCHITECTURAL SIGNIFICANCE: Tests full PUT flow through AccountProfileHandler
    /// FUTURE RESILIENCE: Ensures profile updates work after code changes
    /// </summary>
    [Fact]
    public async Task UpdateAccountProfile_WithValidToken_UpdatesProfileSuccessfully()
    {
        // Arrange
        var email = $"test_profile_update_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();
        var newName = "Updated Test User";
        var newAvatar = "https://example.com/avatar.png";

        // Track entity for cleanup
        TrackEntity(ownerEntityId);

        // Create account using the Jarvis framework
        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(password);

        var account = new core.jarvis.api.Models.Account
        {
            OwnerEntityId = ownerEntityId,
            Email = email,
            PasswordHash = hashedPassword,
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await TestDataContext().Commit(account);

        // Create initial security profile for the user
        var profileHandler = TestDataContext().For<AccountProfileHandler>(ownerEntityId);
        await profileHandler.CreateWithDefaults(email, "Original Name");

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Authenticate through the auth endpoint to get a valid JWT
        var authBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");
        var authResponse = await client.PostAsync("/api/security/auth", authContent);
        authResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var authResponseBody = await authResponse.Content.ReadAsStringAsync();
        var authToken = JsonSerializer.Deserialize<AuthToken>(authResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        authToken.ShouldNotBeNull();
        authToken.AccessToken.ShouldNotBeNullOrEmpty();

        // Prepare update request
        var updateRequest = new
        {
            Name = newName,
            Avatar = newAvatar,
            Preferences = "{\"theme\": \"dark\"}"
        };

        // Act - Update profile via PUT /accounts/me
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.AccessToken);

        var requestBody = JsonSerializer.Serialize(updateRequest);
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await client.PutAsync("/api/accounts/me", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var updatedProfile = JsonSerializer.Deserialize<SecurityProfile>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        updatedProfile.ShouldNotBeNull();
        updatedProfile.Name.ShouldBe(newName);
        updatedProfile.Avatar.ShouldBe(newAvatar);
        updatedProfile.OwnerEntityId.ShouldBe(ownerEntityId);
    }

    /// <summary>
    /// INTENT: Test that unauthenticated requests are rejected
    /// PURPOSE: Ensure the endpoint enforces authentication requirements
    /// BUSINESS CONTEXT: Profile updates require verified user identity
    /// WHY IMPORTANT: Security requirement - only authenticated users can modify profiles
    /// ARCHITECTURAL SIGNIFICANCE: Tests authentication middleware integration
    /// FUTURE RESILIENCE: Prevents accidental removal of authentication requirements
    /// </summary>
    [Fact]
    public async Task UpdateAccountProfile_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var updateRequest = new
        {
            Name = "Unauthorized User",
            Avatar = "https://example.com/hacker.png"
        };

        // Act - Attempt to update profile without authentication
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var requestBody = JsonSerializer.Serialize(updateRequest);
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await client.PutAsync("/api/accounts/me", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// INTENT: Test that users cannot modify their own role assignments
    /// PURPOSE: Ensure the endpoint protects RoleIds from user modification
    /// BUSINESS CONTEXT: Role assignments are administrative actions, not user self-service
    /// WHY IMPORTANT: Security requirement - prevents privilege escalation attacks
    /// ARCHITECTURAL SIGNIFICANCE: Tests security boundary in AccountProfileHandler
    /// FUTURE RESILIENCE: Ensures role protection remains intact after refactoring
    /// </summary>
    [Fact]
    public async Task UpdateAccountProfile_WithRoleIds_IgnoresRoleChanges()
    {
        // Arrange
        var email = $"test_role_protect_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();
        var newName = "Role Test User";
        var attemptedRoleId = Guid.NewGuid().ToString();

        // Track entity for cleanup
        TrackEntity(ownerEntityId);

        // Create account using the Jarvis framework
        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(password);

        var account = new core.jarvis.api.Models.Account
        {
            OwnerEntityId = ownerEntityId,
            Email = email,
            PasswordHash = hashedPassword,
            Password = "",
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await TestDataContext().Commit(account);

        // Create initial security profile for the user
        var profileHandler = TestDataContext().For<AccountProfileHandler>(ownerEntityId);
        var initialProfile = await profileHandler.CreateWithDefaults(email, "Original Name");

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Authenticate through the auth endpoint to get a valid JWT
        var authBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");
        var authResponse = await client.PostAsync("/api/security/auth", authContent);
        authResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var authResponseBody = await authResponse.Content.ReadAsStringAsync();
        var authToken = JsonSerializer.Deserialize<AuthToken>(authResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        authToken.ShouldNotBeNull();
        authToken.AccessToken.ShouldNotBeNullOrEmpty();

        // Prepare update request attempting to change roles
        var updateRequest = new
        {
            Name = newName,
            RoleIds = new[] { attemptedRoleId, "admin-role-id" }
        };

        // Act - Update profile via PUT /accounts/me with role changes
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.AccessToken);

        var requestBody = JsonSerializer.Serialize(updateRequest);
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await client.PutAsync("/api/accounts/me", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var updatedProfile = JsonSerializer.Deserialize<SecurityProfile>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        updatedProfile.ShouldNotBeNull();
        updatedProfile.Name.ShouldBe(newName);
        // Role IDs should NOT contain the attempted malicious role assignments
        updatedProfile.RoleIds.ShouldNotContain(attemptedRoleId);
        updatedProfile.RoleIds.ShouldNotContain("admin-role-id");
    }
}
