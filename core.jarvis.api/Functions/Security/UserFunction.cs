using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.Data;
using core.jarvis.Data.Query;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Function for user profile and navigation endpoints.
/// </summary>
public class UserFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<UserFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public UserFunction(
        IDataContext dataContext,
        ILogger<UserFunction> logger)
    {
        _dataContext = dataContext;
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequestData req)
    {
        try
        {
            // Extract user ID from JWT claims
            var userIdClaim = req.GetClaimValue("sub");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
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

            // Get user profile handler for this user entity
            var userProfileHandler = _dataContext.For<UserProfileHandler>(userId);
            
            // Get user profile
            var userProfile = await userProfileHandler.Get();
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/navigation")] HttpRequestData req)
    {
        try
        {
            // Extract user ID from JWT claims
            var userIdClaim = req.GetClaimValue("sub");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
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

            // Get user profile to check permissions
            var userProfileHandler = _dataContext.For<UserProfileHandler>(userId);
            var userProfile = await userProfileHandler.Get();
            
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
            
            // Get all navigation items and filter by user permissions
            var allNavItems = await _dataContext.Query()
                .WithAll<NavigationItem>(n => true)
                .ToEntityComponents();
                
            var userPermissions = userProfile.PermissionIds;
            var navigation = new List<NavigationItem>();
            
            foreach (var entity in allNavItems)
            {
                var navItem = entity.Value.Get<NavigationItem>();
                if (navItem != null && 
                    (!navItem.RequiredPermissionId.HasValue || 
                     userPermissions.Contains(navItem.RequiredPermissionId.Value.ToString())))
                {
                    navigation.Add(navItem);
                }
            }

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
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/me")] HttpRequestData req)
    {
        try
        {
            // Extract user ID from JWT claims
            var userIdClaim = req.GetClaimValue("sub");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
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

            // Get handler and update profile
            updateProfile.OwnerEntityId = userId;
            var userProfileHandler = _dataContext.For<UserProfileHandler>(userId);
            
            // Handler owns the update operation
            await _dataContext.Commit(updateProfile);
            var updated = await userProfileHandler.Get();

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

/// <summary>
/// Extension methods for HttpRequestData.
/// </summary>
public static partial class HttpRequestDataExtensions
{
    public static string? GetClaimValue(this HttpRequestData request, string claimType)
    {
        // In a real implementation, this would extract claims from the JWT token
        // For now, return a mock value for testing
        if (claimType == "sub")
            return request.Headers.GetValues("X-User-Id")?.FirstOrDefault();
        if (claimType == "email")
            return request.Headers.GetValues("X-User-Email")?.FirstOrDefault();
        return null;
    }
}