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

namespace core.jarvis.api.Middleware;

/// <summary>
/// Middleware for rate limiting requests to prevent brute force attacks.
/// </summary>
public class RateLimitingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();
    private static readonly ConcurrentDictionary<string, AccountLockInfo> _accountLockStore = new();
    
    // Configuration
    private const int MaxAttemptsPerMinute = 5;
    private const int MaxAttemptsPerHour = 20;
    private const int MaxFailedAttemptsBeforeLock = 5;
    private const int LockoutMinutes = 30;
    private const int ProgressiveDelayMilliseconds = 1000; // Add 1 second per failed attempt

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
            var requestBody = await httpRequest.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(requestBody))
            {
                try
                {
                    var user = JsonConvert.DeserializeObject<User>(requestBody);
                    if (!string.IsNullOrEmpty(user?.Email))
                    {
                        if (IsAccountLocked(user.Email))
                        {
                            _logger.LogWarning("Account locked due to failed attempts: {Email}", user.Email);
                            
                            // Add progressive delay to prevent timing attacks
                            var lockInfo = _accountLockStore.GetValueOrDefault(user.Email.ToLowerInvariant());
                            if (lockInfo != null)
                            {
                                var delay = lockInfo.FailedAttempts * ProgressiveDelayMilliseconds;
                                await Task.Delay(Math.Min(delay, 10000)); // Max 10 second delay
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

    private string GetClientIp(HttpRequestData request)
    {
        // Check for forwarded IP first (behind proxy/load balancer)
        if (request.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        if (request.Headers.TryGetValues("X-Real-IP", out var realIp))
        {
            var ip = realIp.FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Fallback to remote address
        if (request.Headers.TryGetValues("REMOTE_ADDR", out var remoteAddr))
        {
            return remoteAddr.FirstOrDefault() ?? "unknown";
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

    private async Task CheckForFailedAuthentication(FunctionContext context, HttpRequestData request)
    {
        // Only check auth endpoints
        if (!request.Url.AbsolutePath.Contains("/auth", StringComparison.OrdinalIgnoreCase))
            return;

        var response = context.GetHttpResponseData();
        if (response == null || response.StatusCode == HttpStatusCode.OK)
            return;

        // Failed authentication attempt
        var requestBody = await request.ReadAsStringAsync();
        if (string.IsNullOrEmpty(requestBody))
            return;

        try
        {
            var user = JsonConvert.DeserializeObject<User>(requestBody);
            if (!string.IsNullOrEmpty(user?.Email))
            {
                TrackFailedAttempt(user.Email);
            }
        }
        catch
        {
            // Ignore parsing errors
        }
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
            response.Headers.Add("Retry-After", "60"); // Tell client to retry after 60 seconds
            await response.WriteStringAsync(JsonConvert.SerializeObject(error));
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