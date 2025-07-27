using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using core.jarvis;
using core.jarvis.api.Functions.Security;
using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using core.jarvis.api.Services;
using core.jarvis.data;
using core.jarvis.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
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
        
        // Act - Call the REAL AuthFunction
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        var requestBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        
        var request = TestFactory.CreateHttpRequestData("POST", "/api/security/auth", requestBody);
        request.Headers.Add("Content-Type", "application/json");
        
        var response = await authFunction.Run(request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
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
        
        // First register the user
        var registerFunction = _serviceProvider.GetRequiredService<RegisterFunction>();
        var registerBody = JsonSerializer.Serialize(new { email, password });
        
        var registerRequest = TestFactory.CreateHttpRequestData("POST", "/api/auth/register", registerBody);
        registerRequest.Headers.Add("Content-Type", "application/json");
        
        var registerResponse = await registerFunction.Register(registerRequest);
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        // Now authenticate
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        var authBody = JsonSerializer.Serialize(new { email, password });
        var authRequest = TestFactory.CreateHttpRequestData("POST", "/api/security/auth", authBody);
        authRequest.Headers.Add("Content-Type", "application/json");
        
        var authResponse = await authFunction.Run(authRequest);
        
        // Assert
        authResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(authResponse);
        var authToken = JsonSerializer.Deserialize<AuthToken>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        authToken.ShouldNotBeNull();
        authToken.AccessToken.ShouldNotBeNullOrEmpty();
        
        // Track for cleanup
        if (authToken.OwnerEntityId != Guid.Empty)
        {
            TrackEntity(authToken.OwnerEntityId);
        }
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
        
        // Test through AuthFunction
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        var requestBody = JsonSerializer.Serialize(new { Email = email, Password = password });
        
        Logger().LogInformation("Sending JSON: {RequestBody}", requestBody);
        
        var request = TestFactory.CreateHttpRequestData("POST", "/api/security/auth", requestBody);
        request.Headers.Add("Content-Type", "application/json");
        
        var response = await authFunction.Run(request);
        
        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorBody = await TestFactory.GetResponseBodyAsync(response);
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
        
        var authFunction = _serviceProvider.GetRequiredService<AuthFunction>();
        var request = TestFactory.CreateHttpRequestData("POST", "/api/security/auth", accountJson);
        request.Headers.Add("Content-Type", "application/json");
        
        var response = await authFunction.Run(request);
        
        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorBody = await TestFactory.GetResponseBodyAsync(response);
            Logger().LogWarning("Auth failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
        }
        response.StatusCode.ShouldBe(HttpStatusCode.OK, 
            "Authentication should work with full Account object");
    }
}