using FastEndpoints;
using core.jarvis.api.Security;
using core.jarvis.Data;
using core.jarvis.Data.GraphQL;

namespace core.jarvis.api.Features.GraphQL;

public class GraphQLRequest
{
    public string Query { get; set; } = string.Empty;
    public object? Variables { get; set; }
    public string? OperationName { get; set; }
}

public class Endpoint : Endpoint<GraphQLRequest, GraphQLResult>
{
    public IPgClient PgClient { get; set; } = null!;
    public IGraphQLQueryValidator QueryValidator { get; set; } = null!;

    public override void Configure()
    {
        Post("/graphql");
        Description(d => d
            .WithTags("GraphQL")
            .Produces<GraphQLResult>(200)
            .Produces(400)
            .Produces(401));
    }

    public override async Task HandleAsync(GraphQLRequest req, CancellationToken ct)
    {
        // Check authentication FIRST before any query processing
        var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var jwt = authHeader.Substring(7);

        // Validate query against security limits
        var validationResult = QueryValidator.Validate(req.Query);
        if (!validationResult.IsValid)
        {
            Logger.LogWarning("GraphQL validation failed: {ErrorCode} - {Message}",
                validationResult.ErrorCode, validationResult.ErrorMessage);

            await SendAsync(new GraphQLResult
            {
                Errors = new List<GraphQLError>
                {
                    new GraphQLError
                    {
                        Message = validationResult.ErrorMessage ?? "Query validation failed",
                        Extensions = new Dictionary<string, object>
                        {
                            ["code"] = validationResult.ErrorCode ?? "VALIDATION_ERROR"
                        }
                    }
                }
            }, 400, ct);
            return;
        }

        try
        {
            // Set JWT on PgClient for RLS
            PgClient.JWT(jwt);

            // GraphQL execution is handled by the data context query system
            // The actual GraphQL endpoint should proxy to pg_graphql or similar
            // For now, return a placeholder indicating GraphQL is not yet fully implemented
            await SendAsync(new GraphQLResult
            {
                Data = new { message = "GraphQL queries should use the component query system" }
            }, cancellation: ct);
        }
        catch (core.jarvis.Exceptions.UnauthorizedException)
        {
            await SendUnauthorizedAsync(ct);
        }
        catch (core.jarvis.Exceptions.GraphQLException ex)
        {
            Logger.LogWarning(ex, "GraphQL execution error");
            await SendAsync(new GraphQLResult
            {
                Errors = ex.GraphQLErrors ?? new List<GraphQLError>
                {
                    new GraphQLError { Message = ex.Message }
                }
            }, 400, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error executing GraphQL query");
            await SendAsync(new GraphQLResult
            {
                Errors = new List<GraphQLError>
                {
                    new GraphQLError { Message = "An error occurred processing the request" }
                }
            }, 500, ct);
        }
    }
}
