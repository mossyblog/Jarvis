using System.Linq.Expressions;

namespace core.jarvis.Data;

/// <summary>
/// Provides a fluent interface for querying across multiple component types
/// while maintaining ECS principles of component independence.
/// </summary>
public interface IEntityQuery
{
    /// <summary>
    /// Adds an intersection (AND) filter for entities that have a specific component matching the predicate.
    /// Multiple WithAll clauses result in an intersection of entity IDs.
    /// </summary>
    /// <typeparam name="T">The component type to filter on.</typeparam>
    /// <param name="filter">The filter expression to apply.</param>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery WithAll<T>(Expression<Func<T, bool>> filter) where T : class, IComponent, new();
    
    /// <summary>
    /// Adds an intersection (AND) filter for entities that have a specific component type.
    /// No filtering is applied - all entities with this component type are included.
    /// </summary>
    /// <typeparam name="T">The component type to filter on.</typeparam>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery WithAll<T>() where T : class, IComponent, new();

    /// <summary>
    /// Adds a union (OR) filter for entities that have a specific component matching the predicate.
    /// Multiple WithAny clauses result in a union of entity IDs.
    /// </summary>
    /// <typeparam name="T">The component type to filter on.</typeparam>
    /// <param name="filter">The filter expression to apply.</param>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery WithAny<T>(Expression<Func<T, bool>> filter) where T : class, IComponent, new();

    /// <summary>
    /// Adds an exclusion (NOT) filter for entities that have a specific component matching the predicate.
    /// Entities matching this filter will be excluded from results.
    /// </summary>
    /// <typeparam name="T">The component type to filter on.</typeparam>
    /// <param name="filter">The filter expression to apply.</param>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery WithNone<T>(Expression<Func<T, bool>> filter) where T : class, IComponent, new();

    /// <summary>
    /// Adds a filter criterion for entities that have a specific component matching the predicate.
    /// Multiple With clauses result in an intersection (AND) of entity IDs.
    /// </summary>
    /// <typeparam name="T">The component type to filter on.</typeparam>
    /// <param name="filter">The filter expression to apply.</param>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery With<T>(Expression<Func<T, bool>> filter) where T : class, new();

    /// <summary>
    /// Includes a component type in the result set without filtering.
    /// Used to eager load components to avoid N+1 queries.
    /// </summary>
    /// <typeparam name="T">The component type to include.</typeparam>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery Include<T>() where T : class, new();

    /// <summary>
    /// Includes a component type with an optional filter.
    /// The filter only affects which components are loaded, not which entities are returned.
    /// </summary>
    /// <typeparam name="T">The component type to include.</typeparam>
    /// <param name="filter">Optional filter for the included components.</param>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery Include<T>(Expression<Func<T, bool>> filter) where T : class, new();

    /// <summary>
    /// Executes the query and returns only the entity IDs that match all criteria.
    /// </summary>
    /// <returns>A list of entity IDs.</returns>
    Task<List<Guid>> ToEntityIds();

    /// <summary>
    /// Executes the query and returns entities with their components.
    /// Components are eagerly loaded based on With and Include clauses.
    /// </summary>
    /// <returns>A dictionary mapping entity IDs to their components.</returns>
    Task<Dictionary<Guid, EntityComponents>> ToEntityComponents();

    /// <summary>
    /// Executes the query and returns Entity objects with DataContext binding for async component resolution.
    /// This is the preferred method for retrieving entities with full component access.
    /// </summary>
    /// <returns>A list of Entity objects with DataContext binding.</returns>
    Task<List<Entity>> ToList();

    /// <summary>
    /// Orders query results by a component property in ascending order.
    /// Clears any existing ordering and sets this as the primary sort.
    /// </summary>
    /// <typeparam name="T">The component type containing the property to sort by.</typeparam>
    /// <param name="keySelector">Expression selecting the property to sort by.</param>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery OrderBy<T>(Expression<Func<T, object>> keySelector) where T : class, IComponent, new();

    /// <summary>
    /// Orders query results by a component property in descending order.
    /// Clears any existing ordering and sets this as the primary sort.
    /// </summary>
    /// <typeparam name="T">The component type containing the property to sort by.</typeparam>
    /// <param name="keySelector">Expression selecting the property to sort by.</param>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery OrderByDescending<T>(Expression<Func<T, object>> keySelector) where T : class, IComponent, new();

    /// <summary>
    /// Adds secondary ordering by a component property in ascending order.
    /// Must be called after OrderBy or OrderByDescending.
    /// </summary>
    /// <typeparam name="T">The component type containing the property to sort by.</typeparam>
    /// <param name="keySelector">Expression selecting the property to sort by.</param>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery ThenBy<T>(Expression<Func<T, object>> keySelector) where T : class, IComponent, new();

    /// <summary>
    /// Adds secondary ordering by a component property in descending order.
    /// Must be called after OrderBy or OrderByDescending.
    /// </summary>
    /// <typeparam name="T">The component type containing the property to sort by.</typeparam>
    /// <param name="keySelector">Expression selecting the property to sort by.</param>
    /// <returns>The query builder for chaining.</returns>
    IEntityQuery ThenByDescending<T>(Expression<Func<T, object>> keySelector) where T : class, IComponent, new();

    /// <summary>
    /// Executes the query and returns a single Entity object with DataContext binding.
    /// Throws InvalidOperationException if the query returns zero or more than one entity.
    /// </summary>
    /// <returns>A single Entity object with DataContext binding.</returns>
    Task<Entity> Single();
}