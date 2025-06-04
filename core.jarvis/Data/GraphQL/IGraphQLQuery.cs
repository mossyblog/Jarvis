using System.Text.Json.Serialization;

namespace core.jarvis.Data.GraphQL
{
    /// <summary>
    /// Fluent interface for building and executing GraphQL queries with authentication
    /// </summary>
    public interface IGraphQLQuery
    {
        /// <summary>
        /// Sets the JWT token for authentication. Required before execution.
        /// </summary>
        IGraphQLQuery WithAuth(string jwt);
        
        /// <summary>
        /// Adds variables to the GraphQL query
        /// </summary>
        IGraphQLQuery WithVariables(object variables);
        
        /// <summary>
        /// Adds operation name for the GraphQL query
        /// </summary>
        IGraphQLQuery WithOperationName(string operationName);
        
        /// <summary>
        /// Requires a specific role for execution
        /// </summary>
        IGraphQLQuery RequireRole(string role);
        
        /// <summary>
        /// Requires specific claim values
        /// </summary>
        IGraphQLQuery RequireClaim(string claimType, string claimValue);
        
        /// <summary>
        /// Adds custom validation before execution
        /// </summary>
        IGraphQLQuery ValidateWith(Func<GraphQLContext, Task<bool>> validator);
        
        /// <summary>
        /// Executes the query and returns typed result
        /// </summary>
        Task<T> Execute<T>();
        
        /// <summary>
        /// Executes the query and returns raw GraphQL result
        /// </summary>
        Task<GraphQLResult> Execute();
    }
    
    /// <summary>
    /// Context passed to validators
    /// </summary>
    public class GraphQLContext
    {
        public string Query { get; set; } = "";
        public Dictionary<string, string> JWTClaims { get; set; } = new();
        public object? Variables { get; set; }
        public string? OperationName { get; set; }
    }
    
    /// <summary>
    /// GraphQL execution result
    /// </summary>
    public class GraphQLResult
    {
        [JsonPropertyName("data")]
        public object? Data { get; set; }
        
        [JsonPropertyName("errors")]
        public List<GraphQLError>? Errors { get; set; }
        
        [JsonPropertyName("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
    }
    
    /// <summary>
    /// GraphQL error details
    /// </summary>
    public class GraphQLError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
        
        [JsonPropertyName("path")]
        public List<object>? Path { get; set; }
        
        [JsonPropertyName("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }
    }
}