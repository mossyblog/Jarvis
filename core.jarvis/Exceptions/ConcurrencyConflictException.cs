namespace core.jarvis.Exceptions;

/// <summary>
/// Exception thrown when a concurrency conflict occurs during optimistic locking.
/// </summary>
public class ConcurrencyConflictException : Exception
{
    public Guid EntityId { get; }
    public string ComponentType { get; }
    public int? ExpectedVersion { get; }
    public int? ActualVersion { get; }

    public ConcurrencyConflictException(Guid entityId, string componentType, int? expectedVersion, int? actualVersion)
        : base($"Concurrency conflict for {componentType} on entity {entityId}. Expected version {expectedVersion}, found {actualVersion}.")
    {
        EntityId = entityId;
        ComponentType = componentType;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public ConcurrencyConflictException(string message) : base(message) { }
    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException) { }
}
