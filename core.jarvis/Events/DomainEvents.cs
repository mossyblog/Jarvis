using MediatR;

namespace core.jarvis.Events;

/// <summary>
/// Base record for domain events originating within the Jarvis framework itself,
/// ensuring necessary context.
/// </summary>
public abstract record DomainEventBase(Guid EntityId, DateTime TimestampUtc) : INotification;

// Application-specific events should be defined in the project where their data contracts reside
// to avoid circular dependencies. 