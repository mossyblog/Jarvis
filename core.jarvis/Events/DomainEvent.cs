namespace core.jarvis.Events;

public abstract record DomainEvent : IEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public virtual string Type => GetType().Name;
    public Dictionary<string, object> Metadata { get; } = new();
}