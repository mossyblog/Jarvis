using core.jarvis.Exceptions;

namespace core.jarvis.Data.Query;

/// <summary>
/// Registry for component query handlers that resolves handlers via DI.
/// Query handlers must be registered in DI as IComponentQueryHandler<T>.
/// </summary>
public class ComponentQueryHandlerRegistry : IComponentQueryHandlerRegistry
{
    private readonly IServiceProvider _serviceProvider;

    public ComponentQueryHandlerRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public IComponentQueryHandler GetHandler(Type componentType)
    {
        // Create the generic handler interface type IComponentQueryHandler<T>
        var handlerInterfaceType = typeof(IComponentQueryHandler<>).MakeGenericType(componentType);
        
        // Resolve from DI
        var handler = _serviceProvider.GetService(handlerInterfaceType);
        
        if (handler is IComponentQueryHandler queryHandler)
        {
            return queryHandler;
        }

        throw new ComponentNotFoundException(
            $"No query handler registered for component type {componentType.Name}. " +
            $"Ensure IComponentQueryHandler<{componentType.Name}> is registered in DI.");
    }
}