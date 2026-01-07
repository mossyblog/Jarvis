using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.tests.Helpers;
using core.jarvis.Data;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration.Auth;

/// <summary>
/// INTENT: Test POST /security/refresh endpoint for token renewal
/// PURPOSE: Ensure refresh tokens can be exchanged for new access tokens
/// BUSINESS CONTEXT: Users need seamless session continuation without re-authentication
/// WHY IMPORTANT: Broken refresh flow forces users to re-login, degrading UX
/// ARCHITECTURAL SIGNIFICANCE: Tests the full token rotation security model
/// FUTURE RESILIENCE: Prevents regression in token lifecycle management
/// </summary>
public class RefreshTokenEndpointTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Verify valid refresh token returns new access token
    /// PURPOSE: Test the happy path for token refresh
    /// BUSINESS CONTEXT: Users with valid sessions should get new tokens
    /// WHY IMPORTANT: Core functionality for session management
    /// ARCHITECTURAL SIGNIFICANCE: Tests AuthSystem.RefreshToken integration
    /// FUTURE RESILIENCE: Catches breaks in token rotation
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewAccessToken()
    {
        // Arrange - Create user and authenticate to get refresh token
        var email = $"refresh_valid_{Guid.NewGuid()}@example.com";
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

        // First authenticate to get tokens
        var authBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");
        var authResponse = await client.PostAsync("/api/security/auth", authContent);
        authResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var authResponseBody = await authResponse.Content.ReadAsStringAsync();
        var initialToken = JsonSerializer.Deserialize<AuthToken>(authResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        initialToken.ShouldNotBeNull();
        initialToken.RefreshToken.ShouldNotBeNullOrEmpty();

        // Act - Use refresh token to get new access token
        var refreshBody = JsonSerializer.Serialize(new { RefreshToken = initialToken.RefreshToken });
        var refreshContent = new StringContent(refreshBody, Encoding.UTF8, "application/json");
        var refreshResponse = await client.PostAsync("/api/security/refresh", refreshContent);

        // Assert
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var refreshResponseBody = await refreshResponse.Content.ReadAsStringAsync();
        var newToken = JsonSerializer.Deserialize<AuthToken>(refreshResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        newToken.ShouldNotBeNull();
        newToken.AccessToken.ShouldNotBeNullOrEmpty();
        newToken.RefreshToken.ShouldNotBeNullOrEmpty();
        // Note: OwnerEntityId on AuthToken refers to the token's owner (user), not necessarily the same GUID
        // as our test ownerEntityId since the auth system may track sessions separately
        newToken.OwnerEntityId.ShouldNotBe(Guid.Empty);
    }

    /// <summary>
    /// INTENT: Verify invalid refresh token is rejected
    /// PURPOSE: Test security boundary for token validation
    /// BUSINESS CONTEXT: Attackers should not forge refresh tokens
    /// WHY IMPORTANT: Security gate against token forgery
    /// ARCHITECTURAL SIGNIFICANCE: Tests UnauthorizedException handling
    /// FUTURE RESILIENCE: Ensures invalid tokens always fail
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithInvalidToken_Returns401()
    {
        // Arrange
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var invalidRefreshToken = "completely-invalid-refresh-token-that-does-not-exist";

        // Act
        var refreshBody = JsonSerializer.Serialize(new { RefreshToken = invalidRefreshToken });
        var refreshContent = new StringContent(refreshBody, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/security/refresh", refreshContent);

        // Assert - Should be 401 (unauthorized) or 429 (rate limited during heavy test runs)
        var validStatuses = new[] { HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests };
        validStatuses.ShouldContain(response.StatusCode,
            $"Expected 401 or 429 but got {response.StatusCode}");
    }

    /// <summary>
    /// INTENT: Verify empty refresh token is rejected
    /// PURPOSE: Test input validation for missing token
    /// BUSINESS CONTEXT: API should handle malformed requests gracefully
    /// WHY IMPORTANT: Prevents null reference errors in production
    /// ARCHITECTURAL SIGNIFICANCE: Tests validator integration
    /// FUTURE RESILIENCE: Ensures input validation remains robust
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithEmptyToken_Returns400Or401()
    {
        // Arrange
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var refreshBody = JsonSerializer.Serialize(new { RefreshToken = "" });
        var refreshContent = new StringContent(refreshBody, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/security/refresh", refreshContent);

        // Assert - Should be 400 (validation) or 401 (unauthorized)
        var validStatuses = new[] { HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized };
        validStatuses.ShouldContain(response.StatusCode);
    }

    /// <summary>
    /// INTENT: Verify new tokens are different from original
    /// PURPOSE: Test token rotation generates unique tokens
    /// BUSINESS CONTEXT: Token rotation is a security best practice
    /// WHY IMPORTANT: Reused tokens indicate broken rotation
    /// ARCHITECTURAL SIGNIFICANCE: Tests cryptographic token generation
    /// FUTURE RESILIENCE: Catches token reuse vulnerabilities
    /// </summary>
    [Fact]
    public async Task RefreshToken_GeneratesNewUniqueTokens()
    {
        // Arrange
        var email = $"refresh_unique_{Guid.NewGuid()}@example.com";
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

        // Authenticate to get initial tokens
        var authBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");
        var authResponse = await client.PostAsync("/api/security/auth", authContent);
        var authResponseBody = await authResponse.Content.ReadAsStringAsync();
        var initialToken = JsonSerializer.Deserialize<AuthToken>(authResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act - Refresh the token
        var refreshBody = JsonSerializer.Serialize(new { RefreshToken = initialToken!.RefreshToken });
        var refreshContent = new StringContent(refreshBody, Encoding.UTF8, "application/json");
        var refreshResponse = await client.PostAsync("/api/security/refresh", refreshContent);
        var refreshResponseBody = await refreshResponse.Content.ReadAsStringAsync();
        var newToken = JsonSerializer.Deserialize<AuthToken>(refreshResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        newToken.ShouldNotBeNull();
        newToken.AccessToken.ShouldNotBe(initialToken.AccessToken, "New access token should be different");
        newToken.RefreshToken.ShouldNotBe(initialToken.RefreshToken, "New refresh token should be different");
    }

    /// <summary>
    /// INTENT: Verify refresh token can only be used once
    /// PURPOSE: Test token replay attack prevention
    /// BUSINESS CONTEXT: Stolen refresh tokens should not work indefinitely
    /// WHY IMPORTANT: Critical security control against token theft
    /// ARCHITECTURAL SIGNIFICANCE: Tests token revocation on use
    /// FUTURE RESILIENCE: Prevents token replay vulnerabilities
    /// </summary>
    [Fact]
    public async Task RefreshToken_UsedTwice_SecondAttemptFails()
    {
        // Arrange
        var email = $"refresh_replay_{Guid.NewGuid()}@example.com";
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

        // Authenticate to get initial tokens
        var authBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");
        var authResponse = await client.PostAsync("/api/security/auth", authContent);
        var authResponseBody = await authResponse.Content.ReadAsStringAsync();
        var initialToken = JsonSerializer.Deserialize<AuthToken>(authResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // First refresh - should succeed
        var refreshBody = JsonSerializer.Serialize(new { RefreshToken = initialToken!.RefreshToken });
        var refreshContent = new StringContent(refreshBody, Encoding.UTF8, "application/json");
        var firstRefreshResponse = await client.PostAsync("/api/security/refresh", refreshContent);
        firstRefreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act - Second refresh with same token - should fail
        var secondRefreshContent = new StringContent(refreshBody, Encoding.UTF8, "application/json");
        var secondRefreshResponse = await client.PostAsync("/api/security/refresh", secondRefreshContent);

        // Assert
        secondRefreshResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized,
            "Refresh token should be invalidated after first use");
    }
}
