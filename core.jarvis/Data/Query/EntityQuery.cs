using System.Linq.Expressions;

namespace core.jarvis.Data.Query;

/// <summary>
/// Implementation of IEntityQuery that provides optimized cross-component querying
/// using type-safe handlers without runtime reflection.
/// </summary>
public class EntityQuery : IEntityQuery
{
    private readonly IComponentQueryHandlerRegistry _handlerRegistry;

    // Store filters for each query type
    private readonly Dictionary<Type, LambdaExpression> _withAll = new();
    private readonly Dictionary<Type, LambdaExpression> _withAny = new();
    private readonly Dictionary<Type, LambdaExpression> _withNone = new();
    private readonly Dictionary<Type, QueryPlan> _includes = new();

    private class QueryPlan
    {
        public LambdaExpression? Filter { get; set; }
    }

    public EntityQuery(IComponentQueryHandlerRegistry handlerRegistry)
    {
        _handlerRegistry = handlerRegistry ?? throw new ArgumentNullException(nameof(handlerRegistry));
    }

    /// <inheritdoc/>
    public IEntityQuery WithAll<T>(Expression<Func<T, bool>> filter) where T : class, IComponent, new()
    {
        _withAll[typeof(T)] = filter;
        return this;
    }

    /// <inheritdoc/>
    public IEntityQuery WithAny<T>(Expression<Func<T, bool>> filter) where T : class, IComponent, new()
    {
        _withAny[typeof(T)] = filter;
        return this;
    }

    /// <inheritdoc/>
    public IEntityQuery WithNone<T>(Expression<Func<T, bool>> filter) where T : class, IComponent, new()
    {
        _withNone[typeof(T)] = filter;
        return this;
    }

    /// <inheritdoc/>
    public IEntityQuery With<T>(Expression<Func<T, bool>> filter) where T : class, new()
    {
        // Legacy support - maps to WithAll
        if (typeof(IComponent).IsAssignableFrom(typeof(T)))
        {
            _withAll[typeof(T)] = filter;
        }
        return this;
    }

    /// <inheritdoc/>
    public IEntityQuery Include<T>() where T : class, new()
    {
        _includes[typeof(T)] = new QueryPlan { Filter = null };
        return this;
    }

    /// <inheritdoc/>
    public IEntityQuery Include<T>(Expression<Func<T, bool>> filter) where T : class, new()
    {
        _includes[typeof(T)] = new QueryPlan { Filter = filter };
        return this;
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> ToEntityIds()
    {
        var allSets = new List<HashSet<Guid>>();
        var anySet = new HashSet<Guid>();
        var noneSet = new HashSet<Guid>();

        // ALL - intersection (AND)
        foreach (var (type, filter) in _withAll)
        {
            var ids = await ExecuteQueryEntityIds(type, filter);
            allSets.Add(ids.ToHashSet());
        }

        // ANY - union (OR)
        foreach (var (type, filter) in _withAny)
        {
            var ids = await ExecuteQueryEntityIds(type, filter);
            anySet.UnionWith(ids);
        }

        // NONE - exclusion (NOT)
        foreach (var (type, filter) in _withNone)
        {
            var ids = await ExecuteQueryEntityIds(type, filter);
            noneSet.UnionWith(ids);
        }

        // Combine results
        HashSet<Guid> result;
        if (allSets.Any())
        {
            result = allSets.Aggregate((a, b) => { a.IntersectWith(b); return a; });
            
            // If we also have ANY filters, intersect with the union of ANY results
            if (anySet.Any())
            {
                result.IntersectWith(anySet);
            }
        }
        else if (anySet.Any())
        {
            result = new HashSet<Guid>(anySet);
        }
        else
        {
            result = new HashSet<Guid>();
        }

        // Exclude NONE matches
        result.ExceptWith(noneSet);
        return result.ToList();
    }

    /// <inheritdoc/>
    public async Task<Dictionary<Guid, EntityComponents>> ToEntityComponents()
    {
        var entityIds = await ToEntityIds();
        if (!entityIds.Any())
            return new Dictionary<Guid, EntityComponents>();

        // Get all types to load (from all filters and includes)
        var allTypes = _withAll.Keys
            .Concat(_withAny.Keys)
            .Concat(_withNone.Keys)
            .Concat(_includes.Keys)
            .Distinct()
            .ToList();

        var result = entityIds.ToDictionary(id => id, _ => new EntityComponents());

        // Load components for each type
        foreach (var type in allTypes)
        {
            await ExecuteLoadAndAssign(type, entityIds, result);
        }

        return result;
    }

    /// <summary>
    /// Executes a query for entity IDs using the appropriate handler.
    /// </summary>
    private async Task<IEnumerable<Guid>> ExecuteQueryEntityIds(Type componentType, LambdaExpression filter)
    {
        var handler = _handlerRegistry.GetHandler(componentType);
        return await handler.QueryEntityIds(filter);
    }

    /// <summary>
    /// Loads components and assigns them to the result dictionary.
    /// </summary>
    private async Task ExecuteLoadAndAssign(Type componentType, List<Guid> entityIds, Dictionary<Guid, EntityComponents> result)
    {
        var handler = _handlerRegistry.GetHandler(componentType);
        var components = await handler.LoadComponents(entityIds);

        foreach (var component in components)
        {
            if (!result.TryGetValue(component.OwnerEntityId, out var ec))
                continue;
            ec.Add(componentType, component);
        }
    }
}