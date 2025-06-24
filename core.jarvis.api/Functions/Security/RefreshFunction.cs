using System.Net;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Azure Function for refreshing authentication tokens.
/// </summary>
public class RefreshFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<RefreshFunction> _logger;

    public RefreshFunction(IDataContext dataContext, ILogger<RefreshFunction> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/security/refresh
    /// Refreshes authentication tokens using a refresh token.
    /// </summary>
    [Function("refresh")]
    [OpenApiOperation(operationId: "refreshToken", tags: new[] { "Security" }, Summary = "Refresh authentication tokens", Description = "Exchange a refresh token for new access and refresh tokens.")]
    [OpenApiRequestBody("application/json", typeof(AuthToken), Required = true, Description = "AuthToken component with refresh token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthToken), Summary = "Token refresh successful", Description = "Returns new JWT access token, refresh token, and session information")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Refresh failed", Description = "Invalid or expired refresh token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Bad request", Description = "Request validation failed")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "security/refresh")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Parse request body
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            var refreshAuth = JsonConvert.DeserializeObject<AuthToken>(requestBody);
            if (refreshAuth == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(refreshAuth.RefreshToken))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Refresh token is required");
            }

            // Create entity for the AuthToken component
            var tokenEntityId = Guid.NewGuid();
            refreshAuth.OwnerEntityId = tokenEntityId;
            await _dataContext.Commit(refreshAuth);
            
            // Get entity-bound handler
            var authTokenHandler = _dataContext.For<AuthTokenHandler>(tokenEntityId);
            
            // Refresh tokens
            var authResponse = await authTokenHandler.RefreshToken();

            // Check if refresh succeeded by looking for new tokens
            if (string.IsNullOrEmpty(authResponse.AccessToken))
            {
                var error = new Error
                {
                    OwnerEntityId = Guid.NewGuid(),
                    Code = "REFRESH_FAILED",
                    Message = "Invalid or expired refresh token",
                    StatusCode = 401
                };
                
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonConvert.SerializeObject(error));
                return errorResponse;
            }

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(authResponse));

            _logger.LogInformation("Tokens refreshed successfully for entity: {EntityId}", authResponse.OwnerEntityId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in refresh function");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred during token refresh");
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var error = new Error
        {
            OwnerEntityId = Guid.NewGuid(),
            Code = statusCode.ToString(),
            Message = message,
            StatusCode = (int)statusCode
        };
        
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonConvert.SerializeObject(error));
        return response;
    }
}