using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Systems;
using core.jarvis.api.Extensions;
using core.jarvis.api.Exceptions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Azure Function for user authentication.
/// </summary>
public class AuthFunction
{
    private readonly AuthSystem _authSystem;
    private readonly ILogger<AuthFunction> _logger;

    public AuthFunction(AuthSystem authSystem, ILogger<AuthFunction> logger)
    {
        _authSystem = authSystem;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/security/auth
    /// Authenticates a user and returns tokens.
    /// </summary>
    [Function("auth")]
    [OpenApiOperation(operationId: "authenticate", tags: new[] { "Security" }, Summary = "Authenticate user", Description = "Authenticates a user with email and password, returning JWT tokens for API access.")]
    [OpenApiRequestBody("application/json", typeof(Account), Required = true, Description = "Authentication credentials as an IComponent object")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthToken), Summary = "Authentication successful", Description = "Returns JWT access token, refresh token, and session information")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Authentication failed", Description = "Invalid credentials provided")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Bad request", Description = "Request validation failed")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/auth")] HttpRequestData req)
    {
        try
        {
            // Reject XML content type to prevent XXE attacks
            if (req.HasXmlContentType())
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "XML content type not allowed");
            }
            
            // Parse and validate request body
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is required");
            }

            // Deserialize to Account component
            Account accountCredentials;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                accountCredentials = System.Text.Json.JsonSerializer.Deserialize<Account>(requestBody, options) 
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning("Invalid JSON in auth request: {Message}", ex.Message);
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            // Basic validation
            if (string.IsNullOrWhiteSpace(accountCredentials.Email))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Email is required");
            }
            if (string.IsNullOrWhiteSpace(accountCredentials.Password))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Password is required");
            }

            // Enrich component with request metadata
            accountCredentials = accountCredentials with
            {
                IpAddress = req.GetClientIpAddress(),
                UserAgent = req.GetUserAgent()
            };

            // Delegate to system with proper component
            var authToken = await _authSystem.AuthenticateUser(accountCredentials);

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(authToken));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Authentication validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (UnauthorizedException)
        {
            return await req.CreateUnauthorizedResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in auth function");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "An error occurred during authentication");
        }
    }

    /// <summary>
    /// POST /api/security/refresh
    /// Refreshes authentication tokens.
    /// </summary>
    [Function("authRefresh")]
    [OpenApiOperation(operationId: "refreshToken", tags: new[] { "Security" }, Summary = "Refresh authentication token", Description = "Refreshes authentication tokens using a valid refresh token.")]
    [OpenApiRequestBody("application/json", typeof(RefreshTokenRequest), Required = true, Description = "Refresh token request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthToken), Summary = "Token refreshed successfully", Description = "Returns new JWT access token and refresh token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Refresh failed", Description = "Invalid or expired refresh token")]
    public async Task<HttpResponseData> RefreshToken(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/refresh")] HttpRequestData req)
    {
        try
        {
            // Parse request
            var requestBody = await req.ReadAsStringAsync();
            var refreshRequest = JsonConvert.DeserializeObject<RefreshTokenRequest>(requestBody);
            
            if (refreshRequest == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request body");
            }

            // Delegate to system
            var newToken = await _authSystem.RefreshToken(refreshRequest.RefreshToken);

            // Return new tokens
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(newToken));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Token refresh validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (UnauthorizedException)
        {
            return await req.CreateUnauthorizedResponse("Invalid refresh token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to refresh token");
        }
    }

    /// <summary>
    /// POST /api/security/validate
    /// Validates an authentication token.
    /// </summary>
    [Function("authValidate")]
    [OpenApiOperation(operationId: "validateToken", tags: new[] { "Security" }, Summary = "Validate authentication token", Description = "Validates that an authentication token is valid and not expired.")]
    [OpenApiRequestBody("application/json", typeof(ValidateTokenRequest), Required = true, Description = "Token validation request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TokenValidationResult), Summary = "Token is valid", Description = "Returns token validation information")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Token is invalid", Description = "Token is expired, revoked, or invalid")]
    public async Task<HttpResponseData> ValidateToken(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/validate")] HttpRequestData req)
    {
        try
        {
            // Parse request
            var requestBody = await req.ReadAsStringAsync();
            var validateRequest = JsonConvert.DeserializeObject<ValidateTokenRequest>(requestBody);
            
            if (validateRequest == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request body");
            }

            // Delegate to system
            var result = await _authSystem.ValidateToken(validateRequest.Token);

            // Return result
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(result));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Token validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (UnauthorizedException)
        {
            return await req.CreateUnauthorizedResponse("Invalid token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Token validation failed");
        }
    }

}