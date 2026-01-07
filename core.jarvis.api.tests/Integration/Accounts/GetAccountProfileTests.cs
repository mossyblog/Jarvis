using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration.Accounts;

/// <summary>
/// INTENT: Test GET /accounts/me endpoint for retrieving authenticated user's profile
/// PURPOSE: Ensure users can retrieve their own security profile data
/// BUSINESS CONTEXT: Users need access to their profile information for display and navigation
/// WHY IMPORTANT: Profile endpoint is foundational for user experience and personalization
/// ARCHITECTURAL SIGNIFICANCE: Tests authentication middleware and data context integration
/// FUTURE RESILIENCE: Prevents regression in profile retrieval functionality
/// </summary>
public class GetAccountProfileTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify authenticated user can retrieve their profile
    /// PURPOSE: Test the happy path for profile retrieval with valid JWT
    /// BUSINESS CONTEXT: Users with valid sessions should see their profile data
    /// WHY IMPORTANT: Core functionality for user dashboards and navigation
    /// ARCHITECTURAL SIGNIFICANCE: Tests JWT claims extraction and SecurityProfile query
    /// FUTURE RESILIENCE: Catches breaks in authenticated profile access
    /// </summary>
    [Fact]
    public async Task GetProfile_WithValidJwt_ReturnsSecurityProfile()
    {
        // Arrange - Create user, profile, and authenticate
        var email = $"profile_valid_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

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

        // Create security profile using the handler pattern like other tests
        var profileHandler = TestDataContext().For<AccountProfileHandler>(ownerEntityId);
        await profileHandler.CreateWithDefaults(email, "Test User");

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Authenticate to get valid JWT
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

        // Act - Get profile with valid JWT
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.AccessToken);
        var profileResponse = await client.GetAsync("/api/accounts/me");

        // Assert
        profileResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var profileBody = await profileResponse.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<SecurityProfile>(profileBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        profile.ShouldNotBeNull();
        profile.OwnerEntityId.ShouldBe(ownerEntityId);
        profile.Name.ShouldBe("Test User");
    }

    /// <summary>
    /// INTENT: Verify unauthenticated requests are rejected
    /// PURPOSE: Test security boundary for profile endpoint
    /// BUSINESS CONTEXT: Anonymous users should not access profile data
    /// WHY IMPORTANT: Security gate against unauthorized access
    /// ARCHITECTURAL SIGNIFICANCE: Tests JWT middleware enforcement
    /// FUTURE RESILIENCE: Ensures authentication requirement is never bypassed
    /// </summary>
    [Fact]
    public async Task GetProfile_WithoutJwt_Returns401()
    {
        // Arrange
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act - Attempt to get profile without JWT
        var response = await client.GetAsync("/api/accounts/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// INTENT: Verify 404 when user has no security profile
    /// PURPOSE: Test graceful handling of missing profile data
    /// BUSINESS CONTEXT: New users or corrupted accounts may lack profiles
    /// WHY IMPORTANT: API should return clear error, not crash
    /// ARCHITECTURAL SIGNIFICANCE: Tests null handling in profile query
    /// FUTURE RESILIENCE: Prevents null reference exceptions in production
    /// </summary>
    [Fact]
    public async Task GetProfile_WhenProfileNotExists_Returns404()
    {
        // Arrange - Create user account but NO security profile
        var email = $"profile_missing_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

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

        // Only commit account, NOT security profile
        await TestDataContext().Commit(account);

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Authenticate to get valid JWT
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

        // Act - Get profile with valid JWT but no profile exists
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.AccessToken);
        var profileResponse = await client.GetAsync("/api/accounts/me");

        // Assert
        profileResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
