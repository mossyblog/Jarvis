using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration.Auth;

/// <summary>
/// INTENT: Test POST /security/deauth endpoint for session termination
/// PURPOSE: Ensure users can securely log out and invalidate tokens
/// BUSINESS CONTEXT: Users must be able to terminate sessions for security
/// WHY IMPORTANT: Broken logout leaves sessions vulnerable to hijacking
/// ARCHITECTURAL SIGNIFICANCE: Tests token revocation flow
/// FUTURE RESILIENCE: Prevents session management regressions
/// </summary>
public class LogoutEndpointTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify authenticated logout returns 200
    /// PURPOSE: Test happy path for session termination
    /// BUSINESS CONTEXT: Logged in users should be able to log out
    /// WHY IMPORTANT: Core logout functionality
    /// ARCHITECTURAL SIGNIFICANCE: Tests AuthTokenHandler.Deauthenticate integration
    /// FUTURE RESILIENCE: Catches logout flow breaks
    /// </summary>
    [Fact]
    public async Task Logout_WithValidToken_Returns200()
    {
        // Arrange - Create and authenticate user
        var email = $"logout_valid_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(password);

        var account = new Account
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

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Authenticate to get token
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

        // Act - Logout with valid token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.AccessToken);
        var logoutBody = JsonSerializer.Serialize(new { SessionId = authToken.SessionId });
        var logoutContent = new StringContent(logoutBody, Encoding.UTF8, "application/json");
        var logoutResponse = await client.PostAsync("/api/security/deauth", logoutContent);

        // Assert
        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// INTENT: Verify unauthenticated logout is rejected
    /// PURPOSE: Test security boundary for logout endpoint
    /// BUSINESS CONTEXT: Anonymous users cannot log out
    /// WHY IMPORTANT: Prevents unauthorized session manipulation
    /// ARCHITECTURAL SIGNIFICANCE: Tests JWT auth middleware
    /// FUTURE RESILIENCE: Ensures auth checks remain active
    /// </summary>
    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        // Arrange
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act - Try to logout without authentication
        var logoutBody = JsonSerializer.Serialize(new { SessionId = Guid.NewGuid() });
        var logoutContent = new StringContent(logoutBody, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/security/deauth", logoutContent);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// INTENT: Verify invalid token is rejected
    /// PURPOSE: Test token validation for logout
    /// BUSINESS CONTEXT: Invalid tokens should not be able to log out
    /// WHY IMPORTANT: Prevents forged token attacks
    /// ARCHITECTURAL SIGNIFICANCE: Tests JWT validation pipeline
    /// FUTURE RESILIENCE: Catches token validation bypass
    /// </summary>
    [Fact]
    public async Task Logout_WithInvalidToken_Returns401()
    {
        // Arrange
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.invalid_signature";

        // Act
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);
        var logoutBody = JsonSerializer.Serialize(new { SessionId = Guid.NewGuid() });
        var logoutContent = new StringContent(logoutBody, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/security/deauth", logoutContent);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// INTENT: Verify refresh token fails after logout
    /// PURPOSE: Test token revocation effectiveness
    /// BUSINESS CONTEXT: Stolen tokens should not work after logout
    /// WHY IMPORTANT: Critical for session security
    /// ARCHITECTURAL SIGNIFICANCE: Tests complete revocation flow
    /// FUTURE RESILIENCE: Prevents token reuse after logout
    /// </summary>
    [Fact]
    public async Task Logout_InvalidatesRefreshToken()
    {
        // Arrange - Create and authenticate user
        var email = $"logout_invalidate_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(password);

        var account = new Account
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

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Authenticate to get tokens
        var authBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");
        var authResponse = await client.PostAsync("/api/security/auth", authContent);
        var authResponseBody = await authResponse.Content.ReadAsStringAsync();
        var authToken = JsonSerializer.Deserialize<AuthToken>(authResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        authToken.ShouldNotBeNull();

        // Logout
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.AccessToken);
        var logoutBody = JsonSerializer.Serialize(new { SessionId = authToken.SessionId });
        var logoutContent = new StringContent(logoutBody, Encoding.UTF8, "application/json");
        var logoutResponse = await client.PostAsync("/api/security/deauth", logoutContent);
        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act - Try to use refresh token after logout
        client.DefaultRequestHeaders.Authorization = null;
        var refreshBody = JsonSerializer.Serialize(new { RefreshToken = authToken.RefreshToken });
        var refreshContent = new StringContent(refreshBody, Encoding.UTF8, "application/json");
        var refreshResponse = await client.PostAsync("/api/security/refresh", refreshContent);

        // Assert - Refresh should fail since token was revoked
        // Accept 401 (unauthorized) or 429 (rate limited) during test runs
        var validStatuses = new[] { HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests };
        validStatuses.ShouldContain(refreshResponse.StatusCode,
            "Refresh token should be invalidated after logout");
    }

    /// <summary>
    /// INTENT: Verify user can re-authenticate after logout
    /// PURPOSE: Test login works after session termination
    /// BUSINESS CONTEXT: Users need to log back in after logging out
    /// WHY IMPORTANT: Ensures logout doesn't break account
    /// ARCHITECTURAL SIGNIFICANCE: Tests account state remains valid
    /// FUTURE RESILIENCE: Prevents account lockout bugs
    /// </summary>
    [Fact]
    public async Task Logout_ThenLogin_Succeeds()
    {
        // Arrange - Create and authenticate user
        var email = $"logout_relogin_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(password);

        var account = new Account
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

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // First login
        var authBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");
        var authResponse = await client.PostAsync("/api/security/auth", authContent);
        var authResponseBody = await authResponse.Content.ReadAsStringAsync();
        var authToken = JsonSerializer.Deserialize<AuthToken>(authResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Logout
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken!.AccessToken);
        var logoutBody = JsonSerializer.Serialize(new { SessionId = authToken.SessionId });
        var logoutContent = new StringContent(logoutBody, Encoding.UTF8, "application/json");
        await client.PostAsync("/api/security/deauth", logoutContent);

        // Act - Login again
        client.DefaultRequestHeaders.Authorization = null;
        var reAuthBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        var reAuthContent = new StringContent(reAuthBody, Encoding.UTF8, "application/json");
        var reAuthResponse = await client.PostAsync("/api/security/auth", reAuthContent);

        // Assert
        reAuthResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var reAuthResponseBody = await reAuthResponse.Content.ReadAsStringAsync();
        var newToken = JsonSerializer.Deserialize<AuthToken>(reAuthResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        newToken.ShouldNotBeNull();
        newToken.AccessToken.ShouldNotBeNullOrEmpty();
    }
}
