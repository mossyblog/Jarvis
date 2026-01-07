using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Middleware;
using core.jarvis.api.Extensions;
using core.jarvis.api.Attributes;
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
    /// Requires admin.roles.read permission.
    /// </summary>
    [Function("GetRoles")]
    [RequirePermission("admin.roles.read")]
    public async Task<HttpResponseData> GetRoles(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "security/roles")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Get user ID from context
            var userIdStr = executionContext.GetUserId();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.Unauthorized, "User not authenticated");
            }

            // Get all roles using handler directly
            var systemId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Well-known system entity ID
            var systemSetupHandler = _dataContext.For<SystemSetupHandler>(systemId);
            var allRoles = await systemSetupHandler.GetAllRoles();

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
    /// Requires admin.roles.write permission.
    /// </summary>
    [Function("CreateRole")]
    [RequirePermission("admin.roles.write")]
    public async Task<HttpResponseData> CreateRole(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "security/roles")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Get user ID from context
            var userIdStr = executionContext.GetUserId();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
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

            // Create new role using handler - NO direct commits!
            var roleId = Guid.NewGuid();
            var roleHandler = _dataContext.For<RoleHandler>(roleId);
            var result = await roleHandler.CreateRole(role);

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
    /// Requires admin.roles.write permission.
    /// </summary>
    [Function("UpdateRole")]
    [RequirePermission("admin.roles.write")]
    public async Task<HttpResponseData> UpdateRole(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "security/roles/{roleId}")] HttpRequestData req,
        string roleId,
        FunctionContext executionContext)
    {
        try
        {
            // Get user ID from context
            var userIdStr = executionContext.GetUserId();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
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

            // Update the role using handler - NO direct commits!
            var roleHandler = _dataContext.For<RoleHandler>(roleGuid);
            var updatedRole = await roleHandler.UpdateRole(updateRole);

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
    /// Requires admin.roles.write permission.
    /// </summary>
    [Function("EnsureDefaultRoles")]
    [RequirePermission("admin.roles.write")]
    public async Task<HttpResponseData> EnsureDefaultRoles(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "security/roles/ensure-defaults")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Ensure default roles using handler directly
            var systemId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Well-known system entity ID
            var systemSetupHandler = _dataContext.For<SystemSetupHandler>(systemId);
            var result = await systemSetupHandler.EnsureDefaultRoles();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
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

