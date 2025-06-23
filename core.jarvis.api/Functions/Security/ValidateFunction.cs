using System.Net;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Text.Json;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Azure Function for validating authentication tokens.
/// </summary>
public class ValidateFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<ValidateFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ValidateFunction(IDataContext dataContext, ILogger<ValidateFunction> logger)
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
    /// GET /api/security/validate
    /// Validates an authentication token.
    /// </summary>
    [Function("validate")]
    [OpenApiOperation(operationId: "validateToken", tags: new[] { "Security" }, Summary = "Validate authentication token", Description = "Validates a token and returns its status and associated claims.")]
    [OpenApiRequestBody("application/json", typeof(TokenValidation), Required = true, Description = "TokenValidation component with token data to validate")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TokenValidation), Summary = "Validation complete", Description = "Returns validation status and token details")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Error), Summary = "Bad request", Description = "Invalid request format")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "security/validate")] HttpRequestData req)
    {
        try
        {
            // Parse request body as TokenValidation component
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                var emptyError = new Error
                {
                    Code = "INVALID_REQUEST",
                    Message = "Request body is required",
                    StatusCode = 400
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(emptyError, _jsonOptions));
                return errorResponse;
            }

            // Try to get token from Authorization header first
            var authHeader = req.Headers.GetValues("Authorization")?.FirstOrDefault();
            string? token = null;
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring(7);
            }

            // Create a validation request component
            var validationRequest = new TokenValidation
            {
                // Store the token in Claims for processing
                Claims = new Dictionary<string, string> { { "token", token ?? string.Empty } }
            };

            // Create entity for the validation request
            var validationEntityId = Guid.NewGuid();
            validationRequest.OwnerEntityId = validationEntityId;
            await _dataContext.Commit(validationRequest);

            // Get entity-bound handler and validate
            var validationHandler = _dataContext.For<TokenValidationHandler>(validationEntityId);
            var validationResult = await validationHandler.ValidateToken();

            // Return the result
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(validationResult, _jsonOptions));

            if (validationResult.IsValid)
            {
                _logger.LogInformation("Token validated successfully");
            }
            else
            {
                _logger.LogWarning("Token validation failed: {Reason}", validationResult.ErrorMessage);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in validate function");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "An error occurred during token validation");
        }
    }

}