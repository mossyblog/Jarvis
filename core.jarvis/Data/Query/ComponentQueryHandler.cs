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
    public async Task<IEnumerable<Guid>> QueryEntityIds(IFilterExpression<T> filter)
    {
        _logger.LogTrace("ComponentQueryHandler.QueryEntityIds called for {ComponentType}", typeof(T).Name);

        var query = _pgClient.From<T>();

        // Apply filter at database level if provided
        if (filter != null)
        {
            _logger.LogTrace("Filter provided: {FilterType}", filter.GetType().Name);
            query = filter.ApplyTo(query);
            _logger.LogTrace("Filter applied successfully");
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
    async Task<IEnumerable<Guid>> IComponentQueryHandler.QueryEntityIds(object filter)
    {
        if (filter == null)
        {
            return await QueryEntityIds(Filter<T>.All());
        }

        if (filter is IFilterExpression<T> typedFilter)
        {
            return await QueryEntityIds(typedFilter);
        }

        throw new ArgumentException(
            $"Filter must be of type IFilterExpression<{typeof(T).Name}>, but was {filter.GetType().Name}",
            nameof(filter));
    }

    /// <inheritdoc/>
    async Task<IEnumerable<IComponent>> IComponentQueryHandler.LoadComponents(IEnumerable<Guid> entityIds)
    {
        var components = await LoadComponents(entityIds);
        return components.Cast<IComponent>();
    }
}
