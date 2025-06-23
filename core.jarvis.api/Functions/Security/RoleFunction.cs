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
/// Function for role management endpoints.
/// </summary>
public class RoleFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<RoleFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RoleFunction(
        IDataContext dataContext,
        ILogger<RoleFunction> logger)
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
    /// Gets all roles in the system.
    /// </summary>
    [Function("GetRoles")]
    public async Task<HttpResponseData> GetRoles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "security/roles")] HttpRequestData req)
    {
        try
        {
            // Get user ID
            var userIdClaim = req.GetClaimValue("sub");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "User not authenticated");
            }

            // Get system setup handler to retrieve all roles
            var systemId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Well-known system entity ID
            var systemHandler = _dataContext.For<SystemSetupHandler>(systemId);
            var allRoles = await systemHandler.GetAllRoles();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(allRoles, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get roles");
        }
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    [Function("CreateRole")]
    public async Task<HttpResponseData> CreateRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/roles")] HttpRequestData req)
    {
        try
        {
            // Get user ID
            var userIdClaim = req.GetClaimValue("sub");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "User not authenticated");
            }

            // Parse request as Role component
            var requestBody = await req.ReadAsStringAsync();
            var role = JsonSerializer.Deserialize<Role>(requestBody, _jsonOptions);
            if (role == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request body");
            }

            // Create entity and commit component
            var roleId = Guid.NewGuid();
            role.OwnerEntityId = roleId;
            await _dataContext.Commit(role);
            
            // Get the created role
            var roleHandler = _dataContext.For<RoleHandler>(roleId);
            var result = await roleHandler.Get();

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create role");
        }
    }

    /// <summary>
    /// Updates a role.
    /// </summary>
    [Function("UpdateRole")]
    public async Task<HttpResponseData> UpdateRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "security/roles/{roleId}")] HttpRequestData req,
        string roleId)
    {
        try
        {
            // Get user ID
            var userIdClaim = req.GetClaimValue("sub");
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "User not authenticated");
            }

            if (!Guid.TryParse(roleId, out var roleGuid))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid role ID");
            }

            // Parse request as Role component
            var requestBody = await req.ReadAsStringAsync();
            var updateRole = JsonSerializer.Deserialize<Role>(requestBody, _jsonOptions);
            if (updateRole == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request body");
            }

            // Update the role
            updateRole.OwnerEntityId = roleGuid;
            await _dataContext.Commit(updateRole);
            
            // Get the updated role
            var roleHandler = _dataContext.For<RoleHandler>(roleGuid);
            var updatedRole = await roleHandler.Get();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(updatedRole, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update role");
        }
    }

    /// <summary>
    /// Ensures default roles exist.
    /// </summary>
    [Function("EnsureDefaultRoles")]
    public async Task<HttpResponseData> EnsureDefaultRoles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/roles/ensure-defaults")] HttpRequestData req)
    {
        try
        {
            // Get system setup handler
            var systemId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Well-known system entity ID
            var systemHandler = _dataContext.For<SystemSetupHandler>(systemId);
            await systemHandler.EnsureDefaultRoles();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            var result = await systemHandler.EnsureDefaultRoles();
            await response.WriteStringAsync(JsonSerializer.Serialize(result, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring default roles");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to ensure default roles");
        }
    }
}

