// For IComponent

namespace core.jarvis.Data
{
    /// <summary>
    /// Represents a log of domain events emitted for a specific entity,
    /// stored as a component on the entity itself.
    /// </summary>
    public record EventAuditLog : IComponent
    {
        /// <summary>
        /// Chronological list of events emitted for this entity.
        /// </summary>
        public List<EmittedEventRecord> Events { get; init; } = new();

        /// <summary>
        /// The unique identifier for this component instance.
        /// Usually matches the OwnerEntityId unless components are reused/shared (not typical here).
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// The ID of the entity this component belongs to.
        /// </summary>
        public Guid OwnerEntityId { get; set; } // Settable for Jarvis hydration

        /// <summary>
        /// When the audit log was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Version number for snapshot tracking. Increments on each update.
        /// </summary>
        public int? Version { get; set; }
    }

    /// <summary>
    /// Represents a single entry in the EventAuditLog, capturing metadata about an emitted event.
    /// </summary>
    /// <param name="EventType">The simple name of the event type (e.g., "ClientPolicyUpdatedEvent").</param>
    /// <param name="TimestampUtc">When the event was registered/triggered.</param>
    /// <param name="PayloadSummary">A concise, non-sensitive summary of the event's key data.</param>
    /// <param name="TriggeredBy">Optional identifier for the user or system process triggering the event.</param>
    public record EmittedEventRecord(
        string EventType,
        DateTime TimestampUtc,
        string PayloadSummary,
        string? TriggeredBy = null // Nullable, as it might be system-generated
    );
}