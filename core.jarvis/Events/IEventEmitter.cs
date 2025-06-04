namespace core.jarvis.Events;

public interface IEventEmitter
{
    Task Emit<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IEvent;
    
    Task EmitBatch<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default) 
        where TEvent : IEvent;
}