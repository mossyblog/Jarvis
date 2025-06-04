using System.Linq.Expressions;

namespace core.jarvis.Data.Query;

/// <summary>
/// Non-generic interface for component query handlers that enables type-safe querying
/// without runtime reflection.
/// </summary>
public interface IComponentQueryHandler
{
    /// <summary>
    /// Gets the type of component this handler queries.
    /// </summary>
    Type ComponentType { get; }

    /// <summary>
    /// Queries for entity IDs that have components matching the given filter.
    /// </summary>
    /// <param name="filter">The filter expression to apply.</param>
    /// <returns>Entity IDs that match the filter.</returns>
    Task<IEnumerable<Guid>> QueryEntityIds(LambdaExpression filter);

    /// <summary>
    /// Loads all components for the given entity IDs.
    /// </summary>
    /// <param name="entityIds">The entity IDs to load components for.</param>
    /// <returns>Components for the specified entities.</returns>
    Task<IEnumerable<IComponent>> LoadComponents(IEnumerable<Guid> entityIds);
}

/// <summary>
/// Generic interface for component query handlers that provides type-safe operations.
/// </summary>
/// <typeparam name="T">The component type this handler manages.</typeparam>
public interface IComponentQueryHandler<T> : IComponentQueryHandler 
    where T : class, IComponent, new()
{
    /// <summary>
    /// Queries for entity IDs that have components matching the given filter.
    /// </summary>
    /// <param name="filter">The filter expression to apply.</param>
    /// <returns>Entity IDs that match the filter.</returns>
    Task<IEnumerable<Guid>> QueryEntityIds(Expression<Func<T, bool>> filter);

    /// <summary>
    /// Loads all components for the given entity IDs.
    /// </summary>
    /// <param name="entityIds">The entity IDs to load components for.</param>
    /// <returns>Typed components for the specified entities.</returns>
    new Task<IEnumerable<T>> LoadComponents(IEnumerable<Guid> entityIds);
}