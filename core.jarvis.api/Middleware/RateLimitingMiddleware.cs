using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Net;
using Newtonsoft.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Security;

namespace core.jarvis.api.Middleware;

/// <summary>
/// Middleware for rate limiting requests to prevent brute force attacks.
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
/// See: https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed
///
/// IP ADDRESS SECURITY: This middleware uses secure IP detection that does NOT trust
/// X-Forwarded-For headers by default. Configure TrustedProxyOptions if your deployment
/// uses trusted reverse proxies.
/// </summary>
public class RateLimitingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();
    private static readonly ConcurrentDictionary<string, AccountLockInfo> _accountLockStore = new();
    
    // Rate Limiting Configuration
    /// <summary>
    /// Maximum number of authentication attempts allowed per minute per IP address.
    /// This helps prevent high-frequency brute force attacks.
    /// </summary>
    private const int MaxAttemptsPerMinute = 5;
    
    /// <summary>
    /// Maximum number of authentication attempts allowed per hour per IP address.
    /// This provides protection against sustained brute force attacks.
    /// </summary>
    private const int MaxAttemptsPerHour = 20;
    
    /// <summary>
    /// Number of consecutive failed authentication attempts that trigger account lockout.
    /// Balance between security and user experience.
    /// </summary>
    private const int MaxFailedAttemptsBeforeLock = 5;
    
    /// <summary>
    /// Duration in minutes for which an account remains locked after exceeding failed attempts.
    /// Industry standard for temporary lockout duration.
    /// </summary>
    private const int LockoutMinutes = 30;
    
    /// <summary>
    /// Base delay in milliseconds added per failed attempt to prevent timing attacks.
    /// Progressive delay helps slow down automated attacks.
    /// </summary>
    private const int ProgressiveDelayMilliseconds = 1000;
    
    /// <summary>
    /// Maximum progressive delay in milliseconds to prevent indefinite delays.
    /// Caps the maximum delay for failed authentication attempts.
    /// </summary>
    private const int MaxProgressiveDelayMilliseconds = 10000;
    
    /// <summary>
    /// HTTP Retry-After header value in seconds for rate-limited responses.
    /// Standard value indicating client should wait before retrying.
    /// </summary>
    private const string RetryAfterSeconds = "60";

    public RateLimitingMiddleware(ILogger<RateLimitingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();
        if (httpRequest == null)
        {
            await next(context);
            return;
        }

        // Only apply rate limiting to security endpoints
        if (!IsSecurityEndpoint(httpRequest.Url.AbsolutePath))
        {
            await next(context);
            return;
        }

        var clientIp = GetClientIp(httpRequest);
        var endpoint = httpRequest.Url.AbsolutePath;
        var key = $"{clientIp}:{endpoint}";

        // Check if IP is rate limited
        if (IsRateLimited(key))
        {
            _logger.LogWarning("Rate limit exceeded for IP {ClientIp} on endpoint {Endpoint}", clientIp, endpoint);
            await CreateRateLimitResponse(context, "Rate limit exceeded. Please try again later.");
            return;
        }

        // For auth endpoints, also check account lockout
        if (endpoint.Contains("/auth", StringComparison.OrdinalIgnoreCase))
        {
            // Cache request body in context items to avoid race condition when stream is re-read later
            var requestBody = await ReadAndCacheRequestBody(context, httpRequest);
            if (!string.IsNullOrEmpty(requestBody))
            {
                try
                {
                    var account = SafeJsonSettings.Deserialize<Account>(requestBody);
                    if (!string.IsNullOrEmpty(account?.Email))
                    {
                        if (IsAccountLocked(account.Email))
                        {
                            _logger.LogWarning("Account locked due to failed attempts: {Email}", account.Email);

                            // Add progressive delay to prevent timing attacks
                            var lockInfo = _accountLockStore.GetValueOrDefault(account.Email.ToLowerInvariant());
                            if (lockInfo != null)
                            {
                                var delay = lockInfo.FailedAttempts * ProgressiveDelayMilliseconds;
                                await Task.Delay(Math.Min(delay, MaxProgressiveDelayMilliseconds));
                            }

                            await CreateRateLimitResponse(context, "Account temporarily locked due to multiple failed attempts.");
                            return;
                        }
                    }
                }
                catch
                {
                    // If we can't parse the request, continue but log it
                    _logger.LogWarning("Failed to parse authentication request for rate limiting");
                }
            }
        }

        // Track the request
        TrackRequest(key);

        // Execute the function
        await next(context);

        // After execution, check if it was a failed auth attempt
        await CheckForFailedAuthentication(context, httpRequest);
    }

    private bool IsSecurityEndpoint(string path)
    {
        return path.StartsWith("/api/security/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the client IP address with secure handling of forwarded headers.
    ///
    /// SECURITY: X-Forwarded-For headers are NOT trusted by default.
    /// Only the socket-level IP (X-Azure-ClientIP) is used unless trusted proxies are configured.
    /// This prevents IP spoofing attacks where attackers send fake X-Forwarded-For headers.
    /// </summary>
    private string GetClientIp(HttpRequestData request, TrustedProxyOptions? trustedProxyOptions = null)
    {
        // Get the socket-level IP address (not spoofable)
        // In Azure Functions, X-Azure-ClientIP contains the actual client IP
        string? socketIp = null;
        if (request.Headers.TryGetValues("X-Azure-ClientIP", out var azureIp))
        {
            socketIp = azureIp.FirstOrDefault()?.Trim();
        }

        // Only trust X-Forwarded-For if:
        // 1. Trusted proxy validation is enabled
        // 2. We have configured trusted proxy IPs
        // 3. The socket IP is from a trusted proxy
        if (trustedProxyOptions?.EnableTrustedProxyValidation == true &&
            trustedProxyOptions.TrustedProxyIps?.Any() == true &&
            socketIp != null &&
            trustedProxyOptions.TrustedProxyIps.Contains(socketIp))
        {
            // Request is from a trusted proxy, use X-Forwarded-For
            if (request.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
            {
                var forwardedValue = forwardedFor.FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedValue))
                {
                    // Use the LAST (rightmost) IP in the chain - this is the client IP
                    // added by the trusted proxy. Earlier IPs in the chain can be spoofed.
                    var ips = forwardedValue.Split(',');
                    var clientIp = ips.LastOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(clientIp))
                    {
                        return clientIp;
                    }
                }
            }
        }

        // Default: use socket address (not spoofable)
        if (!string.IsNullOrEmpty(socketIp))
        {
            return socketIp;
        }

        // Fallback for non-Azure environments (local development)
        if (request.Headers.TryGetValues("REMOTE_ADDR", out var remoteAddr))
        {
            var addr = remoteAddr.FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(addr))
            {
                return addr;
            }
        }

        return "unknown";
    }

    private bool IsRateLimited(string key)
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

    private bool IsAccountLocked(string email)
    {
        var key = email.ToLowerInvariant();
        if (!_accountLockStore.TryGetValue(key, out var info))
            return false;

        if (info.LockedUntil > DateTime.UtcNow)
            return true;

        // Lock expired, remove it
        _accountLockStore.TryRemove(key, out _);
        return false;
    }

    private void TrackRequest(string key)
    {
        _rateLimitStore.AddOrUpdate(key,
            k => new RateLimitInfo { Attempts = new List<DateTime> { DateTime.UtcNow } },
            (k, info) =>
            {
                info.Attempts.Add(DateTime.UtcNow);
                // Keep only last hour of attempts
                info.Attempts = info.Attempts.Where(a => (DateTime.UtcNow - a).TotalHours <= 1).ToList();
                return info;
            });
    }

    /// <summary>
    /// Context items key for cached request body
    /// </summary>
    private const string RequestBodyCacheKey = "RateLimiting_RequestBody";

    /// <summary>
    /// Reads the request body and caches it in the function context to prevent race conditions
    /// when the body needs to be read multiple times (e.g., for account lockout check and failed auth tracking).
    /// </summary>
    private async Task<string?> ReadAndCacheRequestBody(FunctionContext context, HttpRequestData request)
    {
        // Check if already cached
        if (context.Items.TryGetValue(RequestBodyCacheKey, out var cached))
        {
            return cached as string;
        }

        // Read and cache the body
        var body = await request.ReadAsStringAsync();
        context.Items[RequestBodyCacheKey] = body;
        return body;
    }

    /// <summary>
    /// Gets the cached request body from the function context.
    /// Returns null if no body was cached.
    /// </summary>
    private string? GetCachedRequestBody(FunctionContext context)
    {
        return context.Items.TryGetValue(RequestBodyCacheKey, out var cached) ? cached as string : null;
    }

    private Task CheckForFailedAuthentication(FunctionContext context, HttpRequestData request)
    {
        // Only check auth endpoints
        if (!request.Url.AbsolutePath.Contains("/auth", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var response = context.GetHttpResponseData();
        if (response == null || response.StatusCode == HttpStatusCode.OK)
            return Task.CompletedTask;

        // Failed authentication attempt - use cached body to avoid race condition
        var requestBody = GetCachedRequestBody(context);
        if (string.IsNullOrEmpty(requestBody))
            return Task.CompletedTask;

        try
        {
            var account = SafeJsonSettings.Deserialize<Account>(requestBody);
            if (!string.IsNullOrEmpty(account?.Email))
            {
                TrackFailedAttempt(account.Email);
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return Task.CompletedTask;
    }

    private void TrackFailedAttempt(string email)
    {
        var key = email.ToLowerInvariant();
        _accountLockStore.AddOrUpdate(key,
            k => new AccountLockInfo 
            { 
                Email = email,
                FailedAttempts = 1,
                LastAttempt = DateTime.UtcNow
            },
            (k, info) =>
            {
                info.FailedAttempts++;
                info.LastAttempt = DateTime.UtcNow;
                
                if (info.FailedAttempts >= MaxFailedAttemptsBeforeLock)
                {
                    info.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    _logger.LogWarning("Account {Email} locked until {LockedUntil} after {Attempts} failed attempts",
                        email, info.LockedUntil, info.FailedAttempts);
                }
                
                return info;
            });
    }

    private async Task CreateRateLimitResponse(FunctionContext context, string message)
    {
        var error = new Error
        {
            OwnerEntityId = Guid.NewGuid(),
            Code = "RATE_LIMIT_EXCEEDED",
            Message = message,
            StatusCode = 429
        };

        var response = context.GetHttpResponseData();
        if (response == null)
        {
            response = context.GetInvocationResult().Value as HttpResponseData;
        }

        if (response != null)
        {
            response.StatusCode = HttpStatusCode.TooManyRequests;
            response.Headers.Add("Content-Type", "application/json");
            response.Headers.Add("Retry-After", RetryAfterSeconds);
            await response.WriteStringAsync(SafeJsonSettings.Serialize(error));
        }
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