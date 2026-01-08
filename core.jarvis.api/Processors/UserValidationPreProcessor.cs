using FastEndpoints;
using core.jarvis.api.Extensions;
using core.jarvis.api.Models;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Processors;

/// <summary>
/// INTENT: Validate that JWT bearer token references a real, active user in the database.
/// PURPOSE: Close security gap where cryptographically valid JWTs could reference non-existent users.
/// BUSINESS CONTEXT: RLS relies on JWT claims for access control - fake user IDs could bypass security.
/// WHY IMPORTANT: Without this, attackers could create valid JWTs for phantom users that don't exist.
/// ARCHITECTURAL SIGNIFICANCE: First line of defense after JWT signature validation.
/// FUTURE RESILIENCE: Catches deleted/deactivated accounts still holding valid tokens.
/// </summary>
public class UserValidationPreProcessor : IGlobalPreProcessor
{
    // Cache validated users to avoid DB hit on every request
    private const string CacheKeyPrefix = "validated_user:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public async Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
    {
        var httpContext = ctx.HttpContext;

        // Skip anonymous requests - let auth middleware handle those
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Extract user ID from JWT claims using safe extension that handles claim mapping
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            // Invalid sub claim format - reject
            await ctx.HttpContext.Response.SendUnauthorizedAsync(ct);
            return;
        }

        // Check cache first
        var cache = httpContext.Resolve<IMemoryCache>();
        var cacheKey = $"{CacheKeyPrefix}{userId}";

        if (cache.TryGetValue<bool>(cacheKey, out var isValid))
        {
            if (!isValid)
            {
                await ctx.HttpContext.Response.SendUnauthorizedAsync(ct);
            }
            return;
        }

        // Query database to verify user exists and is active
        var dataContext = httpContext.Resolve<IDataContext>();
        var logger = httpContext.Resolve<ILogger<UserValidationPreProcessor>>();

        try
        {
            var accounts = await dataContext.Query()
                .WithAll<Account>(Filter<Account>.Eq(a => a.OwnerEntityId, userId))
                .ToEntityComponents();

            var account = accounts.FirstOrDefault().Value?.Get<Account>();

            if (account == null)
            {
                logger.LogWarning(
                    "JWT validation failed: User {UserId} does not exist in database",
                    userId);
                cache.Set(cacheKey, false, CacheExpiration);
                await ctx.HttpContext.Response.SendUnauthorizedAsync(ct);
                return;
            }

            if (!account.IsActive)
            {
                logger.LogWarning(
                    "JWT validation failed: User {UserId} account is deactivated",
                    userId);
                cache.Set(cacheKey, false, CacheExpiration);
                await ctx.HttpContext.Response.SendUnauthorizedAsync(ct);
                return;
            }

            // User is valid - cache positive result
            cache.Set(cacheKey, true, CacheExpiration);
            logger.LogDebug("User {UserId} validated successfully", userId);
        }
        catch (Exception ex)
        {
            // Log but don't fail - allow request through on DB errors
            // This prevents DB outages from blocking all authenticated requests
            logger.LogError(ex, "Error validating user {UserId} - allowing request", userId);
        }
    }
}
