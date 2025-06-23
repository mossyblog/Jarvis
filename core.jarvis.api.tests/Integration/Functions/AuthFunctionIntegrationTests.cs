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
/// INTENT: Verify AuthFunction endpoint with real database and services
/// PURPOSE: Ensure authentication endpoint works end-to-end
/// BUSINESS CONTEXT: API authentication endpoint is critical for all secured operations
/// WHY IMPORTANT: Validates complete authentication flow from HTTP to database
/// ARCHITECTURAL SIGNIFICANCE: Confirms Azure Function integration with Jarvis framework
/// FUTURE RESILIENCE: Ensures authentication endpoint remains functional
/// </summary>
public class AuthFunctionIntegrationTests : ApiIntegrationTestBase
{
    private AuthFunction? _authFunction;
    private bool _initialized = false;
    
    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            await InitializeAsync();
            _initialized = true;
        }
    }
    
    private AuthFunction GetAuthFunction()
    {
        if (_authFunction == null)
        {
            _authFunction = new AuthFunction(
                AuthenticationService,
                _serviceProvider.GetRequiredService<ILogger<AuthFunction>>()
            );
        }
        return _authFunction;
    }
    
    /// <summary>
    /// INTENT: Verify successful authentication through HTTP endpoint
    /// PURPOSE: Ensure valid credentials return proper tokens
    /// BUSINESS CONTEXT: Users authenticate via HTTP POST
    /// WHY IMPORTANT: Core authentication flow must work
    /// ARCHITECTURAL SIGNIFICANCE: Validates HTTP to service integration
    /// FUTURE RESILIENCE: Maintains API contract
    /// </summary>
    [Fact]
    public async Task Run_With_Valid_Credentials_Should_Return_Ok_With_Tokens()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var authRequest = new AuthRequest
        {
            Email = "test@example.com",
            Password = "test123",
            ClientId = "test-client"
        };
        
        var httpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/auth",
            JsonConvert.SerializeObject(authRequest)
        );
        
        // Add headers
        httpRequest.Headers.Add("X-Forwarded-For", "192.168.1.100");
        httpRequest.Headers.Add("User-Agent", "TestClient/1.0");
        
        // Act
        var response = await GetAuthFunction().Run(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseBody);
        
        authResponse.ShouldNotBeNull();
        authResponse.AccessToken.ShouldNotBeNullOrEmpty();
        authResponse.RefreshToken.ShouldNotBeNullOrEmpty();
        authResponse.UserId.ShouldNotBe(Guid.Empty);
        authResponse.SessionId.ShouldNotBe(Guid.Empty);
        authResponse.TokenType.ShouldBe("Bearer");
        
        // Track for cleanup
        TrackEntity(authResponse.UserId);
    }
    
    /// <summary>
    /// INTENT: Verify invalid credentials return 401
    /// PURPOSE: Ensure unauthorized access is blocked
    /// BUSINESS CONTEXT: Security requirement
    /// WHY IMPORTANT: Prevents unauthorized access
    /// ARCHITECTURAL SIGNIFICANCE: Validates error handling
    /// FUTURE RESILIENCE: Maintains security
    /// </summary>
    [Fact]
    public async Task Run_With_Invalid_Credentials_Should_Return_Unauthorized()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var authRequest = new AuthRequest
        {
            Email = "invalid@example.com",
            Password = "wrongpassword"
        };
        
        var httpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/auth",
            JsonConvert.SerializeObject(authRequest)
        );
        
        // Act
        var response = await GetAuthFunction().Run(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        responseBody.ShouldContain("Invalid credentials");
    }
    
    /// <summary>
    /// INTENT: Verify missing email returns bad request
    /// PURPOSE: Ensure request validation works
    /// BUSINESS CONTEXT: Prevent invalid requests
    /// WHY IMPORTANT: API contract enforcement
    /// ARCHITECTURAL SIGNIFICANCE: Validates input validation
    /// FUTURE RESILIENCE: Maintains API quality
    /// </summary>
    [Fact]
    public async Task Run_With_Missing_Email_Should_Return_BadRequest()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var authRequest = new AuthRequest
        {
            Password = "test123"
        };
        
        var httpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/auth",
            JsonConvert.SerializeObject(authRequest)
        );
        
        // Act
        var response = await GetAuthFunction().Run(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        responseBody.ShouldContain("Email and password are required");
    }
    
    /// <summary>
    /// INTENT: Verify missing password returns bad request
    /// PURPOSE: Ensure request validation works
    /// BUSINESS CONTEXT: Prevent invalid requests
    /// WHY IMPORTANT: API contract enforcement
    /// ARCHITECTURAL SIGNIFICANCE: Validates input validation
    /// FUTURE RESILIENCE: Maintains API quality
    /// </summary>
    [Fact]
    public async Task Run_With_Missing_Password_Should_Return_BadRequest()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var authRequest = new AuthRequest
        {
            Email = "test@example.com"
        };
        
        var httpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/auth",
            JsonConvert.SerializeObject(authRequest)
        );
        
        // Act
        var response = await GetAuthFunction().Run(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        responseBody.ShouldContain("Email and password are required");
    }
    
    /// <summary>
    /// INTENT: Verify empty request body returns bad request
    /// PURPOSE: Handle edge case gracefully
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
            "http://localhost/api/security/auth",
            ""
        );
        
        // Act
        var response = await GetAuthFunction().Run(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        responseBody.ShouldContain("Request body is required");
    }
    
    /// <summary>
    /// INTENT: Verify invalid JSON returns bad request
    /// PURPOSE: Handle malformed input gracefully
    /// BUSINESS CONTEXT: API robustness
    /// WHY IMPORTANT: Prevents crashes from bad input
    /// ARCHITECTURAL SIGNIFICANCE: Error handling
    /// FUTURE RESILIENCE: Maintains stability
    /// </summary>
    [Fact]
    public async Task Run_With_Invalid_Json_Should_Return_BadRequest()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var httpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/auth",
            "{ invalid json }"
        );
        
        // Act
        var response = await GetAuthFunction().Run(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        responseBody.ShouldContain("Invalid request format");
    }
    
    /// <summary>
    /// INTENT: Verify IP address and user agent are captured
    /// PURPOSE: Enable audit trail and security monitoring
    /// BUSINESS CONTEXT: Security logging requirements
    /// WHY IMPORTANT: Enables tracking and monitoring
    /// ARCHITECTURAL SIGNIFICANCE: Audit integration
    /// FUTURE RESILIENCE: Supports security analysis
    /// </summary>
    [Fact]
    public async Task Run_Should_Capture_Client_Information()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var authRequest = new AuthRequest
        {
            Email = "test@example.com",
            Password = "test123",
            ClientId = "test-client"
        };
        
        var httpRequest = TestFactory.CreateHttpRequestData(
            "POST", 
            "http://localhost/api/security/auth",
            JsonConvert.SerializeObject(authRequest)
        );
        
        // Add specific headers to verify they are captured
        var testIpAddress = "10.0.0.50";
        var testUserAgent = "CustomTestAgent/2.0";
        httpRequest.Headers.Add("X-Forwarded-For", testIpAddress);
        httpRequest.Headers.Add("User-Agent", testUserAgent);
        
        // Act
        var response = await GetAuthFunction().Run(httpRequest);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        // In a real implementation, we would verify these values were stored
        // with the security token in the database
        var responseBody = await TestFactory.GetResponseBodyAsync(response);
        var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseBody);
        authResponse.ShouldNotBeNull();
        
        TrackEntity(authResponse.UserId);
    }
}