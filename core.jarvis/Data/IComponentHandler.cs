namespace core.jarvis.Data;

/// <summary>
/// Base interface for all component handlers.
/// Handlers encapsulate all business logic for a specific component type.
/// </summary>
public interface IComponentHandler
{
    /// <summary>
    /// Initializes the handler with the entity context.
    /// Must be called after construction before any operations.
    /// </summary>
    /// <param name="entityId">The entity ID this handler will operate on.</param>
    void InitializeContext(Guid entityId);
    
    /// <summary>
    /// Gets the component associated with this handler.
    /// </summary>
    /// <returns>The component instance.</returns>
    Task<IComponent> Get();
}

/// <summary>
/// Generic interface for strongly-typed component handlers.
/// </summary>
/// <typeparam name="TComponent">The type of component this handler manages.</typeparam>
public interface IComponentHandler<TComponent> : IComponentHandler where TComponent : IComponent
{
    /// <summary>
    /// Gets the strongly-typed component associated with this handler.
    /// </summary>
    /// <returns>The component instance.</returns>
    new Task<TComponent> Get();
}