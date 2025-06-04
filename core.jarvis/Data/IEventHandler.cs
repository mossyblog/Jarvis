namespace core.jarvis.Data;

/// <summary>
/// Interface for handlers that can subscribe to real-time component changes.
/// </summary>
/// <typeparam name="TComponent">The type of component to subscribe to.</typeparam>
public interface IEventHandler<TComponent> where TComponent : IComponent
{
    /// <summary>
    /// Initializes the event handler and sets up subscriptions.
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Cleans up subscriptions when the handler is disposed.
    /// </summary>
    Task DisposeAsync();
}