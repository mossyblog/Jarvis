using System.Net;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
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
    private readonly IAuthenticationService _authService;
    private readonly ILogger<RefreshFunction> _logger;

    public RefreshFunction(IAuthenticationService authService, ILogger<RefreshFunction> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/security/refresh
    /// Refreshes authentication tokens using a refresh token.
    /// </summary>
    [Function("refresh")]
    [OpenApiOperation(operationId: "refreshToken", tags: new[] { "Security" }, Summary = "Refresh authentication tokens", Description = "Exchange a refresh token for new access and refresh tokens.")]
    [OpenApiRequestBody("application/json", typeof(RefreshTokenRequest), Required = true, Description = "Refresh token request as an IComponent object")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthResponse), Summary = "Token refresh successful", Description = "Returns new JWT access token, refresh token, and session information")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Refresh failed", Description = "Invalid or expired refresh token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Bad request", Description = "Request validation failed")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/refresh")] HttpRequestData req)
    {
        try
        {
            // Parse request body
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            var refreshRequest = JsonConvert.DeserializeObject<RefreshTokenRequest>(requestBody);
            if (refreshRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(refreshRequest.RefreshToken))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Refresh token is required");
            }

            // Refresh tokens
            var authResponse = await _authService.RefreshTokenAsync(refreshRequest);
            
            if (authResponse == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid or expired refresh token");
            }

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(authResponse));
            
            _logger.LogInformation("Tokens refreshed successfully for user: {UserId}", authResponse.UserId);
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
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonConvert.SerializeObject(new { error = message }));
        return response;
    }
}