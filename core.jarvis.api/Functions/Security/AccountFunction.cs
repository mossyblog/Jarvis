using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Middleware;
using core.jarvis.Systems;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Function for account profile and navigation endpoints.
/// </summary>
public class AccountFunction
{
    private readonly ISystem _system;
    private readonly ILogger<AccountFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AccountFunction(
        ISystem system,
        ILogger<AccountFunction> logger)
    {
        _system = system;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Gets the current user's profile with roles.
    /// </summary>
    [Function("GetCurrentUser")]
    public async Task<HttpResponseData> GetCurrentUser(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/me")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Extract user ID from context (set by AuthorizationMiddleware)
            var userIdStr = executionContext.GetUserId();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                var authError = new Error
                {
                    Code = "UNAUTHORIZED",
                    Message = "User not authenticated",
                    StatusCode = 401
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(authError, _jsonOptions));
                return errorResponse;
            }

            // Get user profile using System
            var userProfile = await _system.ExecuteHandler<AccountProfileHandler, SecurityProfile>(
                userId,
                handler => handler.Get());
            if (userProfile == null)
            {
                var notFoundError = new Error
                {
                    Code = "USER_NOT_FOUND",
                    Message = "User profile not found",
                    StatusCode = 404
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(notFoundError, _jsonOptions));
                return errorResponse;
            }

            // Return the SecurityProfile component directly (includes roles)
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(userProfile, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            var serverError = new Error
            {
                Code = "INTERNAL_ERROR",
                Message = "Failed to get user profile",
                StatusCode = 500
            };
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(serverError, _jsonOptions));
            return errorResponse;
        }
    }

    /// <summary>
    /// Gets the navigation items available to the current user.
    /// </summary>
    [Function("GetUserNavigation")]
    public async Task<HttpResponseData> GetUserNavigation(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accounts/navigation")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Extract user ID from context (set by AuthorizationMiddleware)
            var userIdStr = executionContext.GetUserId();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                var authError = new Error
                {
                    Code = "UNAUTHORIZED",
                    Message = "User not authenticated",
                    StatusCode = 401
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(authError, _jsonOptions));
                return errorResponse;
            }

            // Get user navigation using System - ALL logic in handler
            var navigation = await _system.ExecuteHandlerWithResult<AccountProfileHandler, List<NavigationItem>>(
                userId,
                handler => handler.GetUserNavigation());

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(navigation, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user navigation");
            var serverError = new Error
            {
                Code = "INTERNAL_ERROR",
                Message = "Failed to get navigation",
                StatusCode = 500
            };
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(serverError, _jsonOptions));
            return errorResponse;
        }
    }

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    [Function("UpdateUserProfile")]
    public async Task<HttpResponseData> UpdateUserProfile(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "accounts/me")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Extract user ID from context (set by AuthorizationMiddleware)
            var userIdStr = executionContext.GetUserId();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                var authError = new Error
                {
                    Code = "UNAUTHORIZED",
                    Message = "User not authenticated",
                    StatusCode = 401
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(authError, _jsonOptions));
                return errorResponse;
            }

            // Parse request body as SecurityProfile component
            var requestBody = await req.ReadAsStringAsync();
            var updateProfile = JsonSerializer.Deserialize<SecurityProfile>(requestBody, _jsonOptions);
            if (updateProfile == null)
            {
                var badRequestError = new Error
                {
                    Code = "INVALID_REQUEST",
                    Message = "Invalid request body",
                    StatusCode = 400
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(badRequestError, _jsonOptions));
                return errorResponse;
            }

            // Update profile using System - ALL logic in handler
            var updated = await _system.ExecuteHandler<AccountProfileHandler, SecurityProfile>(
                userId,
                handler => handler.UpdateProfile(updateProfile));

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(updated, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            var serverError = new Error
            {
                Code = "INTERNAL_ERROR",
                Message = "Failed to update profile",
                StatusCode = 500
            };
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(serverError, _jsonOptions));
            return errorResponse;
        }
    }
}