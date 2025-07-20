using System.Linq;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for authentication token operations.
/// </summary>
public class AuthTokenHandler : ComponentHandler<AuthToken>
{
    private readonly IServiceProvider _serviceProvider;

    public AuthTokenHandler(
        IDataContext dataContext,
        ILogger<AuthTokenHandler> logger,
        IServiceProvider serviceProvider)
        : base(dataContext, logger)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Deauthenticates (revokes) the AuthToken bound to this handler.
    /// </summary>
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
            .WithAll<AuthToken>(t => t.SessionId == authToken.SessionId && !t.IsRevoked)
            .ToEntityComponents();

        foreach (var kvp in tokenEntities)
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
                await DataContext.Commit(revokedToken);
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
    /// Refreshes the authentication token.
    /// </summary>
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
            var tokenService = _serviceProvider.GetRequiredService<ITokenService>();

            // Find active token by refresh token
            var tokenEntities = await DataContext.Query()
                .WithAll<AuthToken>(t => !t.IsRevoked && t.RefreshExpiresAt > DateTime.UtcNow)
                .ToEntityComponents();

            AuthToken? matchingToken = null;
            foreach (var kvp in tokenEntities)
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
            var newAccessToken = tokenService.GenerateAccessToken(matchingToken.OwnerEntityId, string.Empty);
            var newRefreshToken = tokenService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(15);
            var configuration = _serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");

            // Revoke the old token (token rotation)
            var revokedToken = matchingToken with
            {
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            await DataContext.Commit(revokedToken);

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

            await DataContext.Commit(newToken);
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
    /// Validates the authentication token.
    /// </summary>
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
            var tokenService = _serviceProvider.GetRequiredService<ITokenService>();
            var principal = tokenService.ValidateToken(authToken.AccessToken);
            
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
    public async Task<int> CleanupExpiredTokens()
    {
        try
        {
            var expiredTokens = await DataContext.Query()
                .WithAll<AuthToken>(t => t.OwnerEntityId == OwnerEntityId && 
                    (t.RefreshExpiresAt < DateTime.UtcNow || t.IsRevoked))
                .ToEntityComponents();

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
    public async Task EnforceSessionLimit(int maxSessions = 5)
    {
        try
        {
            var activeSessions = await DataContext.Query()
                .WithAll<AuthToken>(t => t.OwnerEntityId == OwnerEntityId && 
                    !t.IsRevoked && t.RefreshExpiresAt > DateTime.UtcNow)
                .ToEntityComponents();

            if (activeSessions.Count <= maxSessions)
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
                    await DataContext.Commit(revokedToken);
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