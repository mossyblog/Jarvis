using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Middleware;
using core.jarvis.Data;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Function for navigation management endpoints.
/// </summary>
public class NavigationFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<NavigationFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public NavigationFunction(
        IDataContext dataContext,
        ILogger<NavigationFunction> logger)
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
    /// Ensures default navigation items exist.
    /// </summary>
    [Function("EnsureDefaultNavigation")]
    public async Task<HttpResponseData> EnsureDefaultNavigation(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "security/navigation/ensure-defaults")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Get system setup handler directly
            var systemId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Well-known system entity ID
            var systemSetupHandler = _dataContext.For<SystemSetupHandler>(systemId);
            var result = await systemSetupHandler.EnsureDefaultNavigation();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring default navigation");
            var serverError = new Error
            {
                Code = "INTERNAL_ERROR",
                Message = "Failed to ensure default navigation",
                StatusCode = 500
            };
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(serverError, _jsonOptions));
            return errorResponse;
        }
    }

    /// <summary>
    /// Gets navigation items filtered by user permissions following API→Handler→ECS pattern.
    /// </summary>
    [Function("GetNavigationItems")]
    public async Task<HttpResponseData> GetNavigationItems(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "security/navigation")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Get user ID from context
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

            // Step 1: Get user permissions via AccountProfileHandler (proper ECS pattern)
            var profileHandler = _dataContext.For<AccountProfileHandler>(userId);
            SecurityProfile? userProfile = null;
            try
            {
                userProfile = await profileHandler.Get();
            }
            catch
            {
                // User profile doesn't exist - will be handled below
            }
            
            if (userProfile == null)
            {
                _logger.LogWarning("No SecurityProfile found for user {UserId}", userId);
                // Return empty navigation for users without profiles
                var emptyResponse = req.CreateResponse(HttpStatusCode.OK);
                emptyResponse.Headers.Add("Content-Type", "application/json");
                await emptyResponse.WriteStringAsync(JsonSerializer.Serialize(new List<NavigationItem>(), _jsonOptions));
                return emptyResponse;
            }

            // Step 2: Get filtered navigation via NavigationHandler (proper ECS pattern)
            var navigationHandler = _dataContext.For<NavigationHandler>(Guid.NewGuid()); // Handler doesn't need specific entity
            var navigationItems = await navigationHandler.GetUserNavigation(userProfile.PermissionIds);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(navigationItems, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting navigation items");
            var serverError = new Error
            {
                Code = "INTERNAL_ERROR",
                Message = "Failed to get navigation items",
                StatusCode = 500
            };
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(serverError, _jsonOptions));
            return errorResponse;
        }
    }

    /// <summary>
    /// Creates a new navigation item.
    /// </summary>
    [Function("CreateNavigationItem")]
    public async Task<HttpResponseData> CreateNavigationItem(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "security/navigation")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Get user ID from context
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

            // Parse request
            var requestBody = await req.ReadAsStringAsync();
            var navItem = JsonSerializer.Deserialize<NavigationItem>(requestBody, _jsonOptions);
            if (navItem == null)
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

            // Create new navigation item
            var navId = Guid.NewGuid();
            navItem.OwnerEntityId = navId;
            await _dataContext.Commit(navItem);
            
            // Get the created navigation item using handler
            var navHandler = _dataContext.For<NavigationItemHandler>(navId);
            var result = await navHandler.Get();

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating navigation item");
            var serverError = new Error
            {
                Code = "INTERNAL_ERROR",
                Message = "Failed to create navigation item",
                StatusCode = 500
            };
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(serverError, _jsonOptions));
            return errorResponse;
        }
    }
}