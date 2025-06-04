using Microsoft.Extensions.Logging;

namespace core.jarvis.Events.Emitters;

public class NoOpEventEmitter : IEventEmitter
{
    private readonly ILogger<NoOpEventEmitter> _logger;

    public NoOpEventEmitter(ILogger<NoOpEventEmitter> logger)
    {
        _logger = logger;
    }

    public Task Emit<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IEvent
    {
        _logger.LogTrace("Event emission disabled - ignoring event {EventId}", @event.Id);
        return Task.CompletedTask;
    }

    public Task EmitBatch<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default) 
        where TEvent : IEvent
    {
        _logger.LogTrace("Event emission disabled - ignoring {Count} events", events.Count());
        return Task.CompletedTask;
    }
}