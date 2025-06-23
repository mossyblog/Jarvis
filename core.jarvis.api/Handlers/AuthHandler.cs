using System.Linq;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for authentication operations on User components.
/// </summary>
public class AuthHandler : ComponentHandler<User>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly int _refreshTokenExpirationDays;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly ISecurityAuditService _securityAudit;

    public AuthHandler(
        IDataContext dataContext,
        ILogger<AuthHandler> logger,
        IServiceProvider serviceProvider)
        : base(dataContext, logger)
    {
        _serviceProvider = serviceProvider;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");
        _passwordPolicy = serviceProvider.GetRequiredService<IPasswordPolicyService>();
        _securityAudit = serviceProvider.GetRequiredService<ISecurityAuditService>();
    }

    /// <summary>
    /// Authenticates user credentials and generates tokens.
    /// Takes User (credentials), validates against existing data, and returns AuthToken with session data.
    /// Does NOT persist anything - authentication is read-only validation.
    /// </summary>
    public async Task<AuthToken> Authenticate(User userCredentials)
    {
        try
        {
            // Get services
            var tokenService = _serviceProvider.GetRequiredService<ITokenService>();
            var pgClient = _serviceProvider.GetRequiredService<PgClient>();
            
            // Validate credentials using PgClient
            var authResult = await pgClient.Authenticate(userCredentials.Email, userCredentials.Password);
            Logger.LogInformation("PgClient.Authenticate returned: {Result} for email: {Email}", authResult, userCredentials.Email);
            if (string.IsNullOrEmpty(authResult))
            {
                Logger.LogWarning("Authentication failed for email: {Email}", userCredentials.Email);
                
                // Log failed authentication attempt
                await _securityAudit.LogFailedAuthentication(
                    userCredentials.Email, 
                    userCredentials.IpAddress ?? "unknown",
                    userCredentials.UserAgent,
                    "Invalid credentials"
                );
                
                return new AuthToken(); // Return empty token to indicate failure
            }

            // Extract entity ID from auth result (format: "auth.success.{entityId}")
            Guid authenticatedEntityId;
            if (authResult.StartsWith("auth.success."))
            {
                var entityIdStr = authResult.Substring("auth.success.".Length);
                if (!Guid.TryParse(entityIdStr, out authenticatedEntityId))
                {
                    Logger.LogError("Failed to parse entity ID from auth result: {Result}", authResult);
                    return new AuthToken();
                }
            }
            else
            {
                Logger.LogError("Unexpected auth result format: {Result}", authResult);
                return new AuthToken();
            }

            // Generate tokens
            var accessToken = tokenService.GenerateAccessToken(authenticatedEntityId, userCredentials.Email);
            var refreshToken = tokenService.GenerateRefreshToken();
            var sessionId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddMinutes(15);

            // Get existing security profile for role claims
            var profileHandler = DataContext.For<UserProfileHandler>(authenticatedEntityId);
            var securityProfile = await profileHandler.Get();
            
            // Generate access token with role claims if profile exists
            var additionalClaims = new Dictionary<string, string>();
            if (securityProfile?.RoleIds.Any() == true)
            {
                additionalClaims["roles"] = string.Join(",", securityProfile.RoleIds);
            }

            var finalAccessToken = tokenService.GenerateAccessToken(authenticatedEntityId, userCredentials.Email, additionalClaims);

            // Create AuthToken result - OwnerEntityId is the authenticated user's entity ID
            var authToken = new AuthToken
            {
                OwnerEntityId = authenticatedEntityId,
                AccessToken = finalAccessToken,
                RefreshToken = refreshToken,
                RefreshTokenHash = tokenService.HashRefreshToken(refreshToken),
                ExpiresAt = expiresAt,
                RefreshExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                SessionId = sessionId,
                ClientId = userCredentials.ClientId,
                UpdatedAt = DateTime.UtcNow
            };

            // Log successful authentication
            await _securityAudit.LogSuccessfulAuthentication(
                authenticatedEntityId,
                userCredentials.Email,
                userCredentials.IpAddress ?? "unknown",
                userCredentials.UserAgent
            );

            return authToken;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during authentication for email: {Email}", userCredentials.Email);
            return new AuthToken();
        }
    }

    /// <summary>
    /// Checks if the provided AuthToken component represents successful authentication.
    /// </summary>
    public bool IsAuthenticated(AuthToken authToken)
    {
        return authToken != null && !string.IsNullOrEmpty(authToken.AccessToken) && authToken.OwnerEntityId != Guid.Empty;
    }

    /// <summary>
    /// Persists the authenticated session token to the database.
    /// Only call this after successful authentication.
    /// </summary>
    public async Task<bool> PersistSession(AuthToken authToken)
    {
        if (!IsAuthenticated(authToken))
        {
            Logger.LogWarning("Cannot persist invalid authentication token");
            return false;
        }

        try
        {
            // Clean up expired tokens and enforce session limit for this user
            var tokenHandler = DataContext.For<AuthTokenHandler>(authToken.OwnerEntityId);
            await tokenHandler.CleanupExpiredTokens();
            await tokenHandler.EnforceSessionLimit(5); // Max 5 active sessions per user

            // Create a new entity for this session
            var sessionEntity = new AuthToken
            {
                OwnerEntityId = authToken.OwnerEntityId,
                AccessToken = string.Empty, // Don't store access token
                RefreshToken = string.Empty, // Don't store plain refresh token
                RefreshTokenHash = authToken.RefreshTokenHash,
                ExpiresAt = authToken.ExpiresAt,
                RefreshExpiresAt = authToken.RefreshExpiresAt,
                SessionId = authToken.SessionId,
                ClientId = authToken.ClientId,
                IsRevoked = false,
                IssuedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await DataContext.Commit(sessionEntity);
            Logger.LogInformation("Persisted session {SessionId} for entity {EntityId}", authToken.SessionId, authToken.OwnerEntityId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting session for entity {EntityId}", authToken.OwnerEntityId);
            return false;
        }
    }
}