using System.Linq.Expressions;

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

    public ComponentQueryHandler(IPgClient pgClient)
    {
        _pgClient = pgClient ?? throw new ArgumentNullException(nameof(pgClient));
    }

    /// <inheritdoc/>
    public Type ComponentType => typeof(T);

    /// <inheritdoc/>
    public async Task<IEnumerable<Guid>> QueryEntityIds(Expression<Func<T, bool>> filter)
    {
        // Note: PgTable doesn't support Expression filters directly.
        // For now, we'll load all records and filter in memory.
        // TODO: Extend PgTable to support expression-based filtering.
        var query = _pgClient.From<T>();
        var results = await query.Get();
        
        // Apply filter in memory if provided
        if (filter != null)
        {
            var compiledFilter = filter.Compile();
            results = results.Where(compiledFilter).ToList();
        }
        
        var entityIds = new HashSet<Guid>();
        foreach (var model in results)
        {
            if (model is IComponent component)
            {
                entityIds.Add(component.OwnerEntityId);
            }
        }

        return entityIds;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> LoadComponents(IEnumerable<Guid> entityIds)
    {
        var idList = entityIds.ToList();
        if (!idList.Any())
            return Enumerable.Empty<T>();

        const int batchSize = 50;
        var allResults = new List<T>();
        
        for (int i = 0; i < idList.Count; i += batchSize)
        {
            var batch = idList.Skip(i).Take(batchSize).ToList();
            var table = _pgClient.From<T>();
            var results = await table
                .In("owner_entity_id", batch.Cast<object>())
                .Get();
            allResults.AddRange(results);
        }
        
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
}