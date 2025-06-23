using System.Net;
using core.jarvis.api.Functions.Security;
using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration.Functions;

/// <summary>
/// INTENT: Verify DeauthFunction endpoint with real database operations
/// PURPOSE: Ensure logout functionality works end-to-end
/// BUSINESS CONTEXT: Users need secure logout to terminate sessions
/// WHY IMPORTANT: Validates session termination and token revocation
/// ARCHITECTURAL SIGNIFICANCE: Confirms session management integration
/// FUTURE RESILIENCE: Ensures logout remains secure and functional
/// </summary>
public class DeauthFunctionIntegrationTests : ApiIntegrationTestBase, IAsyncLifetime
{
    private AuthFunction _authFunction = null!;
    private DeauthFunction _deauthFunction = null!;
    private bool _initialized = false;
    
    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            await InitializeTestAsync();
            _initialized = true;
        }
    }
    
    private async Task InitializeTestAsync()
    {
        await base.InitializeAsync();
        
        // Create functions with real services
        _authFunction = new AuthFunction(
            AuthenticationService,
            _serviceProvider.GetRequiredService<ILogger<AuthFunction>>()
        );
        
        _deauthFunction = new DeauthFunction(
            AuthenticationService,
            _serviceProvider.GetRequiredService<ILogger<DeauthFunction>>()
        );
    }
    
    /// <summary>
    /// INTENT: Verify successful deauthentication revokes session
    /// PURPOSE: Ensure logout properly terminates sessions
    /// BUSINESS CONTEXT: Security requirement for session management
    /// WHY IMPORTANT: Prevents unauthorized access after logout
    /// ARCHITECTURAL SIGNIFICANCE: Validates token revocation flow
    /// FUTURE RESILIENCE: Maintains logout security
    /// </summary>
    [Fact]
    public async Task Run_With_Valid_SessionId_Should_Return_Ok()
    {
        await EnsureInitializedAsync();
        
        // Arrange - First authenticate to create a session
        var authRequest = new AuthRequest
        {
            Email = "test@example.com",
            Password = "test123",
            ClientId = "test-client"
        };
        
        var authHttpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/auth",
            JsonConvert.SerializeObject(authRequest)
        );
        
        var authResponse = await _authFunction.Run(authHttpRequest);
        authResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var authResponseBody = await TestFactory.GetResponseBodyAsync(authResponse);
        var authResult = JsonConvert.DeserializeObject<AuthResponse>(authResponseBody);
        authResult.ShouldNotBeNull();
        
        TrackEntity(authResult.UserId);
        
        // Now deauthenticate - DeauthFunction expects either a plain GUID or {sessionId: GUID}
        var deauthHttpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/deauth",
            JsonConvert.SerializeObject(new { sessionId = authResult.SessionId })
        );
        
        // Act
        var response = await _deauthFunction.Run(deauthHttpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        // DeauthFunction returns a confirmation GUID
        var confirmationId = JsonConvert.DeserializeObject<Guid>(responseBody);
        confirmationId.ShouldNotBe(Guid.Empty);
    }
    
    /// <summary>
    /// INTENT: Verify non-existent session returns bad request
    /// PURPOSE: Handle invalid logout attempts gracefully
    /// BUSINESS CONTEXT: Prevent errors from invalid requests
    /// WHY IMPORTANT: API stability and clear error messages
    /// ARCHITECTURAL SIGNIFICANCE: Error handling pattern
    /// FUTURE RESILIENCE: Maintains API consistency
    /// </summary>
    [Fact]
    public async Task Run_With_NonExistent_SessionId_Should_Return_NotFound()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var nonExistentSessionId = Guid.NewGuid();
        
        var httpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/deauth",
            JsonConvert.SerializeObject(new { sessionId = nonExistentSessionId })
        );
        
        // Act
        var response = await _deauthFunction.Run(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        responseBody.ShouldContain("Session not found or already revoked");
    }
    
    /// <summary>
    /// INTENT: Verify empty session ID returns bad request
    /// PURPOSE: Validate required fields
    /// BUSINESS CONTEXT: API contract enforcement
    /// WHY IMPORTANT: Prevents invalid operations
    /// ARCHITECTURAL SIGNIFICANCE: Input validation
    /// FUTURE RESILIENCE: Maintains data integrity
    /// </summary>
    [Fact]
    public async Task Run_With_Empty_SessionId_Should_Return_BadRequest()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var httpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/deauth",
            JsonConvert.SerializeObject(new { sessionId = Guid.Empty })
        );
        
        // Act
        var response = await _deauthFunction.Run(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        responseBody.ShouldContain("Session ID cannot be empty");
    }
    
    /// <summary>
    /// INTENT: Verify empty request body returns bad request
    /// PURPOSE: Handle edge cases gracefully
    /// BUSINESS CONTEXT: API robustness
    /// WHY IMPORTANT: Prevents crashes from bad input
    /// ARCHITECTURAL SIGNIFICANCE: Error handling
    /// FUTURE RESILIENCE: Maintains stability
    /// </summary>
    [Fact]
    public async Task Run_With_Empty_Body_Should_Return_BadRequest()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var httpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/deauth",
            ""
        );
        
        // Act
        var response = await _deauthFunction.Run(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        responseBody.ShouldContain("Request body is required");
    }
    
    /// <summary>
    /// INTENT: Verify deauthenticating same session twice handles gracefully
    /// PURPOSE: Idempotent logout operations
    /// BUSINESS CONTEXT: Users may click logout multiple times
    /// WHY IMPORTANT: Prevents confusing error messages
    /// ARCHITECTURAL SIGNIFICANCE: Idempotency pattern
    /// FUTURE RESILIENCE: Improves user experience
    /// </summary>
    [Fact]
    public async Task Run_With_Already_Revoked_Session_Should_Handle_Gracefully()
    {
        await EnsureInitializedAsync();
        
        // Arrange - First authenticate
        var authRequest = new AuthRequest
        {
            Email = "test@example.com",
            Password = "test123"
        };
        
        var authHttpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/auth",
            JsonConvert.SerializeObject(authRequest)
        );
        
        var authResponse = await _authFunction.Run(authHttpRequest);
        var authResponseBody = await TestFactory.GetResponseBodyAsync(authResponse);
        var authResult = JsonConvert.DeserializeObject<AuthResponse>(authResponseBody);
        authResult.ShouldNotBeNull();
        
        TrackEntity(authResult.UserId);
        
        // Deauthenticate once
        var firstDeauthRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/deauth",
            JsonConvert.SerializeObject(new { sessionId = authResult.SessionId })
        );
        
        var firstResponse = await _deauthFunction.Run(firstDeauthRequest);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        // Act - Deauthenticate again
        var secondDeauthRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/deauth",
            JsonConvert.SerializeObject(new { sessionId = authResult.SessionId })
        );
        
        var secondResponse = await _deauthFunction.Run(secondDeauthRequest);
        
        // Assert - Should handle gracefully (NotFound since session is already revoked)
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
    
    // IAsyncLifetime implementation
    public async Task InitializeAsync()
    {
        await EnsureInitializedAsync();
    }
    
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}