using System.Text.Json;
using core.jarvis.Exceptions;
using core.jarvis.Security;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Data.GraphQL
{
    /// <summary>
    /// Base implementation for component-specific GraphQL operations
    /// </summary>
    public abstract class GraphQLHandler<TComponent> : ComponentHandler<TComponent>, IGraphQLHandler<TComponent>
        where TComponent : class, IComponent, new()
    {
        private readonly IPgClient _pgClient;
        private readonly IAuditService? _auditService;
        private readonly IJwtValidator? _jwtValidator;

        protected GraphQLHandler(
            IDataContext dataContext,
            IPgClient pgClient,
            ILogger logger,
            IAuditService? auditService = null,
            IJwtValidator? jwtValidator = null) : base(dataContext, logger)
        {
            _pgClient = pgClient ?? throw new ArgumentNullException(nameof(pgClient));
            _auditService = auditService;
            _jwtValidator = jwtValidator;
        }

        public async Task<T> Query<T>(string query, object? variables = null)
        {
            ValidateAuth();
            ValidateEntityContext();

            // Inject entity ID into variables if not present
            var enhancedVariables = InjectEntityId(variables);

            // Debug logging removed - use structured logging context instead

            // Use the query builder for consistent auth handling
            var result = await CreateQueryBuilder(query)
                .WithVariables(enhancedVariables)
                .Execute<T>();

            return result;
        }

        public async Task<GraphQLResult> Mutate(string mutation, object variables)
        {
            ValidateAuth();
            ValidateEntityContext();

            var enhancedVariables = InjectEntityId(variables);

            // Audit mutation attempts
            if (_auditService != null)
            {
                await _auditService.LogEvent("GRAPHQL_MUTATION", OwnerEntityId, new
                {
                    mutation,
                    variables = enhancedVariables,
                    userId = GetCurrentUserId()
                });
            }

            return await CreateQueryBuilder(mutation)
                .WithVariables(enhancedVariables)
                .Execute();
        }

        public async Task<T> GetComponents<T>(params string[] fields) where T : class
        {
            var componentType = typeof(T).Name.ToLower();
            var fieldList = fields.Length > 0 ? string.Join(" ", fields) : "id";

            var query = $@"
                query GetComponents($entityId: UUID!) {{
                    {componentType}Collection(filter: {{owner_entity_id: {{eq: $entityId}}}}) {{
                        edges {{
                            node {{
                                {fieldList}
                            }}
                        }}
                    }}
                }}
            ";

            return await Query<T>(query, new { entityId = OwnerEntityId });
        }

        public async Task<TComponent> GetViaGraphQL(params string[] fields)
        {
            var componentType = typeof(TComponent).Name.ToLower();
            var fieldList = fields.Length > 0 ? string.Join(" ", fields) : "id owner_entity_id";

            var query = $@"
                query GetComponent($entityId: UUID!) {{
                    {componentType}Collection(filter: {{owner_entity_id: {{eq: $entityId}}}}) {{
                        edges {{
                            node {{
                                {fieldList}
                            }}
                        }}
                    }}
                }}
            ";

            var result = await Query<dynamic>(query, new { entityId = OwnerEntityId });
            
            // Extract the first node from the collection
            var edges = result?[componentType + "Collection"]?["edges"];
            if (edges?.Count > 0)
            {
                var nodeJson = JsonSerializer.Serialize(edges[0]["node"]);
                return JsonSerializer.Deserialize<TComponent>(nodeJson) 
                    ?? throw new ComponentNotFoundException($"Failed to deserialize {componentType}");
            }

            throw new ComponentNotFoundException($"No {componentType} found for entity {OwnerEntityId}");
        }

        public async Task<GraphQLResult> UpdateViaGraphQL(TComponent component)
        {
            var componentType = typeof(TComponent).Name.ToLower();
            
            var mutation = $@"
                mutation UpdateComponent($id: UUID!, $data: {componentType}UpdateInput!) {{
                    update{componentType}Collection(
                        filter: {{id: {{eq: $id}}}},
                        set: $data
                    ) {{
                        affectedCount
                        records {{
                            id
                        }}
                    }}
                }}
            ";

            // Create update data excluding system fields
            var updateData = new Dictionary<string, object>();
            foreach (var prop in typeof(TComponent).GetProperties())
            {
                if (prop.Name != "Id" && prop.Name != "CreatedAt" && prop.CanRead)
                {
                    var value = prop.GetValue(component);
                    if (value != null)
                    {
                        updateData[prop.Name.ToLower()] = value;
                    }
                }
            }

            return await Mutate(mutation, new { id = component.Id, data = updateData });
        }

        public async Task<T> ExecuteNamedQuery<T>(string queryName, object? parameters = null)
        {
            // This could be extended to load queries from a query registry
            var query = GetNamedQuery(queryName);
            return await Query<T>(query, parameters);
        }

        protected virtual string GetNamedQuery(string queryName)
        {
            // Override in derived classes to provide named queries
            throw new NotImplementedException($"Named query '{queryName}' not implemented");
        }

        private void ValidateAuth()
        {
            if (!_pgClient.HasValidJWT())
            {
                throw new UnauthorizedException("GraphQL operations require authentication");
            }
        }

        private void ValidateEntityContext()
        {
            if (OwnerEntityId == Guid.Empty)
            {
                throw new InvalidOperationException("Entity context not initialized. Call InitializeContext first.");
            }
        }

        private object InjectEntityId(object? variables)
        {
            if (variables == null)
            {
                return new { entityId = OwnerEntityId };
            }

            // Convert to dictionary to add entityId
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(variables)) ?? new();
            
            dict["entityId"] = OwnerEntityId;
            return dict;
        }

        private string GetCurrentUserId()
        {
            var claims = _pgClient.GetJWTClaims();
            return claims?.GetValueOrDefault("sub", "unknown") ?? "unknown";
        }

        private IGraphQLQuery CreateQueryBuilder(string query)
        {
            var jwt = _pgClient.GetJWT()
                ?? throw new UnauthorizedException("No JWT token available");

            return new GraphQLQueryBuilder(_pgClient,
                Logger as ILogger<GraphQLQueryBuilder> ?? throw new InvalidOperationException("Logger type mismatch"),
                _auditService,
                query,
                _jwtValidator)
                .WithAuth(jwt);
        }
    }
}