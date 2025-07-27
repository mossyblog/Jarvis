using System.Linq.Expressions;
using core.jarvis.data;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Data.Query;

/// <summary>
/// Generic implementation of component query handler that provides type-safe
/// querying without runtime reflection.
/// </summary>
/// <typeparam name="T">The component type this handler manages.</typeparam>
public class ComponentQueryHandler<T> : IComponentQueryHandler<T> 
    where T : class, IComponent, new()
{
    private readonly IPgClient _pgClient;
    private readonly ILogger<ComponentQueryHandler<T>> _logger;

    public ComponentQueryHandler(IPgClient pgClient, ILogger<ComponentQueryHandler<T>> logger)
    {
        _pgClient = pgClient ?? throw new ArgumentNullException(nameof(pgClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Type ComponentType => typeof(T);

    /// <inheritdoc/>
    public async Task<IEnumerable<Guid>> QueryEntityIds(Expression<Func<T, bool>> filter)
    {
        _logger.LogTrace("ComponentQueryHandler.QueryEntityIds called for {ComponentType}", typeof(T).Name);
        
        var query = _pgClient.From<T>();
        
        // Apply filter at database level if provided
        if (filter != null)
        {
            _logger.LogTrace("Filter provided: {Filter}", filter);
            try
            {
                query = ApplyExpressionFilter(query, filter);
                _logger.LogTrace("Expression filter applied successfully");
            }
            catch (Exception ex)
            {
                // Log the error and fall back to in-memory filtering for now
                _logger.LogWarning(ex, "Expression filtering failed: {Message}. Filter expression: {Filter}. Falling back to in-memory filtering", ex.Message, filter);
                
                // Fallback to in-memory filtering
                var allResults = await query.Get();
                _logger.LogWarning("Loaded {RecordCount} records for in-memory filtering", allResults.Count);
                var compiledFilter = filter.Compile();
                var filteredResults = allResults.Where(compiledFilter).ToList();
                _logger.LogWarning("Filtered to {FilteredCount} records in memory", filteredResults.Count);
                
                var fallbackEntityIds = new HashSet<Guid>();
                foreach (var model in filteredResults)
                {
                    if (model is IComponent component)
                    {
                        fallbackEntityIds.Add(component.OwnerEntityId);
                    }
                }
                return fallbackEntityIds;
            }
        }
        else
        {
            _logger.LogTrace("No filter provided, loading all records");
        }
        
        var results = await query.Get();
        _logger.LogTrace("Query returned {RecordCount} records", results.Count);
        
        var entityIds = new HashSet<Guid>();
        foreach (var model in results)
        {
            if (model is IComponent component)
            {
                entityIds.Add(component.OwnerEntityId);
            }
        }
        
        _logger.LogTrace("Returning {EntityIdCount} entity IDs", entityIds.Count);
        return entityIds;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> LoadComponents(IEnumerable<Guid> entityIds)
    {
        var idList = entityIds.ToList();
        _logger.LogTrace("LoadComponents called for {ComponentType} with {EntityIdCount} entity IDs: {EntityIds}", 
            typeof(T).Name, idList.Count, string.Join(", ", idList));
        
        if (!idList.Any())
        {
            _logger.LogTrace("No entity IDs provided, returning empty collection");
            return Enumerable.Empty<T>();
        }

        const int batchSize = 50;
        var allResults = new List<T>();
        
        for (int i = 0; i < idList.Count; i += batchSize)
        {
            var batch = idList.Skip(i).Take(batchSize).ToList();
            _logger.LogTrace("Loading batch {BatchNumber} with {BatchSize} entity IDs", i / batchSize + 1, batch.Count);
            
            var table = _pgClient.From<T>();
            var results = await table
                .In("owner_entity_id", batch.Cast<object>())
                .Get();
            
            _logger.LogTrace("Batch {BatchNumber} returned {ResultCount} components", i / batchSize + 1, results.Count);
            allResults.AddRange(results);
        }
        
        _logger.LogTrace("LoadComponents returning {TotalCount} components for {ComponentType}", allResults.Count, typeof(T).Name);
        return allResults;
    }

    /// <inheritdoc/>
    async Task<IEnumerable<Guid>> IComponentQueryHandler.QueryEntityIds(LambdaExpression filter)
    {
        return await QueryEntityIds((Expression<Func<T, bool>>)filter);
    }

    /// <inheritdoc/>
    async Task<IEnumerable<IComponent>> IComponentQueryHandler.LoadComponents(IEnumerable<Guid> entityIds)
    {
        var components = await LoadComponents(entityIds);
        return components.Cast<IComponent>();
    }
    
    /// <summary>
    /// Converts LINQ expressions to PgTable filter operations for database-level filtering.
    /// </summary>
    /// <param name="query">The PgTable query to apply filters to.</param>
    /// <param name="filter">The LINQ expression to convert to SQL filters.</param>
    /// <returns>The query with applied filters.</returns>
    private PgTable<T> ApplyExpressionFilter(PgTable<T> query, Expression<Func<T, bool>> filter)
    {
        try
        {
            return ProcessExpression(query, filter.Body);
        }
        catch (NotSupportedException ex)
        {
            throw new InvalidOperationException(
                $"Expression '{filter}' cannot be translated to SQL. Supported patterns: " +
                "property == value, property != value, property.Contains(value). " +
                $"Error: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Recursively processes expression nodes to build SQL filters.
    /// </summary>
    private PgTable<T> ProcessExpression(PgTable<T> query, Expression expression)
    {
        switch (expression)
        {
            case BinaryExpression binaryExpr:
                return ProcessBinaryExpression(query, binaryExpr);
                
            case MethodCallExpression methodExpr:
                return ProcessMethodCallExpression(query, methodExpr);
                
            case ConstantExpression constantExpr:
                // Handle constant expressions like 'true' or 'false'
                if (constantExpr.Value is bool boolValue)
                {
                    if (boolValue)
                    {
                        // Return query unchanged for 'true' (no filter)
                        return query;
                    }
                    else
                    {
                        // For 'false', we'd need to return empty results
                        // This is a special case that would require different handling
                        throw new NotSupportedException("Constant 'false' expression would return no results");
                    }
                }
                throw new NotSupportedException($"Constant expression of type {constantExpr.Type} is not supported");
                
            default:
                throw new NotSupportedException($"Expression type {expression.NodeType} is not supported");
        }
    }
    
    /// <summary>
    /// Processes binary expressions like ==, !=, <, >, etc.
    /// </summary>
    private PgTable<T> ProcessBinaryExpression(PgTable<T> query, BinaryExpression binaryExpr)
    {
        // Get property name from left side (e.g., "OwnerEntityId" from c.OwnerEntityId)
        if (!(binaryExpr.Left is MemberExpression memberExpr))
        {
            throw new NotSupportedException("Left side of comparison must be a property access");
        }
        
        var propertyName = memberExpr.Member.Name;
        var columnName = ConvertPropertyNameToColumnName(propertyName);
        
        // Get value from right side
        var value = ExtractValue(binaryExpr.Right);
        
        // Convert expression type to SQL operator
        var sqlOperator = binaryExpr.NodeType switch
        {
            ExpressionType.Equal => "eq",
            ExpressionType.NotEqual => "neq", 
            ExpressionType.LessThan => "lt",
            ExpressionType.LessThanOrEqual => "lte",
            ExpressionType.GreaterThan => "gt",
            ExpressionType.GreaterThanOrEqual => "gte",
            _ => throw new NotSupportedException($"Binary operator {binaryExpr.NodeType} is not supported")
        };
        
        return query.Filter(columnName, sqlOperator, value);
    }
    
    /// <summary>
    /// Processes method call expressions like string.Contains().
    /// </summary>
    private PgTable<T> ProcessMethodCallExpression(PgTable<T> query, MethodCallExpression methodExpr)
    {
        if (methodExpr.Method.Name == "Contains" && methodExpr.Method.DeclaringType == typeof(string))
        {
            // Handle: property.Contains(value)
            if (!(methodExpr.Object is MemberExpression memberExpr))
            {
                throw new NotSupportedException("Contains must be called on a property");
            }
            
            var propertyName = memberExpr.Member.Name;
            var columnName = ConvertPropertyNameToColumnName(propertyName);
            var value = ExtractValue(methodExpr.Arguments[0]);
            
            // Use LIKE operator for contains
            return query.Filter(columnName, "like", $"%{value}%");
        }
        
        throw new NotSupportedException($"Method {methodExpr.Method.Name} is not supported");
    }
    
    /// <summary>
    /// Extracts the actual value from an expression (handles constants, variables, and property access).
    /// </summary>
    private object ExtractValue(Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression constantExpr:
                return constantExpr.Value;
                
            case MemberExpression memberExpr when memberExpr.Expression is ConstantExpression constExpr:
                // Handle field/property access on captured variables
                var field = memberExpr.Member as System.Reflection.FieldInfo;
                var property = memberExpr.Member as System.Reflection.PropertyInfo;
                
                if (field != null)
                    return field.GetValue(constExpr.Value);
                if (property != null)
                    return property.GetValue(constExpr.Value);
                    
                throw new NotSupportedException($"Cannot extract value from member {memberExpr.Member.Name}");
                
            case UnaryExpression unaryExpr when unaryExpr.NodeType == ExpressionType.Convert:
                // Handle type conversions
                return ExtractValue(unaryExpr.Operand);
                
            default:
                // For complex expressions, compile and execute them
                var lambda = Expression.Lambda(expression);
                return lambda.Compile().DynamicInvoke();
        }
    }
    
    /// <summary>
    /// Converts C# property names to database column names using snake_case convention.
    /// </summary>
    private string ConvertPropertyNameToColumnName(string propertyName)
    {
        // Use the proper ToSnakeCase extension method from StringExtensions
        return propertyName.ToSnakeCase();
    }
}