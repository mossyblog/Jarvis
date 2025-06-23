using System.Net;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.Data;
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
    [OpenApiRequestBody("application/json", typeof(User), Required = true, Description = "Authentication credentials as an IComponent object")]
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
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            User? userRequest;
            try
            {
                userRequest = JsonConvert.DeserializeObject<User>(requestBody);
            }
            catch (JsonException)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            if (userRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(userRequest.Email) || string.IsNullOrEmpty(userRequest.Password))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Email and password are required");
            }

            // Get client info
            string? ipAddress = null;
            string? userAgent = null;

            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.FirstOrDefault();
            }
            else if (req.Headers.TryGetValues("REMOTE_ADDR", out var remoteAddr))
            {
                ipAddress = remoteAddr.FirstOrDefault();
            }

            if (req.Headers.TryGetValues("User-Agent", out var userAgentValues))
            {
                userAgent = userAgentValues.FirstOrDefault();
            }

            // Ultra-thin function: create entity, get handler, call single method
            var authEntityId = Guid.NewGuid();
            var authHandler = _dataContext.For<AuthHandler>(authEntityId);
            var authToken = await authHandler.Authenticate(userRequest);

            // Check if authentication succeeded using handler method
            if (!authHandler.IsAuthenticated(authToken))
            {
                var error = new Error
                {
                    OwnerEntityId = Guid.NewGuid(),
                    Code = "AUTH_FAILED",
                    Message = "Invalid credentials",
                    StatusCode = 401
                };
                
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