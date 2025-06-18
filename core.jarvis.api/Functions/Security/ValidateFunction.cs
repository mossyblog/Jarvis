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
/// Azure Function for validating authentication tokens.
/// </summary>
public class ValidateFunction
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ValidateFunction> _logger;

    public ValidateFunction(IAuthenticationService authService, ILogger<ValidateFunction> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/security/validate
    /// Validates an authentication token.
    /// </summary>
    [Function("validate")]
    [OpenApiOperation(operationId: "validateToken", tags: new[] { "Security" }, Summary = "Validate authentication token", Description = "Validates a token ID and returns its status and associated claims.")]
    [OpenApiParameter(name: "X-Token-Id", In = ParameterLocation.Header, Required = true, Type = typeof(string), Description = "Token ID as a GUID in the X-Token-Id header")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ValidationResponse), Summary = "Validation complete", Description = "Returns validation status and token details")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Bad request", Description = "Missing or invalid token ID")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "security/validate")] HttpRequestData req)
    {
        try
        {
            // Get token ID from header
            var tokenIdHeader = req.Headers.GetValues("X-Token-Id")?.FirstOrDefault();
            if (string.IsNullOrEmpty(tokenIdHeader))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "X-Token-Id header is required");
            }

            // Parse token ID as GUID
            if (!Guid.TryParse(tokenIdHeader, out var tokenId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid token ID format");
            }

            // Validate token
            var validationResponse = await _authService.ValidateTokenAsync(tokenId);
            
            // Return response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(validationResponse));
            
            if (validationResponse.IsValid)
            {
                _logger.LogInformation("Token validated successfully: {TokenId}", tokenId);
            }
            else
            {
                _logger.LogWarning("Token validation failed: {TokenId}, Reason: {Reason}", 
                    tokenId, validationResponse.ErrorMessage);
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in validate function");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred during token validation");
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