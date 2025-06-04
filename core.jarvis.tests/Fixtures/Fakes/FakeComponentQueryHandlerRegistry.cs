using System.Linq.Expressions;
using core.jarvis.Data;
using core.jarvis.Data.Query;
using core.jarvis.Exceptions;

namespace core.jarvis.tests.Fixtures.Fakes;

/// <summary>
/// Fake implementation of IComponentQueryHandlerRegistry for testing.
/// </summary>
public class FakeComponentQueryHandlerRegistry : IComponentQueryHandlerRegistry
{
    private readonly Dictionary<Type, IComponentQueryHandler> _handlers = new();

    public void AddHandler(Type componentType, IComponentQueryHandler handler)
    {
        _handlers[componentType] = handler;
    }

    public IComponentQueryHandler GetHandler(Type componentType)
    {
        if (_handlers.TryGetValue(componentType, out var handler))
        {
            return handler;
        }

        throw new ComponentNotFoundException(
            $"No query handler registered for component type {componentType.Name}");
    }
}

/// <summary>
/// Fake implementation of IComponentQueryHandler for testing.
/// </summary>
public class FakeComponentQueryHandler : IComponentQueryHandler
{
    private readonly Type _componentType;
    private readonly List<Guid> _entityIds = new();
    private readonly List<IComponent> _components = new();

    public FakeComponentQueryHandler(Type componentType)
    {
        _componentType = componentType;
    }

    public Type ComponentType => _componentType;

    public void SetEntityIds(IEnumerable<Guid> entityIds)
    {
        _entityIds.Clear();
        _entityIds.AddRange(entityIds);
    }

    public void SetComponents(IEnumerable<IComponent> components)
    {
        _components.Clear();
        _components.AddRange(components);
    }

    public Task<IEnumerable<Guid>> QueryEntityIds(LambdaExpression filter)
    {
        return Task.FromResult(_entityIds.AsEnumerable());
    }

    public Task<IEnumerable<IComponent>> LoadComponents(IEnumerable<Guid> entityIds)
    {
        return Task.FromResult(_components.Where(c => entityIds.Contains(c.OwnerEntityId)));
    }
}