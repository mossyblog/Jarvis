using core.jarvis.Data;

namespace core.jarvis.Systems;

/// <summary>
/// System orchestrates handler execution without exposing DataContext
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Execute a handler method that returns an entity ID
    /// </summary>
    Task<Guid> ExecuteHandler<THandler>(Guid entityId, Func<THandler, Task<Guid>> handlerMethod) 
        where THandler : class, IComponentHandler;
    
    /// <summary>
    /// Execute a handler method that returns a component
    /// </summary>
    Task<TComponent> ExecuteHandler<THandler, TComponent>(Guid entityId, Func<THandler, Task<TComponent>> handlerMethod) 
        where THandler : class, IComponentHandler
        where TComponent : IComponent;
    
    /// <summary>
    /// Execute a handler method with no return value
    /// </summary>
    Task ExecuteHandler<THandler>(Guid entityId, Func<THandler, Task> handlerMethod) 
        where THandler : class, IComponentHandler;
    
    /// <summary>
    /// Create a new component (no handler needed for creation)
    /// </summary>
    Task<Guid> CreateComponent<TComponent>(TComponent component) 
        where TComponent : class, IComponent, new();
    
    /// <summary>
    /// Execute a handler method that returns any result type (not constrained to IComponent)
    /// Used for DTOs, lists, stats, etc.
    /// </summary>
    Task<TResult> ExecuteHandlerWithResult<THandler, TResult>(Guid entityId, Func<THandler, Task<TResult>> handlerMethod) 
        where THandler : class, IComponentHandler;
}