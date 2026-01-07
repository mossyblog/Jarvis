using System.Linq.Expressions;
using core.jarvis.data;

namespace core.jarvis.Data.Query;

/// <summary>
/// Static factory class for creating type-safe filter expressions.
/// Provides a fluent API for building database queries that replaces LINQ-like lambda expressions.
///
/// Usage Examples:
/// <code>
/// // Simple equality
/// var filter = Filter&lt;Account&gt;.Eq(a => a.Email, "user@example.com");
///
/// // Case-insensitive comparison
/// var filter = Filter&lt;Account&gt;.Eq(a => a.Email, "USER@EXAMPLE.COM").IgnoreCase();
///
/// // Combining filters
/// var filter = Filter&lt;Account&gt;.Eq(a => a.IsActive, true)
///     .And(Filter&lt;Account&gt;.Gte(a => a.CreatedAt, DateTime.Today));
///
/// // String operations
/// var filter = Filter&lt;Account&gt;.Contains(a => a.Name, "john");
///
/// // Collection operations
/// var filter = Filter&lt;Account&gt;.In(a => a.Status, new[] { "active", "pending" });
///
/// // Match all records
/// var filter = Filter&lt;Account&gt;.All();
/// </code>
/// </summary>
/// <typeparam name="T">The component type to filter.</typeparam>
public static class Filter<T> where T : class, IComponent, new()
{
    #region Equality Operators

    /// <summary>
    /// Creates a filter for equality comparison (column = value).
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="property">Expression selecting the property to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A filter expression for the equality condition.</returns>
    public static IFilterExpression<T> Eq<TValue>(Expression<Func<T, TValue>> property, TValue value)
    {
        var columnName = GetColumnName(property);
        return new SingleFilter<T>(columnName, FilterOperator.Eq, value);
    }

    /// <summary>
    /// Creates a filter for inequality comparison (column != value).
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="property">Expression selecting the property to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A filter expression for the inequality condition.</returns>
    public static IFilterExpression<T> Neq<TValue>(Expression<Func<T, TValue>> property, TValue value)
    {
        var columnName = GetColumnName(property);
        return new SingleFilter<T>(columnName, FilterOperator.Neq, value);
    }

    #endregion

    #region Comparison Operators

    /// <summary>
    /// Creates a filter for greater than comparison (column > value).
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="property">Expression selecting the property to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A filter expression for the greater than condition.</returns>
    public static IFilterExpression<T> Gt<TValue>(Expression<Func<T, TValue>> property, TValue value)
    {
        var columnName = GetColumnName(property);
        return new SingleFilter<T>(columnName, FilterOperator.Gt, value);
    }

    /// <summary>
    /// Creates a filter for greater than or equal comparison (column >= value).
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="property">Expression selecting the property to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A filter expression for the greater than or equal condition.</returns>
    public static IFilterExpression<T> Gte<TValue>(Expression<Func<T, TValue>> property, TValue value)
    {
        var columnName = GetColumnName(property);
        return new SingleFilter<T>(columnName, FilterOperator.Gte, value);
    }

    /// <summary>
    /// Creates a filter for less than comparison (column < value).
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="property">Expression selecting the property to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A filter expression for the less than condition.</returns>
    public static IFilterExpression<T> Lt<TValue>(Expression<Func<T, TValue>> property, TValue value)
    {
        var columnName = GetColumnName(property);
        return new SingleFilter<T>(columnName, FilterOperator.Lt, value);
    }

    /// <summary>
    /// Creates a filter for less than or equal comparison (column <= value).
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="property">Expression selecting the property to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A filter expression for the less than or equal condition.</returns>
    public static IFilterExpression<T> Lte<TValue>(Expression<Func<T, TValue>> property, TValue value)
    {
        var columnName = GetColumnName(property);
        return new SingleFilter<T>(columnName, FilterOperator.Lte, value);
    }

    #endregion

    #region String Operations

    /// <summary>
    /// Creates a filter for substring matching (column LIKE '%value%').
    /// </summary>
    /// <param name="property">Expression selecting the string property to filter on.</param>
    /// <param name="value">The substring to search for.</param>
    /// <returns>A filter expression for the contains condition.</returns>
    public static IFilterExpression<T> Contains(Expression<Func<T, string>> property, string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var columnName = GetColumnName(property);
        var pattern = $"%{EscapeLikePattern(value)}%";
        return new SingleFilter<T>(columnName, FilterOperator.Like, pattern);
    }

    /// <summary>
    /// Creates a filter for prefix matching (column LIKE 'value%').
    /// </summary>
    /// <param name="property">Expression selecting the string property to filter on.</param>
    /// <param name="value">The prefix to search for.</param>
    /// <returns>A filter expression for the starts with condition.</returns>
    public static IFilterExpression<T> StartsWith(Expression<Func<T, string>> property, string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var columnName = GetColumnName(property);
        var pattern = $"{EscapeLikePattern(value)}%";
        return new SingleFilter<T>(columnName, FilterOperator.Like, pattern);
    }

    /// <summary>
    /// Creates a filter for suffix matching (column LIKE '%value').
    /// </summary>
    /// <param name="property">Expression selecting the string property to filter on.</param>
    /// <param name="value">The suffix to search for.</param>
    /// <returns>A filter expression for the ends with condition.</returns>
    public static IFilterExpression<T> EndsWith(Expression<Func<T, string>> property, string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var columnName = GetColumnName(property);
        var pattern = $"%{EscapeLikePattern(value)}";
        return new SingleFilter<T>(columnName, FilterOperator.Like, pattern);
    }

    #endregion

    #region Collection Operations

    /// <summary>
    /// Creates a filter for matching against a set of values (column IN (values)).
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="property">Expression selecting the property to filter on.</param>
    /// <param name="values">The collection of values to match against.</param>
    /// <returns>A filter expression for the IN condition.</returns>
    public static IFilterExpression<T> In<TValue>(Expression<Func<T, TValue>> property, IEnumerable<TValue> values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var columnName = GetColumnName(property);
        var objectValues = values.Cast<object>();
        return new SingleFilter<T>(columnName, objectValues);
    }

    #endregion

    #region Null Checks

    /// <summary>
    /// Creates a filter for null check (column IS NULL).
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="property">Expression selecting the property to filter on.</param>
    /// <returns>A filter expression for the IS NULL condition.</returns>
    public static IFilterExpression<T> IsNull<TValue>(Expression<Func<T, TValue>> property)
    {
        var columnName = GetColumnName(property);
        return new SingleFilter<T>(columnName, FilterOperator.IsNull, null);
    }

    /// <summary>
    /// Creates a filter for not null check (column IS NOT NULL).
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="property">Expression selecting the property to filter on.</param>
    /// <returns>A filter expression for the IS NOT NULL condition.</returns>
    public static IFilterExpression<T> IsNotNull<TValue>(Expression<Func<T, TValue>> property)
    {
        var columnName = GetColumnName(property);
        return new SingleFilter<T>(columnName, FilterOperator.IsNotNull, null);
    }

    #endregion

    #region Match All

    /// <summary>
    /// Creates a filter that matches all records (no filtering).
    /// This replaces the lambda pattern (r => true).
    /// </summary>
    /// <returns>A filter expression that matches all records.</returns>
    public static IFilterExpression<T> All()
    {
        return MatchAllFilter<T>.Instance;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Extracts the property name from an expression and converts it to snake_case.
    /// </summary>
    private static string GetColumnName<TValue>(Expression<Func<T, TValue>> property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        var propertyName = GetPropertyName(property);
        return core.jarvis.data.StringExtensions.ToSnakeCase(propertyName);
    }

    /// <summary>
    /// Extracts the property name from a member access expression.
    /// </summary>
    private static string GetPropertyName<TValue>(Expression<Func<T, TValue>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }

        if (propertyExpression.Body is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression unaryMemberExpr)
        {
            return unaryMemberExpr.Member.Name;
        }

        throw new ArgumentException(
            $"Expression must be a property access, got: {propertyExpression.Body.NodeType}",
            nameof(propertyExpression));
    }

    /// <summary>
    /// Escapes special characters in LIKE patterns to prevent SQL injection and ensure literal matching.
    /// PostgreSQL LIKE special characters: %, _, \
    /// </summary>
    private static string EscapeLikePattern(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Escape backslash first, then the LIKE wildcards
        return value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    #endregion
}
