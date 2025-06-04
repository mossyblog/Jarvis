namespace core.jarvis.Data.GraphQL
{
    /// <summary>
    /// Component-specific GraphQL handler for executing queries related to a specific entity
    /// </summary>
    public interface IGraphQLHandler : IComponentHandler
    {
        /// <summary>
        /// Executes a GraphQL query scoped to the component's entity
        /// </summary>
        Task<T> Query<T>(string query, object? variables = null);
        
        /// <summary>
        /// Executes a GraphQL mutation scoped to the component's entity
        /// </summary>
        Task<GraphQLResult> Mutate(string mutation, object variables);
        
        /// <summary>
        /// Gets all components of a specific type for this entity via GraphQL
        /// </summary>
        Task<T> GetComponents<T>(params string[] fields) where T : class;
        
        /// <summary>
        /// Executes a pre-defined named query
        /// </summary>
        Task<T> ExecuteNamedQuery<T>(string queryName, object? parameters = null);
    }
    
    /// <summary>
    /// Base implementation for component-specific GraphQL operations
    /// </summary>
    public interface IGraphQLHandler<TComponent> : IGraphQLHandler, IComponentHandler<TComponent>
        where TComponent : class, IComponent, new()
    {
        /// <summary>
        /// Gets the component data via GraphQL with specified fields
        /// </summary>
        Task<TComponent> GetViaGraphQL(params string[] fields);
        
        /// <summary>
        /// Updates the component via GraphQL mutation
        /// </summary>
        Task<GraphQLResult> UpdateViaGraphQL(TComponent component);
    }
}