namespace core.jarvis.Events;

public interface IEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
    string Type { get; }
    Dictionary<string, object> Metadata { get; }
}