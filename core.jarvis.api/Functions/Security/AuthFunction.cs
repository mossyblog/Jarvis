using System.Net;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Services;
using core.jarvis.Data;
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
    private readonly IDataContext _dataContext;
    private readonly ILogger<AuthFunction> _logger;

    public AuthFunction(IDataContext dataContext, ILogger<AuthFunction> logger)
    {
        _dataContext = dataContext;
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
            // Parse request body
            var requestBody = await req.ReadAsStringAsync();
            _logger.LogInformation("Auth request body: {Body}", requestBody);
            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogInformation("Auth request body is empty");

                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            Account? accountRequest;
            try
            {
                // First try with default (PascalCase) deserialization
                accountRequest = JsonConvert.DeserializeObject<Account>(requestBody);
            }
            catch (JsonException)
            {
                try
                {
                    // If that fails, try with camelCase
                    var camelCaseSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };
                    accountRequest = JsonConvert.DeserializeObject<Account>(requestBody, camelCaseSettings);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("JSON deserialization failed: {Message}. Request body: {Body}", ex.Message, requestBody);
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
                }
            }

            if (accountRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(accountRequest.Email) || string.IsNullOrEmpty(accountRequest.Password))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Email and password are required");
            }

            // Get client info
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

            // Add IP and User-Agent to the request
            accountRequest = accountRequest with 
            { 
                IpAddress = ipAddress ?? "unknown",
                UserAgent = userAgent
            };

            // Ultra-thin function: create entity, get handler, call single method
            var authEntityId = Guid.NewGuid();
            var authHandler = _dataContext.For<AuthHandler>(authEntityId);
            var authToken = await authHandler.Authenticate(accountRequest);

            // Check if authentication succeeded using handler method
            if (!authHandler.IsAuthenticated(authToken))
            {
                var error = ErrorResponseService.CreateAuthenticationError();
                
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonConvert.SerializeObject(error));
                return errorResponse;
            }

            // Persist the session
            await authHandler.PersistSession(authToken);

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

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        // Use standardized error messages
        var errorCode = statusCode switch
        {
            HttpStatusCode.BadRequest => "BAD_REQUEST",
            HttpStatusCode.Unauthorized => "AUTH_FAILED",
            HttpStatusCode.Forbidden => "ACCESS_DENIED",
            HttpStatusCode.NotFound => "NOT_FOUND",
            HttpStatusCode.TooManyRequests => "RATE_LIMIT_EXCEEDED",
            _ => "SERVER_ERROR"
        };
        
        var error = ErrorResponseService.CreateError(errorCode);
        
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonConvert.SerializeObject(error));
        return response;
    }
}