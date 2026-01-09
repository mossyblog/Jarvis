using core.jarvis.data;

namespace core.jarvis.Data.Query;

/// <summary>
/// Represents a filter expression that can be applied to a query.
/// Provides a type-safe, fluent API for building database queries without LINQ lambda limitations.
/// </summary>
/// <typeparam name="T">The component type to filter.</typeparam>
public interface IFilterExpression<T> where T : class, IComponent, new()
{
    /// <summary>
    /// Makes the comparison case-insensitive (for string comparisons).
    /// Only affects string operations like Eq, Contains, StartsWith, EndsWith.
    /// </summary>
    /// <returns>A new filter expression with case-insensitive comparison.</returns>
    IFilterExpression<T> IgnoreCase();

    /// <summary>
    /// Combines this filter with another using AND logic.
    /// Both conditions must be true for a record to match.
    /// </summary>
    /// <param name="other">The other filter to combine with.</param>
    /// <returns>A composite filter expression representing (this AND other).</returns>
    IFilterExpression<T> And(IFilterExpression<T> other);

    /// <summary>
    /// Combines this filter with another using OR logic.
    /// Either condition must be true for a record to match.
    /// </summary>
    /// <param name="other">The other filter to combine with.</param>
    /// <returns>A composite filter expression representing (this OR other).</returns>
    IFilterExpression<T> Or(IFilterExpression<T> other);

    /// <summary>
    /// Negates this filter.
    /// Records that would have matched will not match, and vice versa.
    /// </summary>
    /// <returns>A filter expression representing NOT(this).</returns>
    IFilterExpression<T> Not();

    /// <summary>
    /// Applies this filter to a PgTable query.
    /// This method translates the filter expression into actual database query operations.
    /// </summary>
    /// <param name="query">The PgTable query to apply the filter to.</param>
    /// <returns>The query with this filter applied.</returns>
    PgTable<T> ApplyTo(PgTable<T> query);
}
