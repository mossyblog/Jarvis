using core.jarvis.api.Models;
using core.jarvis.api.tests.Helpers;
using Shouldly;
using Xunit;

namespace core.jarvis.api.tests.Integration.Services;

/// <summary>
/// INTENT: Verify authentication service with real database operations
/// PURPOSE: Ensure authentication service correctly handles auth operations against actual database
/// BUSINESS CONTEXT: Core security service for API authentication with database persistence
/// WHY IMPORTANT: Validates end-to-end authentication flow including database interactions
/// ARCHITECTURAL SIGNIFICANCE: Confirms integration between auth service, token service, and database
/// FUTURE RESILIENCE: Protects against authentication issues that only appear with real database
/// </summary>
public class AuthenticationServiceIntegrationTests : ApiIntegrationTestBase
{
    private bool _initialized = false;
    
    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            await InitializeAsync();
            _initialized = true;
        }
    }
    
    /// <summary>
    /// INTENT: Verify successful authentication creates session in database
    /// PURPOSE: Ensure auth flow persists security tokens correctly
    /// BUSINESS CONTEXT: Users need persistent sessions for API access
    /// WHY IMPORTANT: Validates token storage and retrieval mechanism
    /// ARCHITECTURAL SIGNIFICANCE: Confirms security token handler integration
    /// FUTURE RESILIENCE: Ensures session persistence works correctly
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_With_Valid_Credentials_Should_Return_AuthResponse_And_Store_Session()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var authRequest = CreateTestAuthRequest();
        var ipAddress = "192.168.1.1";
        var userAgent = "TestAgent/1.0";
        
        // Act
        var authResponse = await AuthenticationService.AuthenticateAsync(authRequest, ipAddress, userAgent);
        
        // Assert
        authResponse.ShouldNotBeNull();
        authResponse.AccessToken.ShouldNotBeNullOrEmpty();
        authResponse.RefreshToken.ShouldNotBeNullOrEmpty();
        authResponse.UserId.ShouldNotBe(Guid.Empty);
        authResponse.SessionId.ShouldNotBe(Guid.Empty);
        authResponse.ExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow);
        authResponse.TokenType.ShouldBe("Bearer");
        
        // Track for cleanup
        TrackEntity(authResponse.UserId);
    }
    
    /// <summary>
    /// INTENT: Verify invalid credentials are rejected
    /// PURPOSE: Ensure security against unauthorized access
    /// BUSINESS CONTEXT: Prevent unauthorized API access
    /// WHY IMPORTANT: Core security requirement
    /// ARCHITECTURAL SIGNIFICANCE: Validates PgClient authentication
    /// FUTURE RESILIENCE: Maintains security integrity
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_With_Invalid_Credentials_Should_Return_Null()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var authRequest = CreateTestAuthRequest(email: "invalid@example.com", password: "wrongpassword");
        
        // Act
        var authResponse = await AuthenticationService.AuthenticateAsync(authRequest);
        
        // Assert
        authResponse.ShouldBeNull();
    }
    
    /// <summary>
    /// INTENT: Verify session deauthentication marks token as revoked
    /// PURPOSE: Ensure sessions can be properly terminated
    /// BUSINESS CONTEXT: Users need ability to logout securely
    /// WHY IMPORTANT: Prevents session hijacking after logout
    /// ARCHITECTURAL SIGNIFICANCE: Validates token revocation mechanism
    /// FUTURE RESILIENCE: Ensures logout remains secure
    /// </summary>
    [Fact]
    public async Task DeauthenticateAsync_Should_Revoke_Session_Token()
    {
        await EnsureInitializedAsync();
        
        // Arrange - First authenticate to create a session
        var authRequest = CreateTestAuthRequest();
        var authResponse = await AuthenticationService.AuthenticateAsync(authRequest);
        authResponse.ShouldNotBeNull();
        
        TrackEntity(authResponse.UserId);
        
        // Act
        var deauthResult = await AuthenticationService.DeauthenticateAsync(authResponse.SessionId);
        
        // Assert
        deauthResult.ShouldBeTrue();
        
        // Verify token is revoked by trying to validate it
        var validationResponse = await AuthenticationService.ValidateTokenAsync(authResponse.OwnerEntityId);
        validationResponse.IsValid.ShouldBeFalse();
    }
    
    /// <summary>
    /// INTENT: Verify refresh token flow generates new tokens
    /// PURPOSE: Enable token refresh without re-authentication
    /// BUSINESS CONTEXT: Long-lived sessions with short-lived access tokens
    /// WHY IMPORTANT: Balances security with user experience
    /// ARCHITECTURAL SIGNIFICANCE: Validates token rotation mechanism
    /// FUTURE RESILIENCE: Ensures refresh flow remains functional
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_With_Valid_Token_Should_Return_New_Tokens()
    {
        await EnsureInitializedAsync();
        
        // Arrange - First authenticate to get refresh token
        var authRequest = CreateTestAuthRequest();
        var authResponse = await AuthenticationService.AuthenticateAsync(authRequest);
        authResponse.ShouldNotBeNull();
        
        TrackEntity(authResponse.UserId);
        
        var refreshRequest = CreateTestRefreshTokenRequest(authResponse.RefreshToken, authRequest.ClientId);
        
        // Act
        var refreshResponse = await AuthenticationService.RefreshTokenAsync(refreshRequest);
        
        // Assert
        refreshResponse.ShouldNotBeNull();
        refreshResponse.AccessToken.ShouldNotBeNullOrEmpty();
        refreshResponse.RefreshToken.ShouldNotBeNullOrEmpty();
        refreshResponse.AccessToken.ShouldNotBe(authResponse.AccessToken); // New access token
        refreshResponse.RefreshToken.ShouldNotBe(authResponse.RefreshToken); // New refresh token
        refreshResponse.UserId.ShouldBe(authResponse.UserId);
        refreshResponse.SessionId.ShouldBe(authResponse.SessionId); // Same session
    }
    
    /// <summary>
    /// INTENT: Verify expired refresh tokens are rejected
    /// PURPOSE: Ensure token expiration is enforced
    /// BUSINESS CONTEXT: Prevent use of old tokens
    /// WHY IMPORTANT: Security requirement for token lifecycle
    /// ARCHITECTURAL SIGNIFICANCE: Validates token expiration logic
    /// FUTURE RESILIENCE: Maintains token security over time
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_With_Invalid_Token_Should_Return_Null()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var refreshRequest = CreateTestRefreshTokenRequest("invalid-refresh-token");
        
        // Act
        var refreshResponse = await AuthenticationService.RefreshTokenAsync(refreshRequest);
        
        // Assert
        refreshResponse.ShouldBeNull();
    }
    
    /// <summary>
    /// INTENT: Verify client ID validation during refresh
    /// PURPOSE: Ensure tokens are bound to specific clients
    /// BUSINESS CONTEXT: Multi-client security model
    /// WHY IMPORTANT: Prevents token misuse across clients
    /// ARCHITECTURAL SIGNIFICANCE: Validates client binding mechanism
    /// FUTURE RESILIENCE: Maintains client isolation
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_With_Mismatched_ClientId_Should_Return_Null()
    {
        await EnsureInitializedAsync();
        
        // Arrange - First authenticate with one client ID
        var authRequest = CreateTestAuthRequest();
        authRequest = authRequest with { ClientId = "client-1" };
        var authResponse = await AuthenticationService.AuthenticateAsync(authRequest);
        authResponse.ShouldNotBeNull();
        
        TrackEntity(authResponse.UserId);
        
        // Try to refresh with different client ID
        var refreshRequest = CreateTestRefreshTokenRequest(authResponse.RefreshToken, "client-2");
        
        // Act
        var refreshResponse = await AuthenticationService.RefreshTokenAsync(refreshRequest);
        
        // Assert
        refreshResponse.ShouldBeNull();
    }
    
    /// <summary>
    /// INTENT: Verify token validation provides correct information
    /// PURPOSE: Enable token introspection for authorization
    /// BUSINESS CONTEXT: Services need to validate tokens
    /// WHY IMPORTANT: Enables distributed authorization
    /// ARCHITECTURAL SIGNIFICANCE: Validates token metadata retrieval
    /// FUTURE RESILIENCE: Supports future authorization needs
    /// </summary>
    [Fact]
    public async Task ValidateTokenAsync_With_Valid_Token_Should_Return_Token_Info()
    {
        await EnsureInitializedAsync();
        
        // Arrange - First authenticate to create a token
        var authRequest = CreateTestAuthRequest();
        var authResponse = await AuthenticationService.AuthenticateAsync(authRequest);
        authResponse.ShouldNotBeNull();
        
        TrackEntity(authResponse.UserId);
        
        // Act
        var validationResponse = await AuthenticationService.ValidateTokenAsync(authResponse.OwnerEntityId);
        
        // Assert
        validationResponse.ShouldNotBeNull();
        validationResponse.IsValid.ShouldBeTrue();
        validationResponse.UserId.ShouldBe(authResponse.UserId);
        validationResponse.ExpiresAt.ShouldNotBeNull();
        validationResponse.Claims.ShouldNotBeNull();
        validationResponse.Claims.ShouldContainKey("SessionId");
        validationResponse.Claims["SessionId"].ShouldBe(authResponse.SessionId.ToString());
    }
    
    /// <summary>
    /// INTENT: Verify non-existent tokens are reported as invalid
    /// PURPOSE: Ensure proper handling of missing tokens
    /// BUSINESS CONTEXT: Graceful handling of invalid requests
    /// WHY IMPORTANT: Prevents errors from missing data
    /// ARCHITECTURAL SIGNIFICANCE: Validates error handling
    /// FUTURE RESILIENCE: Maintains stability
    /// </summary>
    [Fact]
    public async Task ValidateTokenAsync_With_NonExistent_Token_Should_Return_Invalid()
    {
        await EnsureInitializedAsync();
        
        // Arrange
        var randomTokenId = Guid.NewGuid();
        
        // Act
        var validationResponse = await AuthenticationService.ValidateTokenAsync(randomTokenId);
        
        // Assert
        validationResponse.ShouldNotBeNull();
        validationResponse.IsValid.ShouldBeFalse();
        validationResponse.ErrorMessage.ShouldNotBeNullOrEmpty();
    }
}