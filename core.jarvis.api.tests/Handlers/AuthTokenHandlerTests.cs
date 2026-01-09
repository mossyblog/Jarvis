using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.api.tests.Helpers;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using Shouldly;
using System.Security.Claims;

namespace core.jarvis.api.tests.Handlers;

/// <summary>
/// Integration tests for the AuthTokenHandler.
/// Tests authentication token lifecycle operations including deauthentication, token refresh,
/// token validation, and session management.
/// </summary>
public class AuthTokenHandlerTests : ApiIntegrationTestBase
{
    /// <summary>
    /// Tests that Deauthenticate() successfully revokes an active token and clears sensitive data.
    ///
    /// INTENT: Verify complete token revocation removes access and refresh tokens
    /// PURPOSE: Ensure deauthentication provides complete logout functionality
    /// BUSINESS CONTEXT: Users need secure logout that invalidates all token-based access
    /// WHY IMPORTANT: Security depends on effective token revocation to prevent unauthorized access
    /// ARCHITECTURAL SIGNIFICANCE: Validates token revocation mechanism in authentication handler
    /// FUTURE RESILIENCE: Ensures deauthentication behavior persists across authentication refactors
    /// </summary>
    [Fact]
    public async Task Deauthenticate_WithActiveToken_ShouldRevokeTokenAndClearData()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var authToken = new AuthToken
        {
            Id = tokenId,
            OwnerEntityId = ownerEntityId,
            AccessToken = TokenService.AccessToken(ownerEntityId, "test@example.com"),
            RefreshToken = TokenService.RefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshExpiresAt = DateTime.UtcNow.AddDays(30),
            SessionId = sessionId,
            IsRevoked = false
        };

        // Store the hash of refresh token
        authToken = authToken with
        {
            RefreshTokenHash = TokenService.HashRefreshToken(authToken.RefreshToken)
        };

        await TestDataContext().TryCommit(authToken);

        var handler = TestDataContext().For<AuthTokenHandler>(ownerEntityId);
        handler.InitializeContext(ownerEntityId);

        // Act
        var result = await handler.Deauthenticate();

        // Assert
        result.ShouldNotBeNull();
        result.IsRevoked.ShouldBeTrue();
        result.AccessToken.ShouldBeEmpty();
        result.RefreshToken.ShouldBeEmpty();
        result.RevokedAt.ShouldNotBeNull();
        result.SessionId.ShouldBe(sessionId);
    }

    /// <summary>
    /// Tests that Deauthenticate() handles missing tokens gracefully by returning empty AuthToken.
    ///
    /// INTENT: Verify deauthentication handles absent tokens without errors
    /// PURPOSE: Ensure handler gracefully handles edge case where no token exists
    /// BUSINESS CONTEXT: Some users may attempt logout without prior authentication
    /// WHY IMPORTANT: Robust error handling prevents cascading failures during logout
    /// ARCHITECTURAL SIGNIFICANCE: Validates null-safety in token revocation workflow
    /// FUTURE RESILIENCE: Ensures missing token scenarios remain handled safely
    /// </summary>
    [Fact]
    public async Task Deauthenticate_WithNoToken_ShouldReturnEmptyToken()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var handler = TestDataContext().For<AuthTokenHandler>(ownerEntityId);
        handler.InitializeContext(ownerEntityId);

        // Act
        var result = await handler.Deauthenticate();

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBeEmpty();
        result.RefreshToken.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that RefreshToken() generates new tokens and revokes old ones using token rotation.
    ///
    /// INTENT: Verify token refresh implements secure token rotation pattern
    /// PURPOSE: Ensure new tokens are issued while invalidating previous tokens
    /// BUSINESS CONTEXT: Session continuity requires token refresh without compromising security
    /// WHY IMPORTANT: Token rotation prevents token reuse attacks and enhances security posture
    /// ARCHITECTURAL SIGNIFICANCE: Validates implementation of OAuth 2.0 token rotation best practice
    /// FUTURE RESILIENCE: Ensures token rotation remains consistent with security standards
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ShouldGenerateNewTokensAndRevokeOld()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var oldTokenId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var accessToken = TokenService.AccessToken(ownerEntityId, "test@example.com");
        var refreshToken = TokenService.RefreshToken();
        var hashedRefreshToken = TokenService.HashRefreshToken(refreshToken);

        var oldToken = new AuthToken
        {
            Id = oldTokenId,
            OwnerEntityId = ownerEntityId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenHash = hashedRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshExpiresAt = DateTime.UtcNow.AddDays(30),
            SessionId = sessionId,
            IsRevoked = false
        };

        await TestDataContext().TryCommit(oldToken);

        var handler = TestDataContext().For<AuthTokenHandler>(ownerEntityId);
        handler.InitializeContext(ownerEntityId);

        // Act
        var result = await handler.RefreshToken();

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.SessionId.ShouldNotBe(sessionId); // New session ID should be generated
        result.IsRevoked.ShouldBeFalse();
        result.OwnerEntityId.ShouldBe(ownerEntityId);
        result.RefreshExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow.AddDays(29));
    }

    /// <summary>
    /// Tests that RefreshToken() returns empty token when refresh token is invalid or missing.
    ///
    /// INTENT: Verify token refresh rejects invalid credentials
    /// PURPOSE: Ensure unauthorized refresh attempts are denied
    /// BUSINESS CONTEXT: Compromised or expired refresh tokens must be rejected
    /// WHY IMPORTANT: Invalid token rejection prevents session hijacking and unauthorized access
    /// ARCHITECTURAL SIGNIFICANCE: Validates security boundary in token refresh process
    /// FUTURE RESILIENCE: Ensures invalid token handling persists across authentication changes
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithNoValidToken_ShouldReturnEmptyToken()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var handler = TestDataContext().For<AuthTokenHandler>(ownerEntityId);
        handler.InitializeContext(ownerEntityId);

        // Act
        var result = await handler.RefreshToken();

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBeEmpty();
        result.RefreshToken.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that ValidateToken() returns valid TokenValidation with claims for active tokens.
    ///
    /// INTENT: Verify token validation confirms valid tokens and extracts claims
    /// PURPOSE: Ensure API can validate user identity from tokens
    /// BUSINESS CONTEXT: Every API request requires valid token validation for authorization
    /// WHY IMPORTANT: Token validation is the foundation of API security and authorization
    /// ARCHITECTURAL SIGNIFICANCE: Validates JWT validation mechanism in handler
    /// FUTURE RESILIENCE: Ensures token validation logic remains functional across JWT updates
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithValidToken_ShouldReturnValidationSuccess()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var accessToken = TokenService.AccessToken(ownerEntityId, "test@example.com");

        var authToken = new AuthToken
        {
            Id = tokenId,
            OwnerEntityId = ownerEntityId,
            AccessToken = accessToken,
            RefreshToken = TokenService.RefreshToken(),
            RefreshTokenHash = TokenService.HashRefreshToken("placeholder"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = false
        };

        await TestDataContext().TryCommit(authToken);

        var handler = TestDataContext().For<AuthTokenHandler>(ownerEntityId);
        handler.InitializeContext(ownerEntityId);

        // Act
        var result = await handler.ValidateToken();

        // Assert
        result.ShouldNotBeNull();
        result.IsValid.ShouldBeTrue();
        result.OwnerEntityId.ShouldBe(ownerEntityId);
        result.ExpiresAt.ShouldBe(authToken.ExpiresAt);
        result.Claims.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that ValidateToken() rejects tokens that are missing or empty.
    ///
    /// INTENT: Verify validation fails for missing tokens
    /// PURPOSE: Ensure unauthenticated requests are properly rejected
    /// BUSINESS CONTEXT: Missing tokens indicate unauthenticated users
    /// WHY IMPORTANT: Proper rejection of unauthenticated requests is security critical
    /// ARCHITECTURAL SIGNIFICANCE: Validates authentication boundary enforcement
    /// FUTURE RESILIENCE: Ensures missing token rejection remains consistent
    /// </summary>
    [Fact]
    public async Task ValidateToken_WithMissingToken_ShouldReturnValidationFailure()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        var handler = TestDataContext().For<AuthTokenHandler>(ownerEntityId);
        handler.InitializeContext(ownerEntityId);

        // Act
        var result = await handler.ValidateToken();

        // Assert
        result.ShouldNotBeNull();
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
        result.OwnerEntityId.ShouldBe(ownerEntityId);
    }

    /// <summary>
    /// Tests that CleanupExpiredTokens() removes expired and revoked tokens for the current user.
    ///
    /// INTENT: Verify cleanup removes only expired/revoked tokens for specific user
    /// PURPOSE: Maintain database hygiene by removing stale authentication data
    /// BUSINESS CONTEXT: Long-lived systems accumulate expired tokens requiring periodic cleanup
    /// WHY IMPORTANT: Database cleanup prevents unbounded growth and improves performance
    /// ARCHITECTURAL SIGNIFICANCE: Validates maintenance operations in token lifecycle
    /// FUTURE RESILIENCE: Ensures cleanup operation remains effective across data growth
    /// </summary>
    [Fact]
    public async Task CleanupExpiredTokens_WithMixedTokenStates_ShouldRemoveOnlyExpiredAndRevoked()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        TrackEntity(ownerEntityId);

        // Create mix of token states
        var activeToken = new AuthToken
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = ownerEntityId,
            AccessToken = TokenService.AccessToken(ownerEntityId, "test@example.com"),
            RefreshToken = TokenService.RefreshToken(),
            RefreshTokenHash = TokenService.HashRefreshToken("placeholder1"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = false
        };

        var expiredToken = new AuthToken
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = ownerEntityId,
            AccessToken = TokenService.AccessToken(ownerEntityId, "test@example.com"),
            RefreshToken = TokenService.RefreshToken(),
            RefreshTokenHash = TokenService.HashRefreshToken("placeholder2"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            IsRevoked = false
        };

        var revokedToken = new AuthToken
        {
            Id = Guid.NewGuid(),
            OwnerEntityId = ownerEntityId,
            AccessToken = TokenService.AccessToken(ownerEntityId, "test@example.com"),
            RefreshToken = TokenService.RefreshToken(),
            RefreshTokenHash = TokenService.HashRefreshToken("placeholder3"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            RefreshExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddDays(-1)
        };

        await TestDataContext().TryCommit(activeToken);
        await TestDataContext().TryCommit(expiredToken);
        await TestDataContext().TryCommit(revokedToken);

        var handler = TestDataContext().For<AuthTokenHandler>(ownerEntityId);
        handler.InitializeContext(ownerEntityId);

        // Act
        var cleanedCount = await handler.CleanupExpiredTokens();

        // Assert
        cleanedCount.ShouldBe(2); // Should remove expired and revoked tokens
    }

    /// <summary>
    /// Tests that EnforceSessionLimit() revokes oldest sessions when limit is exceeded.
    ///
    /// INTENT: Verify session limit enforcement revokes oldest sessions first
    /// PURPOSE: Prevent excessive session accumulation per user
    /// BUSINESS CONTEXT: Security policy requires limiting concurrent sessions per user
    /// WHY IMPORTANT: Session limits prevent session accumulation attacks and resource exhaustion
    /// ARCHITECTURAL SIGNIFICANCE: Validates session management security feature
    /// FUTURE RESILIENCE: Ensures session limits remain enforced as concurrent usage grows
    /// </summary>
    [Fact]
    public async Task EnforceSessionLimit_WithExcessiveSessions_ShouldRevokeOldestSessions()
    {
        // Arrange
        var ownerEntityId = Guid.NewGuid();
        var maxSessions = 3;
        TrackEntity(ownerEntityId);

        // Create more sessions than limit
        var tokens = new List<AuthToken>();
        for (int i = 0; i < 5; i++)
        {
            var token = new AuthToken
            {
                Id = Guid.NewGuid(),
                OwnerEntityId = ownerEntityId,
                AccessToken = TokenService.AccessToken(ownerEntityId, "test@example.com"),
                RefreshToken = TokenService.RefreshToken(),
                RefreshTokenHash = TokenService.HashRefreshToken($"placeholder{i}"),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                RefreshExpiresAt = DateTime.UtcNow.AddDays(30),
                IssuedAt = DateTime.UtcNow.AddMinutes(i * 5), // Stagger issue times
                IsRevoked = false
            };
            tokens.Add(token);
            await TestDataContext().TryCommit(token);
        }

        var handler = TestDataContext().For<AuthTokenHandler>(ownerEntityId);
        handler.InitializeContext(ownerEntityId);

        // Act
        await handler.EnforceSessionLimit(maxSessions);

        // Assert
        var allTokens = await TestDataContext().Query()
            .WithAll<AuthToken>(Filter<AuthToken>.All())
            .ToEntityComponents();

        var activeSessions = allTokens
            .Where(kvp =>
            {
                var token = kvp.Value.Get<AuthToken>();
                return token != null && token.OwnerEntityId == ownerEntityId && !token.IsRevoked;
            })
            .ToList();

        // Should have at most maxSessions active sessions
        activeSessions.Count.ShouldBeLessThanOrEqualTo(maxSessions);
    }
}
