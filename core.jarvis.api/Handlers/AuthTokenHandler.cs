using System.Linq;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for authentication token operations.
/// </summary>
public class AuthTokenHandler : ComponentHandler<AuthToken>
{
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthTokenHandler(
        IDataContext dataContext,
        ILogger<AuthTokenHandler> logger,
        ITokenService tokenService,
        IConfiguration configuration)
        : base(dataContext, logger)
    {
        _tokenService = tokenService;
        _configuration = configuration;
    }

    /// <summary>
    /// Deauthenticates (revokes) the AuthToken bound to this handler.
    /// Finds and revokes all tokens associated with the same SessionId to ensure complete logout.
    /// </summary>
    /// <returns>
    /// The deauthenticated AuthToken with cleared access and refresh tokens and IsRevoked set to true.
    /// Returns an empty AuthToken if no token is found for this handler.
    /// </returns>
    /// <remarks>
    /// This method performs a complete logout by:
    /// - Finding all tokens with the same SessionId
    /// - Marking them as revoked with a timestamp
    /// - Clearing sensitive token data from the returned object
    /// </remarks>
    public async Task<AuthToken> Deauthenticate()
    {
        var authToken = await GetOrDefault();
        if (authToken == null)
        {
            Logger.LogWarning("No AuthToken found for deauthentication");
            return new AuthToken();
        }

        if (authToken.SessionId == Guid.Empty)
        {
            Logger.LogWarning("AuthToken has no SessionId for deauthentication");
            return authToken with { IsRevoked = true };
        }

        // Find and revoke the token by SessionId
        var tokenEntities = await DataContext.Query()
            .WithAll<AuthToken>(Filter<AuthToken>.All())
            .ToEntityComponents();

        // Filter in memory to avoid SQL translation issues
        var activeTokens = tokenEntities
            .Where(kvp =>
            {
                var token = kvp.Value.Get<AuthToken>();
                return token != null && token.SessionId == authToken.SessionId && !token.IsRevoked;
            });

        foreach (var kvp in activeTokens)
        {
            var token = kvp.Value.Get<AuthToken>();
            if (token != null)
            {
                var revokedToken = token with
                {
                    IsRevoked = true,
                    RevokedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                await DataContext.TryCommit(revokedToken);
                Logger.LogInformation("Revoked token for session: {SessionId}", token.SessionId);
            }
        }

        return authToken with
        {
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Refreshes the authentication token using token rotation for enhanced security.
    /// Validates the current refresh token, revokes it, and generates new access and refresh tokens.
    /// </summary>
    /// <returns>
    /// A new AuthToken with fresh access and refresh tokens if the current refresh token is valid.
    /// Returns an empty AuthToken if the refresh token is invalid, expired, or not found.
    /// </returns>
    /// <remarks>
    /// This method implements token rotation by:
    /// - Verifying the current refresh token against stored hashes
    /// - Revoking the old token to prevent reuse
    /// - Generating new access and refresh tokens
    /// - Creating a new session ID for the rotated token
    /// - Updating expiration times based on configuration
    /// </remarks>
    public async Task<AuthToken> RefreshToken()
    {
        var authToken = await GetOrDefault();
        if (authToken == null || string.IsNullOrEmpty(authToken.RefreshToken))
        {
            Logger.LogWarning("No valid AuthToken found for refresh");
            return new AuthToken();
        }

        try
        {
            var tokenService = _tokenService;

            // Find active token by refresh token
            var tokenEntities = await DataContext.Query()
                .WithAll<AuthToken>(Filter<AuthToken>.All())
                .ToEntityComponents();

            // Filter in memory for active tokens
            var activeTokens = tokenEntities
                .Where(kvp =>
                {
                    var token = kvp.Value.Get<AuthToken>();
                    return token != null && !token.IsRevoked && token.RefreshExpiresAt > DateTime.UtcNow;
                });

            AuthToken? matchingToken = null;
            foreach (var kvp in activeTokens)
            {
                var token = kvp.Value.Get<AuthToken>();
                if (token != null && tokenService.VerifyRefreshToken(authToken.RefreshToken, token.RefreshTokenHash))
                {
                    matchingToken = token;
                    break;
                }
            }

            if (matchingToken == null)
            {
                Logger.LogWarning("No matching token found for refresh");
                return new AuthToken();
            }

            // Generate new tokens
            var newAccessToken = tokenService.AccessToken(matchingToken.OwnerEntityId, string.Empty);
            var newRefreshToken = tokenService.RefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(15);
            var configuration = _configuration;
            var refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");

            // Revoke the old token (token rotation)
            var revokedToken = matchingToken with
            {
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            await DataContext.TryCommit(revokedToken);

            // Create new token entity (rotation creates new session)
            var newToken = new AuthToken
            {
                Id = Guid.NewGuid(),
                OwnerEntityId = matchingToken.OwnerEntityId,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                RefreshTokenHash = tokenService.HashRefreshToken(newRefreshToken),
                ExpiresAt = expiresAt,
                RefreshExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
                SessionId = Guid.NewGuid(), // New session ID for rotated token
                ClientId = matchingToken.ClientId,
                IsRevoked = false,
                IssuedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            await DataContext.TryCommit(newToken);
            Logger.LogInformation("Token rotated: old session {OldSession} revoked, new session {NewSession} created",
                matchingToken.SessionId, newToken.SessionId);

            return newToken;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during token refresh");
            return new AuthToken();
        }
    }

    /// <summary>
    /// Validates the authentication token bound to this handler.
    /// Performs JWT validation and extracts claims information.
    /// </summary>
    /// <returns>
    /// A TokenValidation object containing validation results, expiration time, and claims if valid.
    /// Returns validation failure information if the token is invalid or expired.
    /// </returns>
    /// <remarks>
    /// This method validates:
    /// - Token presence and format
    /// - JWT signature and structure
    /// - Token expiration
    /// - Claims extraction for valid tokens
    /// </remarks>
    public async Task<TokenValidation> ValidateToken()
    {
        var authToken = await GetOrDefault();
        if (authToken == null || string.IsNullOrEmpty(authToken.AccessToken))
        {
            return new TokenValidation
            {
                OwnerEntityId = OwnerEntityId,
                IsValid = false,
                ErrorMessage = "No token found"
            };
        }

        try
        {
            var tokenService = _tokenService;
            var principal = tokenService.Validate(authToken.AccessToken);

            if (principal == null)
            {
                return new TokenValidation
                {
                    OwnerEntityId = OwnerEntityId,
                    IsValid = false,
                    ErrorMessage = "Invalid or expired token"
                };
            }

            return new TokenValidation
            {
                OwnerEntityId = OwnerEntityId,
                IsValid = true,
                ExpiresAt = authToken.ExpiresAt,
                Claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value)
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during token validation");
            return new TokenValidation
            {
                OwnerEntityId = OwnerEntityId,
                IsValid = false,
                ErrorMessage = "Validation error occurred"
            };
        }
    }

    /// <summary>
    /// Cleans up expired tokens for the current user.
    /// Called automatically during authentication to prevent token accumulation.
    /// </summary>
    /// <returns>The number of expired tokens that were cleaned up and removed from the database</returns>
    /// <remarks>
    /// This method removes tokens that are either:
    /// - Past their refresh expiration date
    /// - Already marked as revoked
    /// This helps maintain database hygiene and prevents accumulation of stale authentication data.
    /// </remarks>
    public async Task<int> CleanupExpiredTokens()
    {
        try
        {
            var allTokens = await DataContext.Query()
                .WithAll<AuthToken>(Filter<AuthToken>.All())
                .ToEntityComponents();

            // Filter in memory for expired/revoked tokens for this owner
            var expiredTokens = allTokens
                .Where(kvp =>
                {
                    var token = kvp.Value.Get<AuthToken>();
                    return token != null && token.OwnerEntityId == OwnerEntityId &&
                        (token.RefreshExpiresAt < DateTime.UtcNow || token.IsRevoked);
                });

            int cleanedCount = 0;
            foreach (var kvp in expiredTokens)
            {
                await DataContext.Remove<AuthToken>(kvp.Key);
                cleanedCount++;
            }

            if (cleanedCount > 0)
            {
                Logger.LogInformation("Cleaned up {Count} expired tokens for entity {EntityId}", cleanedCount, OwnerEntityId);
            }

            return cleanedCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cleaning up expired tokens for entity {EntityId}", OwnerEntityId);
            return 0;
        }
    }

    /// <summary>
    /// Enforces maximum active sessions per user.
    /// Revokes oldest sessions if limit is exceeded.
    /// </summary>
    /// <param name="maxSessions">The maximum number of active sessions allowed per user (default: 5)</param>
    /// <remarks>
    /// This security feature prevents session proliferation by:
    /// - Counting active (non-revoked, non-expired) sessions for the user
    /// - Revoking the oldest sessions if the limit is exceeded
    /// - Making room for new sessions during authentication
    /// Sessions are ordered by IssuedAt timestamp, with oldest sessions revoked first.
    /// </remarks>
    public async Task EnforceSessionLimit(int maxSessions = 5)
    {
        try
        {
            var allSessions = await DataContext.Query()
                .WithAll<AuthToken>(Filter<AuthToken>.All())
                .ToEntityComponents();

            // Filter in memory for active sessions for this owner
            var activeSessions = allSessions
                .Where(kvp =>
                {
                    var token = kvp.Value.Get<AuthToken>();
                    return token != null && token.OwnerEntityId == OwnerEntityId &&
                        !token.IsRevoked && token.RefreshExpiresAt > DateTime.UtcNow;
                })
                .ToList();

            if (activeSessions.Count() <= maxSessions)
            {
                return;
            }

            // Order by IssuedAt and revoke oldest sessions
            var sessionsToRevoke = activeSessions
                .Select(kvp => kvp.Value.Get<AuthToken>())
                .Where(t => t != null)
                .OrderBy(t => t!.IssuedAt)
                .Take(activeSessions.Count - maxSessions + 1) // +1 to make room for new session
                .ToList();

            foreach (var token in sessionsToRevoke)
            {
                if (token != null)
                {
                    var revokedToken = token with
                    {
                        IsRevoked = true,
                        RevokedAt = DateTime.UtcNow,
                        LastUpdated = DateTime.UtcNow
                    };
                    await DataContext.TryCommit(revokedToken);
                    Logger.LogInformation("Revoked old session {SessionId} to enforce session limit", token.SessionId);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error enforcing session limit for entity {EntityId}", OwnerEntityId);
        }
    }
}
