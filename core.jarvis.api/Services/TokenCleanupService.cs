using System;
using System.Threading;
using System.Threading.Tasks;
using core.jarvis.api.Models;
using core.jarvis.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Services;

/// <summary>
/// Background service that periodically cleans up expired authentication tokens.
/// </summary>
public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run every hour

    public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokens();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Token cleanup service stopped");
    }

    private async Task CleanupExpiredTokens()
    {
        using var scope = _serviceProvider.CreateScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<IDataContext>();

        try
        {
            // Query all expired or revoked tokens
            var expiredTokens = await dataContext.Query()
                .WithAll<AuthToken>(t => t.RefreshExpiresAt < DateTime.UtcNow || t.IsRevoked)
                .ToEntityComponents();

            int cleanedCount = 0;
            foreach (var kvp in expiredTokens)
            {
                var token = kvp.Value.Get<AuthToken>();
                if (token != null)
                {
                    // Only delete tokens that have been expired/revoked for more than 7 days
                    var expirationDate = token.IsRevoked && token.RevokedAt.HasValue 
                        ? token.RevokedAt.Value 
                        : token.RefreshExpiresAt;

                    if (expirationDate < DateTime.UtcNow.AddDays(-7))
                    {
                        await dataContext.Remove<AuthToken>(kvp.Key);
                        cleanedCount++;
                    }
                }
            }

            if (cleanedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired tokens", cleanedCount);
            }

            // Also enforce global session limits for users with too many sessions
            await EnforceGlobalSessionLimits(dataContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch token cleanup");
        }
    }

    private async Task EnforceGlobalSessionLimits(IDataContext dataContext)
    {
        try
        {
            // Find users with more than 10 active sessions
            var allActiveTokens = await dataContext.Query()
                .WithAll<AuthToken>(t => !t.IsRevoked && t.RefreshExpiresAt > DateTime.UtcNow)
                .ToEntityComponents();

            // Group by OwnerEntityId
            var userSessions = allActiveTokens
                .Select(kvp => kvp.Value.Get<AuthToken>())
                .Where(t => t != null)
                .GroupBy(t => t!.OwnerEntityId)
                .Where(g => g.Count() > 10);

            foreach (var userGroup in userSessions)
            {
                var sessionsToRevoke = userGroup
                    .Where(t => t != null)
                    .OrderBy(t => t!.IssuedAt)
                    .Take(userGroup.Count() - 10)
                    .ToList();

                foreach (var token in sessionsToRevoke)
                {
                    if (token == null) continue;
                    var revokedToken = token with
                    {
                        IsRevoked = true,
                        RevokedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await dataContext.Commit(revokedToken);
                    _logger.LogInformation("Revoked excess session {SessionId} for user {UserId}", 
                        token.SessionId, token.OwnerEntityId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enforcing global session limits");
        }
    }
}