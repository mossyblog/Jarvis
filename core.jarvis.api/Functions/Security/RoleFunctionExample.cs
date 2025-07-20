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
/// Example of RoleFunction with permission-based authorization.
/// This shows how to use the RequirePermission attribute on endpoints.
/// </summary>
public class RoleFunctionExample
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<RoleFunctionExample> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RoleFunctionExample(
        IDataContext dataContext,
        ILogger<RoleFunctionExample> logger)
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
    [Function("GetRolesExample")]
    [RequirePermission("admin.roles.read")]
    public async Task<HttpResponseData> GetRoles(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "security/roles/example")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Authorization is handled by middleware using the RequirePermission attribute
            // No need to manually check permissions here
            
            var roleEntities = await _dataContext.Query()
                .WithAll<Role>(r => true)
                .ToEntityComponents();
            
            var roles = roleEntities.Values
                .Select(entity => entity.Get<Role>())
                .Where(role => role != null)
                .ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(roles);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error retrieving roles");
        }
    }

    /// <summary>
    /// Creates a new role.
    /// Requires admin.roles.write permission.
    /// </summary>
    [Function("CreateRoleExample")]
    [RequirePermission("admin.roles.write")]
    public async Task<HttpResponseData> CreateRole(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "security/roles/example")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Parse request body
            var newRole = await req.ReadFromJsonAsync<Role>();
            if (newRole == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid role data");
            }

            // Create the role
            var roleId = Guid.NewGuid();
            newRole = newRole with { OwnerEntityId = roleId };
            await _dataContext.Commit(newRole);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(newRole);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error creating role");
        }
    }

    /// <summary>
    /// Updates an existing role.
    /// Requires admin.roles.write permission.
    /// </summary>
    [Function("UpdateRoleExample")]
    [RequirePermission("admin.roles.write")]
    public async Task<HttpResponseData> UpdateRole(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "security/roles/example/{roleId}")] HttpRequestData req,
        string roleId,
        FunctionContext executionContext)
    {
        try
        {
            if (!Guid.TryParse(roleId, out var roleGuid))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid role ID");
            }

            var updatedRole = await req.ReadFromJsonAsync<Role>();
            if (updatedRole == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid role data");
            }

            // Update the role
            updatedRole = updatedRole with { OwnerEntityId = roleGuid };
            await _dataContext.Commit(updatedRole);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(updatedRole);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error updating role");
        }
    }

    /// <summary>
    /// Deletes a role.
    /// Requires admin.roles.delete permission.
    /// </summary>
    [Function("DeleteRoleExample")]
    [RequirePermission("admin.roles.delete")]
    public async Task<HttpResponseData> DeleteRole(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "security/roles/example/{roleId}")] HttpRequestData req,
        string roleId,
        FunctionContext executionContext)
    {
        try
        {
            if (!Guid.TryParse(roleId, out var roleGuid))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid role ID");
            }

            // Delete the role
            await _dataContext.Remove<Role>(roleGuid);

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error deleting role");
        }
    }

    /// <summary>
    /// Example of an endpoint requiring multiple permissions (OR logic).
    /// User needs either admin.roles.read OR roles.read permission.
    /// </summary>
    [Function("GetRolesWithOrPermissionExample")]
    [RequirePermission("admin.roles.read")]
    [RequirePermission("roles.read", Operator = PermissionOperator.Or)]
    public async Task<HttpResponseData> GetRolesWithOrPermission(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "security/roles/example/public")] HttpRequestData req,
        FunctionContext executionContext)
    {
        // Implementation similar to GetRoles above
        return await GetRoles(req, executionContext);
    }
}