using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data.Query;
using core.jarvis.tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace core.jarvis.tests.Handlers;

/// <summary>
/// Integration tests for <see cref="SecurityTokenHandler"/>.
/// Validates session creation, token revocation, and security token lifecycle management.
/// </summary>
public class SecurityTokenHandlerTests : IntegrationTestBase
{
    private ITokenService _tokenService = null!;

    /// <summary>
    /// Initializes the test environment with required services for SecurityTokenHandler testing.
    /// Retrieves ITokenService from the service provider configured in IntegrationTestBase.
    /// </summary>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _tokenService = _serviceProvider.GetRequiredService<ITokenService>();
    }

    /// <summary>
    /// INTENT: Verify that CreateSession creates a valid AuthToken with correct session information.
    /// PURPOSE: Ensures session creation stores refresh token hash securely and sets proper expiration.
    /// BUSINESS CONTEXT: Session creation is the foundation of authentication flow.
    /// WHY IMPORTANT: Validates that users can establish authenticated sessions with secure tokens.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the primary session establishment mechanism.
    /// FUTURE RESILIENCE: Ensures session creation remains consistent as authentication evolves.
    /// </summary>
    [Fact]
    public async Task CreateSession_WithValidParameters_ShouldCreateAuthTokenWithHashedRefreshToken()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var refreshToken = _tokenService.RefreshToken();
        var clientId = "test-client-app";
        TrackEntity(entityId);

        var logger = _serviceProvider.GetRequiredService<ILogger<SecurityTokenHandler>>();
        var handler = new SecurityTokenHandler(TestDataContext(), logger, _serviceProvider);
        handler.InitializeContext(entityId);

        // Act
        var authToken = await handler.CreateSession(entityId, sessionId, refreshToken, clientId);

        // Assert
        authToken.ShouldNotBeNull("CreateSession should return a valid AuthToken");
        authToken.OwnerEntityId.ShouldBe(entityId, "AuthToken should be associated with the correct entity");
        authToken.SessionId.ShouldBe(sessionId, "SessionId should match the provided value");
        authToken.RefreshToken.ShouldBe(refreshToken, "RefreshToken should be stored in the component");
        authToken.RefreshTokenHash.ShouldNotBeNullOrEmpty("RefreshTokenHash should be computed and stored");
        authToken.ClientId.ShouldBe(clientId, "ClientId should be stored in the component");
        authToken.IsRevoked.ShouldBeFalse("New session should not be revoked");
        authToken.RefreshExpiresAt.ShouldBeGreaterThan(DateTime.UtcNow, "RefreshExpiresAt should be in the future");

        // Verify the hash is correct
        var isValidHash = _tokenService.VerifyRefreshToken(refreshToken, authToken.RefreshTokenHash);
        isValidHash.ShouldBeTrue("Stored hash should verify against the original refresh token");

        // Verify persistence by querying the database
        var result = await TestDataContext().Query()
            .WithAll<AuthToken>(Filter<AuthToken>.Eq(t => t.SessionId, sessionId))
            .ToEntityComponents();

        result.ShouldNotBeEmpty("AuthToken should be persisted in the database");
        var persistedToken = result.First().Value.Get<AuthToken>();
        persistedToken.ShouldNotBeNull("Persisted AuthToken should be retrievable");
        persistedToken.SessionId.ShouldBe(sessionId, "Persisted SessionId should match");
    }

    /// <summary>
    /// INTENT: Verify that Revoke marks an active token as revoked with a timestamp.
    /// PURPOSE: Ensures token revocation prevents further use of compromised or expired sessions.
    /// BUSINESS CONTEXT: Token revocation is critical for logout and security incident response.
    /// WHY IMPORTANT: Validates that revoked tokens cannot be used for authentication.
    /// ARCHITECTURAL SIGNIFICANCE: Tests the security mechanism for invalidating sessions.
    /// FUTURE RESILIENCE: Ensures revocation behavior remains consistent with security requirements.
    /// </summary>
    [Fact]
    public async Task Revoke_WithActiveToken_ShouldMarkTokenAsRevokedWithTimestamp()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var refreshToken = _tokenService.RefreshToken();
        TrackEntity(entityId);

        var logger = _serviceProvider.GetRequiredService<ILogger<SecurityTokenHandler>>();
        var handler = new SecurityTokenHandler(TestDataContext(), logger, _serviceProvider);
        handler.InitializeContext(entityId);

        // Create a session first
        var createdToken = await handler.CreateSession(entityId, sessionId, refreshToken);
        createdToken.IsRevoked.ShouldBeFalse("Token should not be revoked after creation");

        var beforeRevoke = DateTime.UtcNow;

        // Act
        var revokedToken = await handler.Revoke();

        // Assert
        revokedToken.ShouldNotBeNull("Revoke should return the updated AuthToken");
        revokedToken.IsRevoked.ShouldBeTrue("Token should be marked as revoked");
        revokedToken.RevokedAt.ShouldNotBeNull("RevokedAt timestamp should be set");
        revokedToken.RevokedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeRevoke, "RevokedAt should be after the revoke call");
        revokedToken.LastUpdated.ShouldBeGreaterThanOrEqualTo(beforeRevoke, "LastUpdated should be updated");

        // Verify persistence
        var result = await TestDataContext().Query()
            .WithAll<AuthToken>(Filter<AuthToken>.Eq(t => t.SessionId, sessionId))
            .ToEntityComponents();

        result.ShouldNotBeEmpty("AuthToken should still exist in the database");
        var persistedToken = result.First().Value.Get<AuthToken>();
        persistedToken.ShouldNotBeNull("Persisted AuthToken should be retrievable");
        persistedToken.IsRevoked.ShouldBeTrue("Persisted token should be marked as revoked");
    }

    /// <summary>
    /// INTENT: Verify that revoking an already-revoked token is idempotent and does not fail.
    /// PURPOSE: Ensures multiple revocation attempts do not cause errors or data corruption.
    /// BUSINESS CONTEXT: Idempotent revocation prevents race conditions during concurrent logout.
    /// WHY IMPORTANT: Validates graceful handling of duplicate revocation requests.
    /// ARCHITECTURAL SIGNIFICANCE: Tests defensive programming in security-critical operations.
    /// FUTURE RESILIENCE: Ensures robust behavior under concurrent access patterns.
    /// </summary>
    [Fact]
    public async Task Revoke_WhenAlreadyRevoked_ShouldReturnTokenWithoutError()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var refreshToken = _tokenService.RefreshToken();
        TrackEntity(entityId);

        var logger = _serviceProvider.GetRequiredService<ILogger<SecurityTokenHandler>>();
        var handler = new SecurityTokenHandler(TestDataContext(), logger, _serviceProvider);
        handler.InitializeContext(entityId);

        // Create and revoke a session
        await handler.CreateSession(entityId, sessionId, refreshToken);
        var firstRevoke = await handler.Revoke();
        var firstRevokedAt = firstRevoke.RevokedAt;

        // Act - Attempt to revoke again
        var secondRevoke = await handler.Revoke();

        // Assert
        secondRevoke.ShouldNotBeNull("Second revoke should return a token");
        secondRevoke.IsRevoked.ShouldBeTrue("Token should remain revoked");

        // Compare RevokedAt within 1 second tolerance to account for database datetime precision
        var timeDifference = Math.Abs((secondRevoke.RevokedAt!.Value - firstRevokedAt!.Value).TotalSeconds);
        timeDifference.ShouldBeLessThan(1, "RevokedAt should not change significantly on subsequent revocations");

        // Verify only one token exists and it's revoked
        var result = await TestDataContext().Query()
            .WithAll<AuthToken>(Filter<AuthToken>.Eq(t => t.SessionId, sessionId))
            .ToEntityComponents();

        result.Count.ShouldBe(1, "Only one token should exist for the session");
        var persistedToken = result.First().Value.Get<AuthToken>();
        persistedToken!.IsRevoked.ShouldBeTrue("Token should remain revoked in database");
    }
}
