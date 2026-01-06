using System.Linq.Expressions;

namespace core.jarvis.Data.Query;

/// <summary>
/// Implementation of IEntityQuery that provides optimized cross-component querying
/// using type-safe handlers without runtime reflection.
/// </summary>
public class EntityQuery : IEntityQuery
{
    private readonly IComponentQueryHandlerRegistry _handlerRegistry;
    private readonly IDataContext? _dataContext;

    // Store filters for each query type
    private readonly Dictionary<Type, LambdaExpression> _withAll = new();
    private readonly Dictionary<Type, LambdaExpression> _withAny = new();
    private readonly Dictionary<Type, LambdaExpression> _withNone = new();
    private readonly Dictionary<Type, QueryPlan> _includes = new();
    private readonly List<OrderingExpression> _orderings = new();

    private class QueryPlan
    {
        public LambdaExpression? Filter { get; set; }
    }

    private class OrderingExpression
    {
        public Type ComponentType { get; set; } = null!;
        public LambdaExpression KeySelector { get; set; } = null!;
        public bool Descending { get; set; }
        public bool IsPrimary { get; set; }
    }

    public EntityQuery(IComponentQueryHandlerRegistry handlerRegistry, IDataContext? dataContext)
    {
        _handlerRegistry = handlerRegistry ?? throw new ArgumentNullException(nameof(handlerRegistry));
        _dataContext = dataContext;
    }

    /// <inheritdoc/>
    public IEntityQuery WithAll<T>(Expression<Func<T, bool>> filter) where T : class, IComponent, new()
    {
        _withAll[typeof(T)] = filter;
        return this;
    }
    
    /// <inheritdoc/>
    public IEntityQuery WithAll<T>() where T : class, IComponent, new()
    {
        // Use a filter that always returns true
        Expression<Func<T, bool>> alwaysTrue = _ => true;
        _withAll[typeof(T)] = alwaysTrue;
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
        
        var entityIds = result.ToList();
        
        // Apply ordering if specified
        if (_orderings.Any() && entityIds.Any())
        {
            entityIds = await ApplyOrdering(entityIds);
        }
        
        return entityIds;
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

    /// <inheritdoc/>
    public async Task<List<Entity>> ToList()
    {
        if (_dataContext == null)
            throw new InvalidOperationException("DataContext is required for ToList(). EntityQuery must be created through IDataContext.Query().");
        
        // Get the entity IDs that match the query
        var entityIds = await ToEntityIds();
        
        // Create Entity objects with DataContext binding
        var entities = new List<Entity>();
        foreach (var id in entityIds)
        {
            var entity = new Entity(id);
            entity.SetContext(_dataContext);
            entities.Add(entity);
        }
        
        return entities;
    }

    /// <summary>
    /// Applies ordering to a list of entity IDs based on component properties.
    /// </summary>
    private async Task<List<Guid>> ApplyOrdering(List<Guid> entityIds)
    {
        // Load components needed for ordering
        var orderingData = new Dictionary<Guid, Dictionary<Type, IComponent>>();
        
        // Get unique component types from orderings
        var componentTypes = _orderings.Select(o => o.ComponentType).Distinct();
        
        foreach (var componentType in componentTypes)
        {
            var handler = _handlerRegistry.GetHandler(componentType);
            var components = await handler.LoadComponents(entityIds);
            
            foreach (var component in components)
            {
                if (!orderingData.ContainsKey(component.OwnerEntityId))
                    orderingData[component.OwnerEntityId] = new Dictionary<Type, IComponent>();
                
                orderingData[component.OwnerEntityId][componentType] = component;
            }
        }
        
        // Apply ordering using LINQ
        IOrderedEnumerable<Guid>? orderedQuery = null;
        
        foreach (var ordering in _orderings)
        {
            // Compile the key selector for this ordering
            var compiledSelector = CompileKeySelector(ordering);
            
            if (ordering.IsPrimary)
            {
                orderedQuery = ordering.Descending
                    ? entityIds.OrderByDescending(id => GetOrderKey(id, ordering, orderingData, compiledSelector))
                    : entityIds.OrderBy(id => GetOrderKey(id, ordering, orderingData, compiledSelector));
            }
            else if (orderedQuery != null)
            {
                orderedQuery = ordering.Descending
                    ? orderedQuery.ThenByDescending(id => GetOrderKey(id, ordering, orderingData, compiledSelector))
                    : orderedQuery.ThenBy(id => GetOrderKey(id, ordering, orderingData, compiledSelector));
            }
        }
        
        return orderedQuery?.ToList() ?? entityIds;
    }
    
    /// <summary>
    /// Compiles the key selector expression for a specific ordering.
    /// </summary>
    private static Delegate CompileKeySelector(OrderingExpression ordering)
    {
        // The KeySelector is Expression<Func<T, object>> but we need to compile it 
        // to a delegate that can work with IComponent
        return ordering.KeySelector.Compile();
    }
    
    /// <summary>
    /// Gets the sort key value for a specific entity and ordering.
    /// </summary>
    private static object? GetOrderKey(Guid entityId, OrderingExpression ordering, 
        Dictionary<Guid, Dictionary<Type, IComponent>> orderingData, Delegate compiledSelector)
    {
        // Check if we have data for this entity
        if (!orderingData.TryGetValue(entityId, out var entityComponents))
            return null;
        
        // Check if we have the component for this ordering
        if (!entityComponents.TryGetValue(ordering.ComponentType, out var component))
            return null;
        
        // Apply the compiled selector to get the sort key
        try
        {
            return compiledSelector.DynamicInvoke(component);
        }
        catch
        {
            // If there's an error invoking the selector, return null to sort to the end
            return null;
        }
    }

    /// <inheritdoc/>
    public IEntityQuery OrderBy<T>(Expression<Func<T, object>> keySelector) where T : class, IComponent, new()
    {
        _orderings.Clear();
        _orderings.Add(new OrderingExpression
        {
            ComponentType = typeof(T),
            KeySelector = keySelector,
            Descending = false,
            IsPrimary = true
        });
        return this;
    }

    /// <inheritdoc/>
    public IEntityQuery OrderByDescending<T>(Expression<Func<T, object>> keySelector) where T : class, IComponent, new()
    {
        _orderings.Clear();
        _orderings.Add(new OrderingExpression
        {
            ComponentType = typeof(T),
            KeySelector = keySelector,
            Descending = true,
            IsPrimary = true
        });
        return this;
    }

    /// <inheritdoc/>
    public IEntityQuery ThenBy<T>(Expression<Func<T, object>> keySelector) where T : class, IComponent, new()
    {
        if (!_orderings.Any())
            throw new InvalidOperationException("ThenBy can only be used after OrderBy or OrderByDescending");
        
        _orderings.Add(new OrderingExpression
        {
            ComponentType = typeof(T),
            KeySelector = keySelector,
            Descending = false,
            IsPrimary = false
        });
        return this;
    }

    /// <inheritdoc/>
    public IEntityQuery ThenByDescending<T>(Expression<Func<T, object>> keySelector) where T : class, IComponent, new()
    {
        if (!_orderings.Any())
            throw new InvalidOperationException("ThenByDescending can only be used after OrderBy or OrderByDescending");
        
        _orderings.Add(new OrderingExpression
        {
            ComponentType = typeof(T),
            KeySelector = keySelector,
            Descending = true,
            IsPrimary = false
        });
        return this;
    }

    /// <inheritdoc/>
    public async Task<Entity> Single()
    {
        if (_dataContext == null)
            throw new InvalidOperationException("DataContext is required for Single(). EntityQuery must be created through IDataContext.Query().");
        
        // Get the entity IDs that match the query
        var entityIds = await ToEntityIds();
        
        // Check for exactly one result
        if (!entityIds.Any())
            throw new InvalidOperationException("Sequence contains no elements");
        
        if (entityIds.Count > 1)
            throw new InvalidOperationException($"Sequence contains more than one element ({entityIds.Count} entities found)");
        
        // Create and return the single Entity with DataContext binding
        var entity = new Entity(entityIds[0]);
        entity.SetContext(_dataContext);
        return entity;
    }
}