using System.Linq.Expressions;
using System.Text;
using core.jarvis.data;

namespace core.jarvis.Data.Query;

/// <summary>
/// Supported filter operators for database queries.
/// </summary>
public enum FilterOperator
{
    /// <summary>Equals (=)</summary>
    Eq,
    /// <summary>Not equals (<>)</summary>
    Neq,
    /// <summary>Greater than (>)</summary>
    Gt,
    /// <summary>Greater than or equal (>=)</summary>
    Gte,
    /// <summary>Less than (<)</summary>
    Lt,
    /// <summary>Less than or equal (<=)</summary>
    Lte,
    /// <summary>LIKE pattern match</summary>
    Like,
    /// <summary>Case-insensitive LIKE pattern match (ILIKE)</summary>
    ILike,
    /// <summary>IN clause with multiple values</summary>
    In,
    /// <summary>IS NULL check</summary>
    IsNull,
    /// <summary>IS NOT NULL check</summary>
    IsNotNull
}

/// <summary>
/// Logical operators for combining filter expressions.
/// </summary>
public enum LogicalOperator
{
    /// <summary>AND - both conditions must be true</summary>
    And,
    /// <summary>OR - at least one condition must be true</summary>
    Or
}

/// <summary>
/// Base implementation for filter expressions.
/// Provides common functionality for And, Or, and Not operations.
/// </summary>
/// <typeparam name="T">The component type to filter.</typeparam>
public abstract class FilterExpressionBase<T> : IFilterExpression<T> where T : class, IComponent, new()
{
    /// <inheritdoc/>
    public abstract IFilterExpression<T> IgnoreCase();

    /// <inheritdoc/>
    public IFilterExpression<T> And(IFilterExpression<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));
        return new CompositeFilter<T>(this, other, LogicalOperator.And);
    }

    /// <inheritdoc/>
    public IFilterExpression<T> Or(IFilterExpression<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));
        return new CompositeFilter<T>(this, other, LogicalOperator.Or);
    }

    /// <inheritdoc/>
    public IFilterExpression<T> Not()
    {
        return new NegatedFilter<T>(this);
    }

    /// <inheritdoc/>
    public abstract PgTable<T> ApplyTo(PgTable<T> query);

    /// <summary>
    /// Converts a PascalCase property name to snake_case column name.
    /// </summary>
    /// <param name="propertyName">The PascalCase property name.</param>
    /// <returns>The snake_case column name.</returns>
    protected static string ToSnakeCase(string propertyName)
    {
        return core.jarvis.data.StringExtensions.ToSnakeCase(propertyName);
    }

    /// <summary>
    /// Extracts the property name from a member access expression.
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="propertyExpression">The property access expression.</param>
    /// <returns>The property name.</returns>
    protected static string GetPropertyName<TValue>(Expression<Func<T, TValue>> propertyExpression)
    {
        if (propertyExpression == null)
            throw new ArgumentNullException(nameof(propertyExpression));

        if (propertyExpression.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }

        if (propertyExpression.Body is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression unaryMemberExpr)
        {
            return unaryMemberExpr.Member.Name;
        }

        throw new ArgumentException($"Expression must be a property access, got: {propertyExpression.Body.NodeType}", nameof(propertyExpression));
    }
}

/// <summary>
/// Represents a single filter condition (e.g., column = value, column > value).
/// </summary>
/// <typeparam name="T">The component type to filter.</typeparam>
public class SingleFilter<T> : FilterExpressionBase<T> where T : class, IComponent, new()
{
    private readonly string _columnName;
    private readonly FilterOperator _operator;
    private readonly object? _value;
    private readonly IEnumerable<object>? _values;
    private bool _ignoreCase;

    /// <summary>
    /// Creates a new single filter with a single value.
    /// </summary>
    /// <param name="columnName">The snake_case column name.</param>
    /// <param name="op">The filter operator.</param>
    /// <param name="value">The value to compare against.</param>
    public SingleFilter(string columnName, FilterOperator op, object? value)
    {
        _columnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        _operator = op;
        _value = value;
        _values = null;
        _ignoreCase = false;
    }

    /// <summary>
    /// Creates a new single filter with multiple values (for IN clause).
    /// </summary>
    /// <param name="columnName">The snake_case column name.</param>
    /// <param name="values">The values to match against.</param>
    public SingleFilter(string columnName, IEnumerable<object> values)
    {
        _columnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        _operator = FilterOperator.In;
        _value = null;
        _values = values ?? throw new ArgumentNullException(nameof(values));
        _ignoreCase = false;
    }

    /// <inheritdoc/>
    public override IFilterExpression<T> IgnoreCase()
    {
        return new SingleFilter<T>(_columnName, _operator, _value)
        {
            _ignoreCase = true
        };
    }

    /// <inheritdoc/>
    public override PgTable<T> ApplyTo(PgTable<T> query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        switch (_operator)
        {
            case FilterOperator.Eq:
                var eqOp = _ignoreCase && _value is string ? "ilike" : "eq";
                return query.Filter(_columnName, eqOp, _value!);

            case FilterOperator.Neq:
                // For case-insensitive not equals, we use NOT (column ILIKE value) pattern
                // But PgTable doesn't support negation directly, so we use neq
                return query.Filter(_columnName, "neq", _value!);

            case FilterOperator.Gt:
                return query.Filter(_columnName, "gt", _value!);

            case FilterOperator.Gte:
                return query.Filter(_columnName, "gte", _value!);

            case FilterOperator.Lt:
                return query.Filter(_columnName, "lt", _value!);

            case FilterOperator.Lte:
                return query.Filter(_columnName, "lte", _value!);

            case FilterOperator.Like:
                var likeOp = _ignoreCase ? "ilike" : "like";
                return query.Filter(_columnName, likeOp, _value!);

            case FilterOperator.ILike:
                return query.Filter(_columnName, "ilike", _value!);

            case FilterOperator.In:
                if (_values == null)
                    throw new InvalidOperationException("IN filter requires values collection");
                return query.In(_columnName, _values);

            case FilterOperator.IsNull:
                // PostgreSQL: column IS NULL
                // PgTable doesn't have direct IS NULL support, so we use a workaround
                // The 'is' operator with 'null' value maps to IS NULL
                return query.Filter(_columnName, "eq", DBNull.Value);

            case FilterOperator.IsNotNull:
                // PostgreSQL: column IS NOT NULL
                // Similar workaround for IS NOT NULL
                return query.Filter(_columnName, "neq", DBNull.Value);

            default:
                throw new NotSupportedException($"Filter operator {_operator} is not supported");
        }
    }
}

/// <summary>
/// Represents a composite filter combining two filters with AND or OR logic.
/// </summary>
/// <typeparam name="T">The component type to filter.</typeparam>
public class CompositeFilter<T> : FilterExpressionBase<T> where T : class, IComponent, new()
{
    private readonly IFilterExpression<T> _left;
    private readonly IFilterExpression<T> _right;
    private readonly LogicalOperator _logicalOperator;

    /// <summary>
    /// Creates a new composite filter.
    /// </summary>
    /// <param name="left">The left-hand filter.</param>
    /// <param name="right">The right-hand filter.</param>
    /// <param name="logicalOperator">The logical operator (AND or OR).</param>
    public CompositeFilter(IFilterExpression<T> left, IFilterExpression<T> right, LogicalOperator logicalOperator)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _logicalOperator = logicalOperator;
    }

    /// <inheritdoc/>
    public override IFilterExpression<T> IgnoreCase()
    {
        // IgnoreCase doesn't apply to composite filters
        // It only affects individual string comparisons
        return this;
    }

    /// <inheritdoc/>
    public override PgTable<T> ApplyTo(PgTable<T> query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        // For AND logic, we can chain filters since PgTable uses AND between Filter() calls
        if (_logicalOperator == LogicalOperator.And)
        {
            var intermediate = _left.ApplyTo(query);
            return _right.ApplyTo(intermediate);
        }

        // For OR logic, PgTable doesn't directly support OR between Filter() calls
        // This is a limitation of the current PgTable implementation
        // We throw a clear exception indicating this limitation
        throw new NotSupportedException(
            "OR logic between filters is not supported by the current PgTable implementation. " +
            "Consider restructuring your query or using multiple separate queries.");
    }
}

/// <summary>
/// Represents a negated filter (NOT condition).
/// </summary>
/// <typeparam name="T">The component type to filter.</typeparam>
public class NegatedFilter<T> : FilterExpressionBase<T> where T : class, IComponent, new()
{
    private readonly IFilterExpression<T> _inner;

    /// <summary>
    /// Creates a new negated filter.
    /// </summary>
    /// <param name="inner">The filter to negate.</param>
    public NegatedFilter(IFilterExpression<T> inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc/>
    public override IFilterExpression<T> IgnoreCase()
    {
        // IgnoreCase on negated filter should apply to inner filter
        return new NegatedFilter<T>(_inner.IgnoreCase());
    }

    /// <inheritdoc/>
    public override PgTable<T> ApplyTo(PgTable<T> query)
    {
        // NOT logic is not directly supported by PgTable's Filter method
        // For simple cases like NOT equals, we can convert to the opposite operator
        // But for complex expressions, this is a limitation
        throw new NotSupportedException(
            "NOT logic is not directly supported by the current PgTable implementation. " +
            "Consider using the opposite operator (e.g., Neq instead of Not(Eq)).");
    }
}

/// <summary>
/// Represents a filter that matches all records (no filtering).
/// </summary>
/// <typeparam name="T">The component type to filter.</typeparam>
public class MatchAllFilter<T> : FilterExpressionBase<T> where T : class, IComponent, new()
{
    /// <summary>
    /// Singleton instance for matching all records.
    /// </summary>
    public static readonly MatchAllFilter<T> Instance = new();

    private MatchAllFilter()
    {
    }

    /// <inheritdoc/>
    public override IFilterExpression<T> IgnoreCase()
    {
        // IgnoreCase doesn't affect a match-all filter
        return this;
    }

    /// <inheritdoc/>
    public override PgTable<T> ApplyTo(PgTable<T> query)
    {
        // No filtering needed - return query unchanged
        return query;
    }
}

/// <summary>
/// Represents a filter that wraps a lambda expression for backwards compatibility.
/// Converts common lambda patterns into filter expressions at query time.
/// </summary>
/// <typeparam name="T">The component type to filter.</typeparam>
public class LambdaFilter<T> : FilterExpressionBase<T> where T : class, IComponent, new()
{
    private readonly Expression<Func<T, bool>> _expression;

    /// <summary>
    /// Creates a new lambda filter wrapping the given expression.
    /// </summary>
    /// <param name="expression">The lambda expression to wrap.</param>
    public LambdaFilter(Expression<Func<T, bool>> expression)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <inheritdoc/>
    public override IFilterExpression<T> IgnoreCase()
    {
        // Lambda expressions don't support IgnoreCase modification
        return this;
    }

    /// <inheritdoc/>
    public override PgTable<T> ApplyTo(PgTable<T> query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        // Parse the lambda expression and convert to filter operations
        return ParseAndApply(query, _expression.Body);
    }

    private PgTable<T> ParseAndApply(PgTable<T> query, Expression expr)
    {
        switch (expr)
        {
            case BinaryExpression binary:
                return HandleBinaryExpression(query, binary);

            case MethodCallExpression methodCall:
                return HandleMethodCallExpression(query, methodCall);

            case ConstantExpression constant when constant.Value is bool boolValue:
                // Handle expressions like (c => true) - return as-is (match all)
                return boolValue ? query : throw new NotSupportedException("Filter (c => false) would match nothing");

            default:
                throw new NotSupportedException(
                    $"Lambda expression type '{expr.NodeType}' is not supported. " +
                    "Please use the Filter<T> API (e.g., Filter<T>.Eq(c => c.Property, value)) for complex queries.");
        }
    }

    private PgTable<T> HandleBinaryExpression(PgTable<T> query, BinaryExpression binary)
    {
        switch (binary.NodeType)
        {
            case ExpressionType.AndAlso:
                // Handle c => expr1 && expr2
                query = ParseAndApply(query, binary.Left);
                return ParseAndApply(query, binary.Right);

            case ExpressionType.Equal:
                return HandleComparison(query, binary, "eq");

            case ExpressionType.NotEqual:
                return HandleComparison(query, binary, "neq");

            case ExpressionType.GreaterThan:
                return HandleComparison(query, binary, "gt");

            case ExpressionType.GreaterThanOrEqual:
                return HandleComparison(query, binary, "gte");

            case ExpressionType.LessThan:
                return HandleComparison(query, binary, "lt");

            case ExpressionType.LessThanOrEqual:
                return HandleComparison(query, binary, "lte");

            default:
                throw new NotSupportedException(
                    $"Binary expression type '{binary.NodeType}' is not supported in lambda filters. " +
                    "Please use the Filter<T> API for complex queries.");
        }
    }

    private PgTable<T> HandleComparison(PgTable<T> query, BinaryExpression binary, string op)
    {
        // Determine which side is the property and which is the value
        string? columnName = null;
        object? value = null;

        if (TryGetPropertyName(binary.Left, out var leftProp))
        {
            columnName = ToSnakeCase(leftProp);
            value = GetExpressionValue(binary.Right);
        }
        else if (TryGetPropertyName(binary.Right, out var rightProp))
        {
            columnName = ToSnakeCase(rightProp);
            value = GetExpressionValue(binary.Left);
            // Flip the operator for reversed comparisons (e.g., 5 > c.Value becomes c.Value < 5)
            op = FlipOperator(op);
        }
        else
        {
            throw new NotSupportedException(
                "Comparison must involve a property access. " +
                "Please use the Filter<T> API for complex expressions.");
        }

        return query.Filter(columnName, op, value!);
    }

    private PgTable<T> HandleMethodCallExpression(PgTable<T> query, MethodCallExpression methodCall)
    {
        var methodName = methodCall.Method.Name;

        switch (methodName)
        {
            case "Contains" when methodCall.Object != null && TryGetPropertyName(methodCall.Object, out var containsProp):
                // Handle c.Property.Contains("value")
                var containsColumnName = ToSnakeCase(containsProp);
                var containsValue = GetExpressionValue(methodCall.Arguments[0]);
                var pattern = $"%{EscapeLikePattern(containsValue?.ToString() ?? "")}%";
                return query.Filter(containsColumnName, "like", pattern);

            case "StartsWith" when methodCall.Object != null && TryGetPropertyName(methodCall.Object, out var startsProp):
                // Handle c.Property.StartsWith("value")
                var startsColumnName = ToSnakeCase(startsProp);
                var startsValue = GetExpressionValue(methodCall.Arguments[0]);
                var startsPattern = $"{EscapeLikePattern(startsValue?.ToString() ?? "")}%";
                return query.Filter(startsColumnName, "like", startsPattern);

            case "EndsWith" when methodCall.Object != null && TryGetPropertyName(methodCall.Object, out var endsProp):
                // Handle c.Property.EndsWith("value")
                var endsColumnName = ToSnakeCase(endsProp);
                var endsValue = GetExpressionValue(methodCall.Arguments[0]);
                var endsPattern = $"%{EscapeLikePattern(endsValue?.ToString() ?? "")}";
                return query.Filter(endsColumnName, "like", endsPattern);

            default:
                throw new NotSupportedException(
                    $"Method '{methodName}' is not supported in lambda filters. " +
                    "Supported methods: Contains, StartsWith, EndsWith. " +
                    "Please use the Filter<T> API for other operations.");
        }
    }

    private static bool TryGetPropertyName(Expression expr, out string propertyName)
    {
        propertyName = "";

        if (expr is MemberExpression member && member.Expression is ParameterExpression)
        {
            propertyName = member.Member.Name;
            return true;
        }

        // Handle unary expressions (e.g., boxing)
        if (expr is UnaryExpression unary && unary.Operand is MemberExpression unaryMember
            && unaryMember.Expression is ParameterExpression)
        {
            propertyName = unaryMember.Member.Name;
            return true;
        }

        return false;
    }

    private static object? GetExpressionValue(Expression expr)
    {
        switch (expr)
        {
            case ConstantExpression constant:
                return constant.Value;

            case MemberExpression member:
                // Handle closure variables (e.g., local variables captured in the lambda)
                return Expression.Lambda(member).Compile().DynamicInvoke();

            case UnaryExpression unary:
                return GetExpressionValue(unary.Operand);

            default:
                // Try to compile and evaluate the expression
                try
                {
                    return Expression.Lambda(expr).Compile().DynamicInvoke();
                }
                catch
                {
                    throw new NotSupportedException(
                        $"Cannot extract value from expression type '{expr.NodeType}'. " +
                        "Please use the Filter<T> API for complex expressions.");
                }
        }
    }

    private static string FlipOperator(string op) => op switch
    {
        "gt" => "lt",
        "gte" => "lte",
        "lt" => "gt",
        "lte" => "gte",
        _ => op
    };

    private static string EscapeLikePattern(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }
}
