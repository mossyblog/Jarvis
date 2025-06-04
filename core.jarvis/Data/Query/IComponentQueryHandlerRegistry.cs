namespace core.jarvis.Data.Query;

/// <summary>
/// Registry for component query handlers that enables dynamic resolution
/// without coupling to specific component types.
/// </summary>
public interface IComponentQueryHandlerRegistry
{
    /// <summary>
    /// Gets the query handler for the specified component type.
    /// </summary>
    /// <param name="componentType">The component type to get a handler for.</param>
    /// <returns>The query handler for the component type.</returns>
    /// <exception cref="ComponentNotFoundException">Thrown when no handler is registered for the component type.</exception>
    IComponentQueryHandler GetHandler(Type componentType);
}