using Microsoft.Extensions.Logging;

namespace core.jarvis.Events.Emitters;

public class InMemoryEventEmitter : IEventEmitter
{
    private readonly List<IEvent> _emittedEvents = new();
    private readonly ILogger<InMemoryEventEmitter> _logger;

    public IReadOnlyList<IEvent> EmittedEvents => _emittedEvents.AsReadOnly();

    public InMemoryEventEmitter(ILogger<InMemoryEventEmitter> logger)
    {
        _logger = logger;
    }

    public Task Emit<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IEvent
    {
        _emittedEvents.Add(@event);
        _logger.LogDebug("Emitted event {EventId} to in-memory store", @event.Id);
        return Task.CompletedTask;
    }

    public Task EmitBatch<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default) 
        where TEvent : IEvent
    {
        var eventsList = events.ToList();
        _emittedEvents.AddRange(eventsList.Cast<IEvent>());
        _logger.LogDebug("Emitted {Count} events to in-memory store", eventsList.Count);
        return Task.CompletedTask;
    }

    public void Clear() => _emittedEvents.Clear();
}