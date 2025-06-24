using System.Text.Json;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace core.jarvis.Data.GraphQL
{
    /// <summary>
    /// Builder implementation for GraphQL queries with authentication and validation
    /// </summary>
    public class GraphQLQueryBuilder : IGraphQLQuery
    {
        private readonly IPgClient _pgClient;
        private readonly ILogger<GraphQLQueryBuilder> _logger;
        private readonly IAuditService? _auditService;
        private readonly string _query;
        
        private string? _jwt;
        private object? _variables;
        private string? _operationName;
        private string? _requiredRole;
        private readonly Dictionary<string, string> _requiredClaims = new();
        private readonly List<Func<GraphQLContext, Task<bool>>> _validators = new();

        public GraphQLQueryBuilder(
            IPgClient pgClient, 
            ILogger<GraphQLQueryBuilder> logger,
            IAuditService? auditService,
            string query)
        {
            _pgClient = pgClient ?? throw new ArgumentNullException(nameof(pgClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditService = auditService;
            _query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public IGraphQLQuery WithAuth(string jwt)
        {
            _jwt = jwt;
            _pgClient.JWT(jwt);
            return this;
        }

        public IGraphQLQuery WithVariables(object variables)
        {
            _variables = variables;
            return this;
        }

        public IGraphQLQuery WithOperationName(string operationName)
        {
            _operationName = operationName;
            return this;
        }

        public IGraphQLQuery RequireRole(string role)
        {
            _requiredRole = role;
            return this;
        }

        public IGraphQLQuery RequireClaim(string claimType, string claimValue)
        {
            _requiredClaims[claimType] = claimValue;
            return this;
        }

        public IGraphQLQuery ValidateWith(Func<GraphQLContext, Task<bool>> validator)
        {
            _validators.Add(validator);
            return this;
        }

        public async Task<T> Execute<T>()
        {
            var result = await Execute();
            
            if (result.Errors?.Any() == true)
            {
                throw new GraphQLException($"GraphQL errors: {string.Join(", ", result.Errors.Select(e => e.Message))}", result.Errors);
            }

            if (result.Data == null)
            {
                throw new GraphQLException("No data returned from GraphQL query");
            }

            // Deserialize the data portion
            var dataJson = JsonSerializer.Serialize(result.Data);
            return JsonSerializer.Deserialize<T>(dataJson) 
                ?? throw new GraphQLException("Failed to deserialize GraphQL response");
        }

        public async Task<GraphQLResult> Execute()
        {
            // Validate authentication
            if (string.IsNullOrEmpty(_jwt))
            {
                // Audit the unauthorized attempt
                if (_auditService != null)
                {
                    await _auditService.LogEvent(AuditEventTypes.GraphQLUnauthorized, Guid.Empty, new
                    {
                        reason = "Missing JWT token",
                        query = _query
                    });
                }
                throw new UnauthorizedException("GraphQL queries require authentication. Call WithAuth() before Execute()");
            }

            // Parse JWT claims
            var claims = ParseJWTClaims(_jwt);
            
            // Create context for validation
            var context = new GraphQLContext
            {
                Query = _query,
                JWTClaims = claims,
                Variables = _variables,
                OperationName = _operationName
            };

            // Validate role if required
            if (!string.IsNullOrEmpty(_requiredRole))
            {
                if (!claims.TryGetValue("role", out var role) || role != _requiredRole)
                {
                    // Audit the authorization failure
                    if (_auditService != null)
                    {
                        var authUserId = claims.TryGetValue("sub", out var subClaim) ? subClaim : "unknown";
                        await _auditService.LogEvent(AuditEventTypes.AuthorizationFailed, Guid.Empty, new
                        {
                            userId = authUserId,
                            requiredRole = _requiredRole,
                            actualRole = role ?? "none",
                            query = _query
                        });
                    }
                    throw new ForbiddenException($"Role '{_requiredRole}' is required for this query");
                }
            }

            // Validate required claims
            foreach (var (claimType, expectedValue) in _requiredClaims)
            {
                if (!claims.TryGetValue(claimType, out var actualValue) || actualValue != expectedValue)
                {
                    // Audit the authorization failure
                    if (_auditService != null)
                    {
                        var claimUserId = claims.TryGetValue("sub", out var subClaim2) ? subClaim2 : "unknown";
                        await _auditService.LogEvent(AuditEventTypes.AuthorizationFailed, Guid.Empty, new
                        {
                            userId = claimUserId,
                            requiredClaim = claimType,
                            expectedValue,
                            actualValue = actualValue ?? "none",
                            query = _query
                        });
                    }
                    throw new ForbiddenException($"Claim '{claimType}' with value '{expectedValue}' is required");
                }
            }

            // Run custom validators
            foreach (var validator in _validators)
            {
                if (!await validator(context))
                {
                    throw new ForbiddenException("Custom validation failed for GraphQL query");
                }
            }

            // Get user ID for audit
            var userId = claims.TryGetValue("sub", out var userIdClaim) ? userIdClaim : "unknown";

            // Audit if service available
            if (_auditService != null)
            {
                // Determine if this is a query or mutation
                var eventType = _query.TrimStart().StartsWith("mutation", StringComparison.OrdinalIgnoreCase) 
                    ? AuditEventTypes.GraphQLMutation 
                    : AuditEventTypes.GraphQLQuery;
                    
                await _auditService.LogEvent(eventType, Guid.Empty, new
                {
                    userId,
                    query = _query,
                    variables = _variables ?? new { },
                    operationName = _operationName ?? ""
                });
            }

            // Execute the GraphQL query
            return await ExecuteGraphQL();
        }

        private async Task<GraphQLResult> ExecuteGraphQL()
        {
            try
            {
                // Execute via pg_graphql - it expects just the query string
                using var conn = await _pgClient.GetConnectionAsync();
                
                // Try to use the wrapper function first (for test environments where postgres user doesn't have graphql access)
                string? resultJson = null;
                try
                {
                    using var cmd = new NpgsqlCommand("SELECT public.graphql_resolve($1::text)", conn);
                    cmd.Parameters.AddWithValue(_query);
                    resultJson = await cmd.ExecuteScalarAsync() as string;
                }
                catch (PostgresException ex) when (ex.SqlState == "42883") // function does not exist
                {
                    // Fall back to direct graphql.resolve
                }
                
                // Fall back to direct graphql.resolve if wrapper doesn't exist or didn't return a result
                if (resultJson == null)
                {
                    using var cmd2 = new NpgsqlCommand("SELECT graphql.resolve($1::text)", conn);
                    cmd2.Parameters.AddWithValue(_query);
                    resultJson = await cmd2.ExecuteScalarAsync() as string;
                }
                
                if (string.IsNullOrEmpty(resultJson))
                {
                    throw new GraphQLException("Empty response from GraphQL");
                }

                var result = JsonSerializer.Deserialize<GraphQLResult>(resultJson) 
                    ?? throw new GraphQLException("Failed to parse GraphQL response");
                
                // Check for GraphQL errors
                if (result.Errors?.Any() == true)
                {
                    var errorMessages = string.Join(", ", result.Errors.Select(e => e.Message));
                    
                    // Audit the GraphQL errors
                    if (_auditService != null)
                    {
                        await _auditService.LogEvent(AuditEventTypes.GraphQLError, Guid.Empty, new
                        {
                            errors = result.Errors,
                            query = _query,
                            variables = _variables
                        });
                    }
                    
                    throw new GraphQLException($"GraphQL errors: {errorMessages}", result.Errors);
                }
                
                return result;
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "PostgreSQL error executing GraphQL");
                throw new GraphQLException($"Database error: {ex.Message}", innerException: ex);
            }
            catch (Exception ex) when (!(ex is GraphQLException))
            {
                _logger.LogError(ex, "Unexpected error executing GraphQL");
                throw new GraphQLException("Failed to execute GraphQL query", innerException: ex);
            }
        }

        private Dictionary<string, string> ParseJWTClaims(string jwt)
        {
            try
            {
                // This is a simplified version - in production, use proper JWT parsing
                var parts = jwt.Split('.');
                if (parts.Length != 3)
                {
                    return new Dictionary<string, string>();
                }

                var payload = parts[1];
                // Add padding if needed
                var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var bytes = Convert.FromBase64String(padded);
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                
                var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
                
                // Convert to string dictionary
                return claims.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? ""
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse JWT claims");
                return new Dictionary<string, string>();
            }
        }
    }
}