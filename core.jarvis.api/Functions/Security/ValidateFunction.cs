using System.Net;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Security.Claims;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Azure Function for validating authentication tokens.
/// </summary>
public class ValidateFunction
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<ValidateFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ValidateFunction(ITokenService tokenService, ILogger<ValidateFunction> logger)
    {
        _tokenService = tokenService;
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
            // Try to get token from Authorization header first
            string? authHeader = null;
            try
            {
                authHeader = req.Headers.GetValues("Authorization")?.FirstOrDefault();
            }
            catch
            {
                // Header doesn't exist
            }
            
            string? token = null;
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring(7);
                _logger.LogDebug("Extracted token from Authorization header: {TokenLength} chars", token?.Length ?? 0);
            }

            // If no authorization header, check request body
            if (string.IsNullOrEmpty(token))
            {
                var requestBody = await req.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(requestBody))
                {
                    try
                    {
                        var tokenRequest = JsonSerializer.Deserialize<TokenValidation>(requestBody, _jsonOptions);
                        if (tokenRequest?.Claims?.TryGetValue("token", out var bodyToken) == true)
                        {
                            token = bodyToken;
                        }
                    }
                    catch (JsonException)
                    {
                        // Invalid JSON, but we'll continue without a token
                    }
                }
            }

            // If still no token, return Unauthorized
            if (string.IsNullOrEmpty(token))
            {
                var emptyError = new Error
                {
                    Code = "AUTH_REQUIRED",
                    Message = "Authentication required",
                    StatusCode = 401
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(emptyError, _jsonOptions));
                return errorResponse;
            }

            // Validate the token using TokenService
            var principal = _tokenService.ValidateToken(token);
            
            if (principal == null)
            {
                // Token is invalid
                var invalidError = new Error
                {
                    Code = "INVALID_TOKEN",
                    Message = "Invalid or expired token",
                    StatusCode = 401
                };
                var invalidResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                invalidResponse.Headers.Add("Content-Type", "application/json");
                await invalidResponse.WriteStringAsync(JsonSerializer.Serialize(invalidError, _jsonOptions));
                _logger.LogWarning("Token validation failed");
                return invalidResponse;
            }

            // Token is valid - extract claims
            var validationResult = new TokenValidation
            {
                IsValid = true,
                Claims = new Dictionary<string, string>()
            };

            // Extract standard claims
            foreach (var claim in principal.Claims)
            {
                validationResult.Claims[claim.Type] = claim.Value;
            }

            // Add specific extracted values for convenience
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var roles = principal.FindFirst("roles")?.Value;

            if (userId != null) validationResult.Claims["userId"] = userId;
            if (email != null) validationResult.Claims["email"] = email;
            if (roles != null) validationResult.Claims["roles"] = roles;

            // Return success
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(validationResult, _jsonOptions));

            _logger.LogInformation("Token validated successfully for user {UserId}", userId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in validate function");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            var error = new Error
            {
                Code = "SERVER_ERROR",
                Message = "An error occurred during token validation",
                StatusCode = 500
            };
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(error, _jsonOptions));
            return errorResponse;
        }
    }

}