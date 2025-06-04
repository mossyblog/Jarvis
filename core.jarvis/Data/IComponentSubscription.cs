namespace core.jarvis.Data;

/// <summary>
/// Abstraction for managing individual real-time subscriptions.
/// </summary>
public interface IComponentSubscription : IDisposable
{
    /// <summary>
    /// The entity ID this subscription is filtered for.
    /// </summary>
    Guid EntityId { get; }
    
    /// <summary>
    /// The component type being subscribed to.
    /// </summary>
    Type ComponentType { get; }
    
    /// <summary>
    /// Whether the subscription is currently active.
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// Starts the subscription.
    /// </summary>
    Task StartAsync();
    
    /// <summary>
    /// Stops the subscription.
    /// </summary>
    Task StopAsync();
}