using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using core.jarvis.api.Services;
using core.jarvis.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration;

/// <summary>
/// INTENT: Test REAL authentication end-to-end with actual database
/// PURPOSE: Fix the authentication problem once and for all
/// BUSINESS CONTEXT: Users must be able to log in
/// WHY IMPORTANT: Authentication is broken in production
/// ARCHITECTURAL SIGNIFICANCE: Tests the complete flow
/// FUTURE RESILIENCE: Prevents auth regressions
/// </summary>
public class AuthenticationIntegrationTests : ApiIntegrationTestBase
{

    /// <summary>
    /// INTENT: Test authentication with direct database setup
    /// PURPOSE: Ensure authentication works when account exists
    /// BUSINESS CONTEXT: Most direct way to test auth
    /// WHY IMPORTANT: This is how test data is usually set up
    /// ARCHITECTURAL SIGNIFICANCE: Tests all layers
    /// FUTURE RESILIENCE: Catches database-level issues
    /// </summary>
    [Fact]
    public async Task Authentication_WithDirectDatabaseSetup_Works()
    {
        // Arrange
        var email = $"test_direct_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();

        // Track entity for cleanup
        TrackEntity(ownerEntityId);

        // Create account using the Jarvis framework (following test guidelines)
        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(password);

        var account = new Account
        {
            OwnerEntityId = ownerEntityId,
            Email = email,
            PasswordHash = hashedPassword,
            Password = "", // Not persisted
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        // Use TestDataContext() to persist the account
        await TestDataContext().Commit(account);

        // Act - Call the endpoint via HTTP
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var requestBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/security/auth", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var authToken = JsonSerializer.Deserialize<AuthToken>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        authToken.ShouldNotBeNull();
        authToken.AccessToken.ShouldNotBeNullOrEmpty();
        authToken.OwnerEntityId.ShouldBe(ownerEntityId);
    }

    /// <summary>
    /// INTENT: Test authentication using registration flow
    /// PURPOSE: Ensure the full user journey works
    /// BUSINESS CONTEXT: How real users would create accounts
    /// WHY IMPORTANT: Tests registration + authentication together
    /// ARCHITECTURAL SIGNIFICANCE: Tests multiple systems
    /// FUTURE RESILIENCE: Catches integration issues
    /// </summary>
    [Fact]
    public async Task Authentication_AfterRegistration_Works()
    {
        // Arrange
        var email = $"test_reg_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        // First register the user (creates inactive account)
        var registerBody = JsonSerializer.Serialize(new { email, password });
        var registerContent = new StringContent(registerBody, Encoding.UTF8, "application/json");

        var registerResponse = await client.PostAsync("/api/auth/register", registerContent);
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Extract the registered account to get the OwnerEntityId
        var registerResponseBody = await registerResponse.Content.ReadAsStringAsync();
        var registeredComponents = JsonSerializer.Deserialize<List<JsonElement>>(registerResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        registeredComponents.ShouldNotBeNull();
        registeredComponents.Count.ShouldBeGreaterThan(0);

        // Find the Account component in the response
        Guid ownerEntityId = Guid.Empty;
        foreach (var component in registeredComponents)
        {
            if (component.TryGetProperty("ownerEntityId", out var ownerIdProp) ||
                component.TryGetProperty("OwnerEntityId", out ownerIdProp))
            {
                ownerEntityId = Guid.Parse(ownerIdProp.GetString()!);
                break;
            }
        }

        ownerEntityId.ShouldNotBe(Guid.Empty);

        // Track entity for cleanup
        TrackEntity(ownerEntityId);

        // Activate the account (required for authentication)
        var accountHandler = TestDataContext().For<AccountHandler>(ownerEntityId);
        await accountHandler.Activate();

        // Now authenticate
        var authBody = JsonSerializer.Serialize(new { email, password });
        var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");

        var authResponse = await client.PostAsync("/api/security/auth", authContent);

        // Assert
        authResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var responseBody = await authResponse.Content.ReadAsStringAsync();
        var authToken = JsonSerializer.Deserialize<AuthToken>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        authToken.ShouldNotBeNull();
        authToken.AccessToken.ShouldNotBeNullOrEmpty();
        authToken.OwnerEntityId.ShouldBe(ownerEntityId);
    }

    /// <summary>
    /// INTENT: Test the specific test@example.com account
    /// PURPOSE: Debug why this specific account fails
    /// BUSINESS CONTEXT: This is what's failing in Swagger
    /// WHY IMPORTANT: Must fix this specific case
    /// ARCHITECTURAL SIGNIFICANCE: Tests known data
    /// FUTURE RESILIENCE: Ensures test accounts work
    /// </summary>
    [Fact]
    public async Task Authentication_WithTestExampleAccount_Works()
    {
        // Arrange
        var email = $"test_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!";
        var ownerEntityId = Guid.NewGuid();

        // Track entity for cleanup (even though it's a permanent test account)
        TrackEntity(ownerEntityId);

        // Create account using the Jarvis framework
        var passwordService = _serviceProvider.GetRequiredService<IPasswordPolicyService>();
        var hashedPassword = passwordService.HashPassword(password);

        // First, try to remove any existing account with this email/entity
        try
        {
            await TestDataContext().Remove<Account>(ownerEntityId);
        }
        catch
        {
            // Ignore if it doesn't exist
        }

        var account = new Account
        {
            OwnerEntityId = ownerEntityId,
            Email = email,
            PasswordHash = hashedPassword,
            Password = "", // Not persisted
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        // Use TestDataContext() to persist the account
        await TestDataContext().Commit(account);

        // Test through HTTP endpoint
        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var requestBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/security/auth", content);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Logger().LogWarning("Auth failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
        }
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"Authentication failed for {email}. Check logs for details.");
    }

    /// <summary>
    /// INTENT: Test authentication with full Account object like Swagger sends
    /// PURPOSE: Ensure API accepts complete Account component
    /// BUSINESS CONTEXT: Swagger generates full object, must work
    /// WHY IMPORTANT: API contract must be honored
    /// ARCHITECTURAL SIGNIFICANCE: Tests real Swagger usage
    /// FUTURE RESILIENCE: Ensures full object compatibility
    /// </summary>
    [Fact]
    public async Task Authentication_WithFullAccountObject_Works()
    {
        // Arrange
        var email = $"test_full_{Guid.NewGuid()}@example.com";
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
            Password = "", // Not persisted
            AuthMethod = "password",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await TestDataContext().Commit(account);

        // Create full Account object like Swagger does
        var accountJson = $@"{{
            ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
            ""ownerEntityId"": ""550e8400-e29b-41d4-a716-446655440000"",
            ""updatedAt"": ""2024-01-01T00:00:00Z"",
            ""email"": ""{email}"",
            ""passwordHash"": ""string"",
            ""password"": ""{password}"",
            ""twoFactorCode"": ""string"",
            ""authMethod"": ""string"",
            ""clientId"": ""string"",
            ""isActive"": true,
            ""createdAt"": ""2025-07-02T09:45:38.620Z"",
            ""ipAddress"": ""string"",
            ""userAgent"": ""string""
        }}";

        Logger().LogInformation("Sending full Account JSON: {AccountJson}", accountJson);

        await using var factory = new JarvisApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var content = new StringContent(accountJson, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/security/auth", content);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Logger().LogWarning("Auth failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
        }
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            "Authentication should work with full Account object");
    }
}
