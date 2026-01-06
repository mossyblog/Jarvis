using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using core.jarvis.api.Handlers;
using core.jarvis.api.Models;
using core.jarvis.Data;
using Newtonsoft.Json.Serialization;

namespace core.jarvis.api.Functions.Security;

/// <summary>
/// Azure Function for user deauthentication (logout).
/// </summary>
public class DeauthFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<DeauthFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DeauthFunction(IDataContext dataContext, ILogger<DeauthFunction> logger)
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
    /// POST /api/security/deauth
    /// Deauthenticates a session.
    /// </summary>
    [Function("deauth")]
    [OpenApiOperation(operationId: "deauthenticate", tags: new[] { "Security" }, Summary = "Deauthenticate session", Description = "Revokes a user session by session ID.")]
    [OpenApiRequestBody("application/json", typeof(AuthToken), Required = true, Description = "AuthToken component with SessionId to deauthenticate")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthToken), Summary = "Deauthentication result", Description = "Returns AuthToken component with deauthentication status")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Error), Summary = "Session not found", Description = "Session ID not found or already revoked")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Error), Summary = "Bad request", Description = "Invalid request format")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(Error), Summary = "Unauthorized", Description = "Authentication required")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "security/deauth")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Parse request body as Auth component
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

            AuthToken? deauthRequest;
            try
            {
                deauthRequest = JsonSerializer.Deserialize<AuthToken>(requestBody, _jsonOptions);
            }
            catch (JsonException)
            {
                var parseError = new Error
                {
                    Code = "INVALID_JSON",
                    Message = "Invalid JSON format",
                    StatusCode = 400
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(parseError, _jsonOptions));
                return errorResponse;
            }

            if (deauthRequest == null || deauthRequest.SessionId == Guid.Empty)
            {
                var invalidError = new Error
                {
                    Code = "INVALID_SESSION",
                    Message = "Valid SessionId is required",
                    StatusCode = 400
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(invalidError, _jsonOptions));
                return errorResponse;
            }

            // Find tokens by SessionId and revoke them using handler
            var tokenEntities = await _dataContext.Query()
                .WithAll<AuthToken>(t => t.SessionId == deauthRequest.SessionId)
                .ToList();

            if (!tokenEntities.Any())
            {
                var notFoundError = new Error
                {
                    Code = "SESSION_NOT_FOUND",
                    Message = "Session not found or already revoked",
                    StatusCode = 404
                };
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                notFoundResponse.Headers.Add("Content-Type", "application/json");
                await notFoundResponse.WriteStringAsync(JsonSerializer.Serialize(notFoundError, _jsonOptions));
                return notFoundResponse;
            }

            // Use handler to deauthenticate - handler owns ALL data operations
            var tokenEntity = tokenEntities.First();
            var authTokenHandler = _dataContext.For<AuthTokenHandler>(tokenEntity.Id);
            var result = await authTokenHandler.Deauthenticate();

            // Return the result component
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result, _jsonOptions));

            _logger.LogInformation("Session deauthentication processed: {SessionId}", deauthRequest.SessionId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in deauth function");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "An error occurred during deauthentication");
        }
    }

}

/// <summary>
/// Extension method for creating error responses.
/// </summary>
public static partial class HttpRequestDataExtensions
{
    public static async Task<HttpResponseData> CreateErrorResponse(
        this HttpRequestData request,
        HttpStatusCode statusCode,
        string message,
        string code = "ERROR")
    {
        var error = new Error
        {
            Code = code,
            Message = message,
            StatusCode = (int)statusCode
        };
        
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(error, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
        return response;
    }
}

/// <summary>
/// Example for deauth request in OpenAPI documentation.
/// </summary>
public class DeauthRequestExample : OpenApiExample<AuthToken>
{
    public override IOpenApiExample<AuthToken> Build(NamingStrategy? namingStrategy = null)
    {
        Examples.Add(OpenApiExampleResolver.Resolve("Deauth Request", new AuthToken 
        { 
            SessionId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000")
        }, namingStrategy));
        return this;
    }
}