using System.Net;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Azure Function for user authentication.
/// </summary>
public class AuthFunction
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthFunction> _logger;

    public AuthFunction(IAuthenticationService authService, ILogger<AuthFunction> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/security/auth
    /// Authenticates a user and returns tokens.
    /// </summary>
    [Function("auth")]
    [OpenApiOperation(operationId: "authenticate", tags: new[] { "Security" }, Summary = "Authenticate user", Description = "Authenticates a user with email and password, returning JWT tokens for API access.")]
    [OpenApiRequestBody("application/json", typeof(AuthRequest), Required = true, Description = "Authentication credentials as an IComponent object")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthResponse), Summary = "Authentication successful", Description = "Returns JWT access token, refresh token, and session information")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object), Summary = "Authentication failed", Description = "Invalid credentials provided")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Bad request", Description = "Request validation failed")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/auth")] HttpRequestData req)
    {
        try
        {
            // Parse request body
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            var authRequest = JsonConvert.DeserializeObject<AuthRequest>(requestBody);
            if (authRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(authRequest.Email) || string.IsNullOrEmpty(authRequest.Password))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Email and password are required");
            }

            // Get client info
            var ipAddress = req.Headers.GetValues("X-Forwarded-For")?.FirstOrDefault() 
                         ?? req.Headers.GetValues("REMOTE_ADDR")?.FirstOrDefault();
            var userAgent = req.Headers.GetValues("User-Agent")?.FirstOrDefault();

            // Authenticate
            var authResponse = await _authService.AuthenticateAsync(authRequest, ipAddress, userAgent);
            
            if (authResponse == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid credentials");
            }

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(authResponse));
            
            _logger.LogInformation("User authenticated successfully: {UserId}", authResponse.UserId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in auth function");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred during authentication");
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