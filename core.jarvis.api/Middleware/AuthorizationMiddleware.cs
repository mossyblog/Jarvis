using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using core.jarvis.api.Attributes;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace core.jarvis.api.Middleware;

/// <summary>
/// Middleware to handle JWT authorization for protected endpoints.
/// </summary>
public class AuthorizationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<AuthorizationMiddleware> _logger;

    // Cache for reflection-based permission attribute lookups
    // Key: method entry point string, Value: list of RequirePermissionAttribute
    private static readonly ConcurrentDictionary<string, List<RequirePermissionAttribute>> _permissionAttributeCache = new();

    // Define which endpoints don't require authentication
    private static readonly HashSet<string> PublicEndpoints = new()
    {
        "/api/security/auth",      // Login endpoint
        "/api/auth/register",      // Registration endpoint
        "/api/security/validate",  // Token validation endpoint
        "/api/swagger",           // Swagger documentation
        "/api/swagger/ui",        // Swagger UI
        "/api/health"             // Health check endpoint
    };

    public AuthorizationMiddleware(ILogger<AuthorizationMiddleware> logger)
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

        var path = httpRequest.Url.AbsolutePath.ToLower();

        // Allow public endpoints without authentication
        if (IsPublicEndpoint(path))
        {
            await next(context);
            return;
        }

        // Extract and validate JWT token
        string? authHeader = null;
        if (httpRequest.Headers.TryGetValues("Authorization", out var authValues))
        {
            authHeader = authValues?.FirstOrDefault();
        }

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Missing or invalid Authorization header for path: {Path}", path);
            await CreateUnauthorizedResponse(context, "Authorization header required");
            return;
        }

        var token = authHeader.Substring(7); // Remove "Bearer " prefix

        try
        {
            // Validate token
            var configuration = context.InstanceServices.GetRequiredService<IConfiguration>();
            var tokenService = context.InstanceServices.GetRequiredService<ITokenService>();

            var principal = tokenService.Validate(token);
            if (principal == null)
            {
                _logger.LogWarning("Invalid token provided for path: {Path}", path);
                await CreateUnauthorizedResponse(context, "Invalid token");
                return;
            }

            // Extract user information from claims
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var sessionId = principal.FindFirst("sessionId")?.Value;

            // Ensure we have a valid user ID
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Token missing user identifier claim for path: {Path}", path);
                await CreateUnauthorizedResponse(context, "Invalid token claims");
                return;
            }

            // Check token revocation
            if (!string.IsNullOrEmpty(sessionId) && Guid.TryParse(sessionId, out var sessionGuid))
            {
                var dataContext = context.InstanceServices.GetRequiredService<IDataContext>();
                var authTokens = await dataContext.Query()
                    .WithAll<AuthToken>(Filter<AuthToken>.Eq(t => t.SessionId, sessionGuid).And(Filter<AuthToken>.Eq(t => t.IsRevoked, true)))
                    .ToEntityComponents();

                if (authTokens.Any())
                {
                    _logger.LogWarning("Revoked token used by user {UserId} for path: {Path}", userId, path);
                    await CreateUnauthorizedResponse(context, "Token has been revoked");
                    return;
                }
            }

            // Load user permissions from security profile
            var permissionService = context.InstanceServices.GetRequiredService<IPermissionService>();
            var securityProfile = await permissionService.GetSecurityProfileAsync(userGuid);
            var userPermissions = await permissionService.GetUserPermissionsAsync(userGuid);

            // Check permission-based authorization for specific endpoints
            if (!await HasRequiredPermission(context, path, userGuid, permissionService))
            {
                _logger.LogWarning("Access denied for user {UserId} to path: {Path}", userId, path);
                await CreateForbiddenResponse(context, "Insufficient permissions");
                return;
            }

            // Store user context for downstream handlers
            context.Items["UserId"] = userId;
            context.Items["UserEmail"] = email ?? string.Empty;
            context.Items["UserPermissions"] = userPermissions;
            context.Items["SecurityProfile"] = securityProfile;
            context.Items["AuthToken"] = token;

            _logger.LogInformation("Authorized user {UserId} for path: {Path}", userId, path);

            // Continue to the function
            await next(context);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            await CreateUnauthorizedResponse(context, "Token validation failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authorization middleware");
            await CreateErrorResponse(context, "Authorization error occurred");
        }
    }

    private bool IsPublicEndpoint(string path)
    {
        return PublicEndpoints.Any(endpoint => path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> HasRequiredPermission(FunctionContext context, string path, Guid userId, IPermissionService permissionService)
    {
        // First check for RequirePermission attributes on the function
        var targetMethod = context.FunctionDefinition?.EntryPoint;
        if (!string.IsNullOrEmpty(targetMethod))
        {
            // Get cached permission attributes or compute and cache them
            var permissionAttributes = _permissionAttributeCache.GetOrAdd(targetMethod, entryPoint =>
            {
                // Parse the method info from EntryPoint (format: "Namespace.Class.Method")
                var lastDotIndex = entryPoint.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    var typeName = entryPoint.Substring(0, lastDotIndex);
                    var methodName = entryPoint.Substring(lastDotIndex + 1);

                    // Try to find the type and method
                    var type = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == typeName);

                    if (type != null)
                    {
                        var method = type.GetMethod(methodName);
                        if (method != null)
                        {
                            return method.GetCustomAttributes<RequirePermissionAttribute>().ToList();
                        }
                    }
                }
                return new List<RequirePermissionAttribute>();
            });

            if (permissionAttributes.Any())
            {
                // Group by operator
                var orPermissions = permissionAttributes
                    .Where(p => p.Operator == PermissionOperator.Or)
                    .Select(p => p.Permission)
                    .ToList();

                var andPermissions = permissionAttributes
                    .Where(p => p.Operator == PermissionOperator.And)
                    .Select(p => p.Permission)
                    .ToList();

                // Check OR permissions (user needs at least one)
                if (orPermissions.Any())
                {
                    var hasAnyOrPermission = await permissionService.HasAnyPermissionAsync(userId, orPermissions.ToArray());
                    if (!hasAnyOrPermission)
                    {
                        return false;
                    }
                }

                // Check AND permissions (user needs all)
                if (andPermissions.Any())
                {
                    var hasAllAndPermissions = await permissionService.HasAllPermissionsAsync(userId, andPermissions.ToArray());
                    if (!hasAllAndPermissions)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // Fallback to path-based permission requirements for backward compatibility
        var permissionRequirements = new Dictionary<string, string>
        {
            { "/api/security/roles", "admin.roles" },           // Role management
            { "/api/security/accounts", "admin.accounts" },     // Account management
            { "/api/security/permissions", "admin.permissions" }, // Permission management
            { "/api/security/navigation", null }                // Navigation available to all authenticated users
        };

        // Check if path has specific permission requirements
        foreach (var requirement in permissionRequirements)
        {
            if (path.StartsWith(requirement.Key, StringComparison.OrdinalIgnoreCase))
            {
                // Null permission means any authenticated user can access
                if (requirement.Value == null)
                {
                    return true;
                }

                return await permissionService.HasPermissionAsync(userId, requirement.Value);
            }
        }

        // All authenticated users can access endpoints without specific permission requirements
        return true;
    }

    private async Task CreateUnauthorizedResponse(FunctionContext context, string message)
    {
        var error = ErrorResponseService.CreateError("AUTH_REQUIRED");
        await WriteErrorResponse(context, HttpStatusCode.Unauthorized, error);
    }

    private async Task CreateForbiddenResponse(FunctionContext context, string message)
    {
        var error = ErrorResponseService.CreateError("ACCESS_DENIED");
        await WriteErrorResponse(context, HttpStatusCode.Forbidden, error);
    }

    private async Task CreateErrorResponse(FunctionContext context, string message)
    {
        var error = ErrorResponseService.CreateError("SERVER_ERROR");
        await WriteErrorResponse(context, HttpStatusCode.InternalServerError, error);
    }

    private async Task WriteErrorResponse(FunctionContext context, HttpStatusCode statusCode, Error error)
    {
        var httpRequest = await context.GetHttpRequestDataAsync();
        if (httpRequest != null)
        {
            var response = httpRequest.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(error));

            // Set the response in the context to prevent further processing
            context.GetInvocationResult().Value = response;
        }
    }
}

/// <summary>
/// Extension methods for FunctionContext to access user information.
/// </summary>
public static class FunctionContextExtensions
{
    public static string? GetUserId(this FunctionContext context)
    {
        return context.Items.TryGetValue("UserId", out var userId) ? userId?.ToString() : null;
    }

    public static string? GetUserEmail(this FunctionContext context)
    {
        return context.Items.TryGetValue("UserEmail", out var email) ? email?.ToString() : null;
    }

    public static HashSet<string>? GetUserPermissions(this FunctionContext context)
    {
        return context.Items.TryGetValue("UserPermissions", out var permissions) ? permissions as HashSet<string> : null;
    }

    public static SecurityProfile? GetSecurityProfile(this FunctionContext context)
    {
        return context.Items.TryGetValue("SecurityProfile", out var profile) ? profile as SecurityProfile : null;
    }

    public static string? GetAuthToken(this FunctionContext context)
    {
        return context.Items.TryGetValue("AuthToken", out var token) ? token?.ToString() : null;
    }
}
