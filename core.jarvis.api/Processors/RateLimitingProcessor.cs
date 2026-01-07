using System.Collections.Concurrent;
using FastEndpoints;
using core.jarvis.api.Models;
using core.jarvis.api.Security;

namespace core.jarvis.api.Processors;

/// <summary>
/// Pre-processor for rate limiting requests to prevent brute force attacks.
///
/// SECURITY LIMITATION: Rate limiting uses in-memory storage and is NOT distributed.
///
/// In multi-instance deployments behind a load balancer:
/// - Rate limits apply per-instance, not globally
/// - Users can exceed intended limits by hitting different instances
/// - Account lockouts are not shared across instances
///
/// PRODUCTION RECOMMENDATION: For multi-instance deployments, replace
/// ConcurrentDictionary with Redis or another distributed cache.
/// </summary>
public class RateLimitingPreProcessor : IGlobalPreProcessor
{
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();
    private static readonly ConcurrentDictionary<string, AccountLockInfo> _accountLockStore = new();

    private const int MaxAttemptsPerMinute = 5;
    private const int MaxAttemptsPerHour = 20;
    private const int MaxFailedAttemptsBeforeLock = 5;
    private const int LockoutMinutes = 30;
    private const int ProgressiveDelayMilliseconds = 1000;
    private const int MaxProgressiveDelayMilliseconds = 10000;

    public async Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
    {
        var httpContext = ctx.HttpContext;
        var path = httpContext.Request.Path.Value ?? string.Empty;

        // Only apply rate limiting to security endpoints
        if (!path.StartsWith("/api/security/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var clientIp = GetClientIp(httpContext);
        var key = $"{clientIp}:{path}";

        // Check if IP is rate limited
        if (IsRateLimited(key))
        {
            var logger = httpContext.Resolve<ILogger<RateLimitingPreProcessor>>();
            logger.LogWarning("Rate limit exceeded for IP {ClientIp} on endpoint {Endpoint}", clientIp, path);

            httpContext.Response.Headers["Retry-After"] = "60";
            await ctx.HttpContext.Response.SendAsync(new Error
            {
                OwnerEntityId = Guid.NewGuid(),
                Code = "RATE_LIMIT_EXCEEDED",
                Message = "Rate limit exceeded. Please try again later.",
                StatusCode = 429
            }, 429, cancellation: ct);
            return;
        }

        // Track the request
        TrackRequest(key);
    }

    private static string GetClientIp(HttpContext httpContext)
    {
        // Get socket-level IP (not spoofable)
        var socketIp = httpContext.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(socketIp))
        {
            return socketIp;
        }

        return "unknown";
    }

    private static bool IsRateLimited(string key)
    {
        if (!_rateLimitStore.TryGetValue(key, out var info))
            return false;

        var now = DateTime.UtcNow;

        // Check minute limit
        var recentMinuteAttempts = info.Attempts.Count(a => (now - a).TotalMinutes <= 1);
        if (recentMinuteAttempts >= MaxAttemptsPerMinute)
            return true;

        // Check hour limit
        var recentHourAttempts = info.Attempts.Count(a => (now - a).TotalHours <= 1);
        if (recentHourAttempts >= MaxAttemptsPerHour)
            return true;

        return false;
    }

    private static void TrackRequest(string key)
    {
        _rateLimitStore.AddOrUpdate(key,
            _ => new RateLimitInfo { Attempts = new List<DateTime> { DateTime.UtcNow } },
            (_, info) =>
            {
                info.Attempts.Add(DateTime.UtcNow);
                // Keep only last hour of attempts
                info.Attempts = info.Attempts.Where(a => (DateTime.UtcNow - a).TotalHours <= 1).ToList();
                return info;
            });
    }

    /// <summary>
    /// Checks if an account is locked and applies progressive delay
    /// </summary>
    public static async Task<bool> IsAccountLockedAsync(string email, ILogger logger)
    {
        var key = email.ToLowerInvariant();
        if (!_accountLockStore.TryGetValue(key, out var info))
            return false;

        if (info.LockedUntil > DateTime.UtcNow)
        {
            // Add progressive delay to prevent timing attacks
            var delay = info.FailedAttempts * ProgressiveDelayMilliseconds;
            await Task.Delay(Math.Min(delay, MaxProgressiveDelayMilliseconds));
            return true;
        }

        // Lock expired, remove it
        _accountLockStore.TryRemove(key, out _);
        return false;
    }

    /// <summary>
    /// Tracks a failed authentication attempt for the specified email
    /// </summary>
    public static void TrackFailedAttempt(string email, ILogger logger)
    {
        var key = email.ToLowerInvariant();
        _accountLockStore.AddOrUpdate(key,
            _ => new AccountLockInfo
            {
                Email = email,
                FailedAttempts = 1,
                LastAttempt = DateTime.UtcNow
            },
            (_, info) =>
            {
                info.FailedAttempts++;
                info.LastAttempt = DateTime.UtcNow;

                if (info.FailedAttempts >= MaxFailedAttemptsBeforeLock)
                {
                    info.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    logger.LogWarning("Account {Email} locked until {LockedUntil} after {Attempts} failed attempts",
                        email, info.LockedUntil, info.FailedAttempts);
                }

                return info;
            });
    }

    private class RateLimitInfo
    {
        public List<DateTime> Attempts { get; set; } = new();
    }

    private class AccountLockInfo
    {
        public string Email { get; set; } = string.Empty;
        public int FailedAttempts { get; set; }
        public DateTime LastAttempt { get; set; }
        public DateTime LockedUntil { get; set; }
    }
}
