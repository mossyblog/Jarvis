using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using core.jarvis.api.Services;
using core.jarvis.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration.Auth;

/// <summary>
/// INTENT: Test the token validation endpoint thoroughly
/// PURPOSE: Ensure token validation works correctly for valid and invalid tokens
/// BUSINESS CONTEXT: Security-critical endpoint that validates user session tokens
/// WHY IMPORTANT: Prevents unauthorized access through invalid token acceptance
/// ARCHITECTURAL SIGNIFICANCE: Tests authentication validation layer
/// FUTURE RESILIENCE: Prevents token validation regressions
/// </summary>
public class ValidateTokenEndpointTests : ApiIntegrationTestBase
{
    /// <summary>
    /// INTENT: Test successful token validation with valid JWT token
    /// PURPOSE: Ensure the endpoint accepts and validates valid tokens correctly
    /// BUSINESS CONTEXT: Most common use case - validating real user tokens
    /// WHY IMPORTANT: Users must be able to validate their session tokens
    /// ARCHITECTURAL SIGNIFICANCE: Tests happy path of token validation
    /// FUTURE RESILIENCE: Ensures token validation doesn't break with valid tokens
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithValidToken_ReturnsSuccessfulValidation()
    {
        // Arrange
        var email = $"test_validate_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();

        // Track entity for cleanup
        TrackEntity(ownerEntityId);

        // Create account using the Jarvis framework
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

        // Persist the account
        await TestDataContext().Commit(account);

        // Generate a valid token for the user
        var token = TokenService.AccessToken(ownerEntityId, email, new Dictionary<string, string> { { "roles", "user" } });

        // Act - Validate the token via POST to /security/validate
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var requestBody = JsonSerializer.Serialize(new { Token = token });
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/security/validate", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var validationResult = JsonSerializer.Deserialize<TokenValidationResult>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationResult.ShouldNotBeNull();
        validationResult.IsValid.ShouldBeTrue();
        validationResult.UserId.ShouldBe(ownerEntityId.ToString());
        validationResult.Email.ShouldBe(email);
        validationResult.Roles.ShouldNotBeNull();
        validationResult.Roles.ShouldContain("user");
    }

    /// <summary>
    /// INTENT: Test token validation endpoint rejects invalid tokens
    /// PURPOSE: Ensure endpoint does not accept malformed or expired tokens
    /// BUSINESS CONTEXT: Security requirement - only valid tokens should pass
    /// WHY IMPORTANT: Invalid tokens must not be accepted to prevent unauthorized access
    /// ARCHITECTURAL SIGNIFICANCE: Tests security boundary of validation layer
    /// FUTURE RESILIENCE: Ensures invalid tokens are properly rejected
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.signature";

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var requestBody = JsonSerializer.Serialize(new { Token = invalidToken });
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/security/validate", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// INTENT: Test token validation endpoint with GET request and Bearer token
    /// PURPOSE: Ensure endpoint accepts tokens from Authorization header
    /// BUSINESS CONTEXT: Standard HTTP header-based token passing
    /// WHY IMPORTANT: Many clients pass tokens via Authorization header instead of body
    /// ARCHITECTURAL SIGNIFICANCE: Tests header-based token extraction
    /// FUTURE RESILIENCE: Ensures both POST and GET methods work correctly
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithGetRequestAndBearerToken_ReturnsSuccessfulValidation()
    {
        // Arrange
        var email = $"test_get_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();

        // Track entity for cleanup
        TrackEntity(ownerEntityId);

        // Create account using the Jarvis framework
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

        // Persist the account
        await TestDataContext().Commit(account);

        // Generate a valid token for the user
        var token = TokenService.AccessToken(ownerEntityId, email, new Dictionary<string, string> { { "roles", "user" } });

        // Act - Validate the token via GET with Bearer token in Authorization header
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/validate");
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var validationResult = JsonSerializer.Deserialize<TokenValidationResult>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validationResult.ShouldNotBeNull();
        validationResult.IsValid.ShouldBeTrue();
        validationResult.UserId.ShouldBe(ownerEntityId.ToString());
        validationResult.Email.ShouldBe(email);
    }
}
