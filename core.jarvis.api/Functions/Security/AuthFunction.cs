using System.Net;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Systems;
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
    private readonly ISystem _system;
    private readonly ILogger<AuthFunction> _logger;

    public AuthFunction(ISystem system, ILogger<AuthFunction> logger)
    {
        _system = system;
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
            // Check content type - reject XML to prevent XXE attacks
            if (req.Headers.TryGetValues("Content-Type", out var contentTypes))
            {
                var contentType = contentTypes.FirstOrDefault()?.ToLower();
                if (contentType != null && (contentType.Contains("xml") || contentType.Contains("text/xml")))
                {
                    var xmlError = ErrorResponseService.CreateError("INVALID_CONTENT_TYPE");
                    var xmlErrorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    xmlErrorResponse.Headers.Add("Content-Type", "application/json");
                    await xmlErrorResponse.WriteStringAsync(JsonConvert.SerializeObject(xmlError));
                    return xmlErrorResponse;
                }
            }
            
            // Extract request data
            var requestBody = await req.ReadAsStringAsync();
            _logger.LogInformation("Auth request body: {Body}", requestBody);
            
            // Extract headers
            string? ipAddress = null;
            string? userAgent = null;

            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            }
            else if (req.Headers.TryGetValues("X-Real-IP", out var realIp))
            {
                ipAddress = realIp.FirstOrDefault();
            }
            else if (req.Headers.TryGetValues("REMOTE_ADDR", out var remoteAddr))
            {
                ipAddress = remoteAddr.FirstOrDefault();
            }

            if (req.Headers.TryGetValues("User-Agent", out var userAgentValues))
            {
                userAgent = userAgentValues.FirstOrDefault();
            }

            // Execute authentication through handler
            var authEntityId = Guid.NewGuid();
            var authToken = await _system.ExecuteHandler<AuthHandler, AuthToken>(
                authEntityId,
                handler => handler.AuthenticateFromJson(requestBody, ipAddress, userAgent));

            // Check result
            if (authToken == null || string.IsNullOrEmpty(authToken.AccessToken) || authToken.OwnerEntityId == Guid.Empty)
            {
                var error = ErrorResponseService.CreateAuthenticationError();
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonConvert.SerializeObject(error));
                return errorResponse;
            }

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(authToken));

            _logger.LogInformation("User authenticated successfully: {EntityId}", authToken.OwnerEntityId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in auth function");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred during authentication");
        }
    }

    /// <summary>
    /// POST /api/security/refresh
    /// Refreshes authentication tokens.
    /// </summary>
    [Function("refresh")]
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
            
            if (refreshRequest == null || string.IsNullOrEmpty(refreshRequest.RefreshToken))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Refresh token is required");
            }

            // Execute token refresh through handler
            var authEntityId = Guid.NewGuid();
            var newToken = await _system.ExecuteHandler<AuthHandler, AuthToken>(
                authEntityId,
                handler => handler.RefreshToken(refreshRequest.RefreshToken));

            if (newToken == null || string.IsNullOrEmpty(newToken.AccessToken))
            {
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid refresh token");
            }

            // Return new tokens
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(newToken));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Failed to refresh token");
        }
    }

    /// <summary>
    /// POST /api/security/validate
    /// Validates an authentication token.
    /// </summary>
    [Function("validate")]
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
            
            if (validateRequest == null || string.IsNullOrEmpty(validateRequest.Token))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Token is required");
            }

            // Validate token
            var tokenService = req.FunctionContext.InstanceServices.GetService(typeof(ITokenService)) as ITokenService;
            if (tokenService == null)
            {
                _logger.LogError("Token service not available");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Token service not available");
            }

            var principal = tokenService.ValidateToken(validateRequest.Token);
            if (principal == null)
            {
                _logger.LogWarning("Token validation failed");
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid token");
            }

            // Extract claims
            var userId = principal.FindFirst("sub")?.Value;
            var email = principal.FindFirst("email")?.Value;
            var roles = principal.FindFirst("roles")?.Value;

            var result = new TokenValidationResult
            {
                IsValid = true,
                UserId = userId,
                Email = email,
                Roles = roles?.Split(',').ToList() ?? new List<string>()
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(result));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Token validation failed");
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var error = new { error = message };
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonConvert.SerializeObject(error));
        return response;
    }
}

/// <summary>
/// Refresh token request model.
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Validate token request model.
/// </summary>
public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Token validation result model.
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
}