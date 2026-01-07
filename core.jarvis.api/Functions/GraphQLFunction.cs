using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Middleware;
using core.jarvis.api.Security;
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
    private readonly IGraphQLQueryValidator _queryValidator;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly bool _isProduction;

    public GraphQLFunction(
        IDataContext dataContext,
        ILogger<GraphQLFunction> logger,
        IGraphQLQueryValidator? queryValidator = null)
    {
        _dataContext = dataContext;
        _logger = logger;
        _queryValidator = queryValidator ?? new GraphQLQueryValidator();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        _isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development"
                     && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Test";
    }

    /// <summary>
    /// Gets a production-safe error message that doesn't leak implementation details.
    /// </summary>
    private string GetSafeErrorMessage(Exception ex)
    {
        // In development/test, show detailed errors for debugging
        if (!_isProduction)
            return ex.Message;

        // In production, return generic messages to prevent information disclosure
        return ex switch
        {
            JsonException => "Invalid request format",
            UnauthorizedAccessException => "Access denied",
            ArgumentException => "Invalid request parameters",
            InvalidOperationException => "Operation could not be completed",
            _ => "An error occurred processing your request"
        };
    }

    /// <summary>
    /// Execute GraphQL queries from the UI.
    /// This is a temporary bridge until the UI can connect directly to PostgreSQL GraphQL.
    /// No explicit permission required - any authenticated user can execute GraphQL queries.
    /// Row-level security is enforced by the database via JWT claims.
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
                _logger.LogWarning(ex, "Invalid JSON in GraphQL request");
                var badRequestError = new Error
                {
                    Code = "INVALID_JSON",
                    Message = GetSafeErrorMessage(ex),
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

            // Validate GraphQL query against security limits (depth, field count, introspection)
            var validationResult = _queryValidator.Validate(graphqlRequest.Query);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("GraphQL query validation failed: {ErrorCode} - {ErrorMessage}",
                    validationResult.ErrorCode, validationResult.ErrorMessage);

                var validationError = new Error
                {
                    Code = validationResult.ErrorCode ?? "QUERY_VALIDATION_FAILED",
                    Message = validationResult.ErrorMessage ?? "Query validation failed",
                    StatusCode = 400
                };
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(validationError, _jsonOptions));
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
            // Always log the full error details server-side
            _logger.LogError(ex, "Error executing GraphQL query: {Message}", ex.Message);

            // Return production-safe error message to client
            var serverError = new Error
            {
                Code = "GRAPHQL_ERROR",
                Message = GetSafeErrorMessage(ex),
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