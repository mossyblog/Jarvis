using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/navigation/ensure-defaults")] HttpRequestData req)
    {
        try
        {
            // Get system setup handler
            var systemId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Well-known system entity ID
            var systemHandler = _dataContext.For<SystemSetupHandler>(systemId);
            await systemHandler.EnsureDefaultNavigation();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            var result = await systemHandler.EnsureDefaultNavigation();
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
    /// Gets all navigation items.
    /// </summary>
    [Function("GetNavigationItems")]
    public async Task<HttpResponseData> GetNavigationItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "security/navigation")] HttpRequestData req)
    {
        try
        {
            // Get user ID
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

            // Get all navigation items
            var navEntities = await _dataContext.Query()
                .WithAll<NavigationItem>(n => true)
                .ToEntityComponents();

            var navigationItems = new List<NavigationItem>();
            foreach (var kvp in navEntities)
            {
                var navItem = kvp.Value.Get<NavigationItem>();
                if (navItem != null)
                {
                    navigationItems.Add(navItem);
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(navigationItems.OrderBy(n => n.SortOrder), _jsonOptions));
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/navigation")] HttpRequestData req)
    {
        try
        {
            // Get user ID
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

            // Create entity and commit component
            var navId = Guid.NewGuid();
            navItem.OwnerEntityId = navId;
            await _dataContext.Commit(navItem);
            
            // Let handler process it
            var navHandler = _dataContext.For<NavigationItemHandler>(navId);
            var result = await navHandler.Save();

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