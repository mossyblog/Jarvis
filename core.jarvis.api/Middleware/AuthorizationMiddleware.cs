using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
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
    
    // Define which endpoints don't require authentication
    private static readonly HashSet<string> PublicEndpoints = new()
    {
        "/api/security/auth",      // Login endpoint
        "/api/security/validate",  // Token validation endpoint
        "/api/swagger",           // Swagger documentation
        "/api/swagger/ui"         // Swagger UI
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
        var authHeader = httpRequest.Headers.GetValues("Authorization")?.FirstOrDefault();
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
            
            var principal = tokenService.ValidateToken(token);
            if (principal == null)
            {
                _logger.LogWarning("Invalid token provided for path: {Path}", path);
                await CreateUnauthorizedResponse(context, "Invalid token");
                return;
            }

            // Extract user information from claims
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var roles = principal.FindFirst("roles")?.Value?.Split(',') ?? Array.Empty<string>();

            // Check role-based authorization for specific endpoints
            if (!HasRequiredRole(path, roles))
            {
                _logger.LogWarning("Access denied for user {UserId} to path: {Path}", userId, path);
                await CreateForbiddenResponse(context, "Insufficient permissions");
                return;
            }

            // Store user context for downstream handlers
            context.Items["UserId"] = userId;
            context.Items["UserEmail"] = email;
            context.Items["UserRoles"] = roles;
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

    private bool HasRequiredRole(string path, string[] userRoles)
    {
        // Define role requirements for specific endpoints
        var roleRequirements = new Dictionary<string, string[]>
        {
            { "/api/security/roles", new[] { "admin" } },           // Role management requires admin
            { "/api/security/accounts", new[] { "admin" } },        // Account management requires admin
            { "/api/security/permissions", new[] { "admin" } },     // Permission management requires admin
            { "/api/security/navigation", new[] { "admin", "user" } } // Navigation available to all authenticated users
        };

        // Check if path has specific role requirements
        foreach (var requirement in roleRequirements)
        {
            if (path.StartsWith(requirement.Key, StringComparison.OrdinalIgnoreCase))
            {
                return requirement.Value.Any(requiredRole => 
                    userRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase));
            }
        }

        // All authenticated users can access endpoints without specific role requirements
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

    public static string[]? GetUserRoles(this FunctionContext context)
    {
        return context.Items.TryGetValue("UserRoles", out var roles) ? roles as string[] : null;
    }

    public static string? GetAuthToken(this FunctionContext context)
    {
        return context.Items.TryGetValue("AuthToken", out var token) ? token?.ToString() : null;
    }
}