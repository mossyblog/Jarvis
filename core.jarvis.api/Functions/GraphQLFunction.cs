using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Middleware;
using core.jarvis.Data;
using core.jarvis.Data.GraphQL;

namespace core.jarvis.api.Functions;

/// <summary>
/// GraphQL bridge function for UI to execute GraphQL queries.
/// This provides a temporary bridge until UI can connect directly to PostgreSQL GraphQL.
/// </summary>
public class GraphQLFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<GraphQLFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GraphQLFunction(
        IDataContext dataContext,
        ILogger<GraphQLFunction> logger)
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
    /// Execute GraphQL queries from the UI.
    /// This is a temporary bridge until the UI can connect directly to PostgreSQL GraphQL.
    /// </summary>
    [Function("ExecuteGraphQL")]
    public async Task<HttpResponseData> ExecuteGraphQL(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "graphql")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            // Get user ID from context for security
            var userIdStr = executionContext.GetUserId();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                var authError = new Error
                {
                    Code = "UNAUTHORIZED",
                    Message = "User not authenticated",
                    StatusCode = 401
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(authError, _jsonOptions));
                return errorResponse;
            }

            // Parse GraphQL request
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                var badRequestError = new Error
                {
                    Code = "INVALID_REQUEST",
                    Message = "Request body is required",
                    StatusCode = 400
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(badRequestError, _jsonOptions));
                return errorResponse;
            }

            GraphQLRequest? graphqlRequest;
            try
            {
                graphqlRequest = JsonSerializer.Deserialize<GraphQLRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                var badRequestError = new Error
                {
                    Code = "INVALID_JSON",
                    Message = $"Invalid JSON: {ex.Message}",
                    StatusCode = 400
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(badRequestError, _jsonOptions));
                return errorResponse;
            }

            if (graphqlRequest == null || string.IsNullOrEmpty(graphqlRequest.Query))
            {
                var badRequestError = new Error
                {
                    Code = "INVALID_GRAPHQL",
                    Message = "GraphQL query is required",
                    StatusCode = 400
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(badRequestError, _jsonOptions));
                return errorResponse;
            }

            // Get JWT token from Authorization header
            string? authHeader = null;
            if (req.Headers.TryGetValues("Authorization", out var authValues))
            {
                authHeader = authValues.FirstOrDefault();
            }
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                var authError = new Error
                {
                    Code = "UNAUTHORIZED",
                    Message = "Authorization header with Bearer token is required",
                    StatusCode = 401
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(authError, _jsonOptions));
                return errorResponse;
            }

            var jwt = authHeader.Substring("Bearer ".Length);

            // Execute GraphQL query using the existing infrastructure
            var graphqlQuery = _dataContext.GraphQL(graphqlRequest.Query);
            
            // Add JWT authentication
            var result = await graphqlQuery
                .WithAuth(jwt)
                .WithVariables(graphqlRequest.Variables ?? new object())
                .Execute<object>();

            // Return GraphQL result
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new { data = result }, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GraphQL query: {Message}", ex.Message);
            var serverError = new Error
            {
                Code = "GRAPHQL_ERROR", 
                Message = $"GraphQL query execution failed: {ex.Message}",
                StatusCode = 500
            };
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { 
                errors = new[] { serverError } 
            }, _jsonOptions));
            return errorResponse;
        }
    }
}

/// <summary>
/// GraphQL request model for the API bridge
/// </summary>
public class GraphQLRequest
{
    public string Query { get; set; } = string.Empty;
    public object? Variables { get; set; }
    public string? OperationName { get; set; }
}