using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing authentication tokens in the database.
/// </summary>
public class SecurityTokenHandler : ComponentHandler<AuthToken>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly int _refreshTokenExpirationDays;

    public SecurityTokenHandler(
        IDataContext dataContext,
        ILogger<SecurityTokenHandler> logger,
        IServiceProvider serviceProvider)
        : base(dataContext, logger)
    {
        _serviceProvider = serviceProvider;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");
    }

    /// <summary>
    /// Creates a new session token for the specified user.
    /// </summary>
    public async Task<AuthToken> CreateSession(Guid authenticatedEntityId, Guid sessionId, string refreshToken, string? clientId = null)
    {
        var tokenService = _serviceProvider.GetRequiredService<ITokenService>();

        var authToken = new AuthToken
        {
            OwnerEntityId = authenticatedEntityId,
            SessionId = sessionId,
            RefreshToken = refreshToken,
            RefreshTokenHash = tokenService.HashRefreshToken(refreshToken),
            RefreshExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            ClientId = clientId,
            IpAddress = null, // Could be added later if needed
            UserAgent = null, // Could be added later if needed
            IsRevoked = false,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(authToken);
        Logger.LogInformation("Created session {SessionId} for entity {EntityId}", sessionId, authenticatedEntityId);
        return authToken;
    }

    /// <summary>
    /// Revokes this security token.
    /// </summary>
    public async Task<AuthToken> Revoke()
    {
        var token = await GetOrDefault() ?? throw new InvalidOperationException("AuthToken component not found");
        
        if (token.IsRevoked)
        {
            Logger.LogWarning("Token {TokenId} is already revoked", OwnerEntityId);
            return token;
        }

        var revoked = token with 
        { 
            IsRevoked = true, 
            RevokedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        await DataContext.Commit(revoked);
        Logger.LogInformation("Token {TokenId} has been revoked", OwnerEntityId);
        return revoked;
    }
}