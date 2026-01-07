using core.jarvis.Data.GraphQL;
using core.jarvis.Data.Query;

namespace core.jarvis.Data;

/// <summary>
/// Core data operations interface for DataContext delegation.
/// </summary>
public interface IDataContextCore
{
    IComponentHandler For(Type componentType, Guid entityId);
    THandler For<THandler>(Guid entityId) where THandler : class, IComponentHandler;
    Entity NewEntity();
    Task EnsureTableExists<TComponent>() where TComponent : class, IComponent, new();
    Task<TComponent?> GetExistingComponent<TComponent>(TComponent component) where TComponent : class, IComponent, new();
    Task Commit<TComponent>(TComponent component) where TComponent : class, IComponent, new();
    Task<bool> TryCommit<TComponent>(TComponent component) where TComponent : class, IComponent, new();
    Task CreateSnapshot<TComponent>(TComponent component, TComponent? existing, string operation) where TComponent : class, IComponent, new();
    Task Remove<TComponent>(Guid entityId) where TComponent : class, IComponent, new();
    Task Insert<TModel>(TModel model) where TModel : class, IComponent, new();
    ISnapshotQuery Snapshots();
    IGraphQLQuery GraphQL(string query);
}
