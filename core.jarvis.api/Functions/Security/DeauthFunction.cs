using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using core.jarvis.api.Services;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Azure Function for user deauthentication (logout).
/// </summary>
public class DeauthFunction
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<DeauthFunction> _logger;

    public DeauthFunction(IAuthenticationService authService, ILogger<DeauthFunction> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/security/deauth
    /// Deauthenticates a session.
    /// </summary>
    [Function("deauth")]
    [OpenApiOperation(operationId: "deauthenticate", tags: new[] { "Security" }, Summary = "Deauthenticate session", Description = "Revokes a user session by session ID.")]
    [OpenApiRequestBody("application/json", typeof(string), Required = true, Description = "Session ID as a GUID string", Example = typeof(DeauthRequestExample))]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Guid), Summary = "Deauthentication successful", Description = "Returns a confirmation GUID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Session not found", Description = "Session ID not found or already revoked")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Bad request", Description = "Invalid GUID format")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "security/deauth")] HttpRequestData req)
    {
        try
        {
            // Parse request body - expecting a GUID
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Request body is required");
            }

            // Try to parse as GUID directly or from JSON
            Guid sessionId;
            if (Guid.TryParse(requestBody.Trim('"'), out sessionId))
            {
                // Direct GUID string
            }
            else
            {
                // Try parsing from JSON object
                try
                {
                    var requestObj = JsonConvert.DeserializeObject<dynamic>(requestBody);
                    if (requestObj?.sessionId != null)
                    {
                        sessionId = Guid.Parse(requestObj.sessionId.ToString());
                    }
                    else
                    {
                        return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format - expected GUID or {sessionId: GUID}");
                    }
                }
                catch
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid GUID format");
                }
            }
            
            // Validate session ID is not empty
            if (sessionId == Guid.Empty)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Session ID cannot be empty");
            }

            // Deauthenticate
            var success = await _authService.DeauthenticateAsync(sessionId);
            
            if (!success)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Session not found or already revoked");
            }

            // Return success response with confirmation GUID
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            var confirmationId = Guid.NewGuid();
            await response.WriteStringAsync(JsonConvert.SerializeObject(confirmationId));
            
            _logger.LogInformation("Session deauthenticated successfully: {SessionId}", sessionId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in deauth function");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred during deauthentication");
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

/// <summary>
/// Example for deauth request in OpenAPI documentation.
/// </summary>
public class DeauthRequestExample : OpenApiExample<string>
{
    public override IOpenApiExample<string> Build(NamingStrategy? namingStrategy = null)
    {
        Examples.Add(OpenApiExampleResolver.Resolve("Session ID GUID", "550e8400-e29b-41d4-a716-446655440000", namingStrategy));
        return this;
    }
}