using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Manages real-time subscriptions per entity and component type.
/// NOTE: This functionality is temporarily disabled as PgClient doesn't support real-time subscriptions.
/// This class is kept for API compatibility but operations are no-ops.
/// </summary>
public class EventSubscriptionManager : IDisposable
{
    private readonly ILogger<EventSubscriptionManager> _logger;
    private bool _disposed;

    public EventSubscriptionManager(ILogger<EventSubscriptionManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Subscribes to real-time changes for a specific component type and entity.
    /// NOTE: Currently a no-op as PgClient doesn't support real-time.
    /// </summary>
    public async Task SubscribeToComponent<TComponent>(Guid entityId, Action<TComponent> onChanged)
        where TComponent : class, IComponent, new()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EventSubscriptionManager));

        _logger.LogWarning("Real-time subscriptions are not supported with PgClient. Component: {Component}, Entity: {EntityId}", 
            typeof(TComponent).Name, entityId);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Unsubscribes from a specific component and entity combination.
    /// NOTE: Currently a no-op as PgClient doesn't support real-time.
    /// </summary>
    public async Task UnsubscribeFromComponent<TComponent>(Guid entityId)
        where TComponent : class, IComponent, new()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EventSubscriptionManager));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Unsubscribes from all subscriptions for a specific entity.
    /// NOTE: Currently a no-op as PgClient doesn't support real-time.
    /// </summary>
    public async Task UnsubscribeFromEntity(Guid entityId)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EventSubscriptionManager));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes all active subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}