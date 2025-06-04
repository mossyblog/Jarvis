namespace core.jarvis.Data.Components;

/// <summary>
/// Represents an audit event in the system.
/// Table: audit_event (automatic snake_case mapping)
/// </summary>
public class AuditEvent : BaseComponent
{
    /// <summary>
    /// Type of entity this event relates to.
    /// Maps to entity_type in database.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Type of event that occurred.
    /// Maps to event_type in database.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// User ID who triggered the event.
    /// Maps to user_id in database.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Previous value before the change (JSON).
    /// Maps to old_value in database.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value after the change (JSON).
    /// Maps to new_value in database.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Additional metadata about the event (JSON).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Transaction ID if part of a transaction.
    /// Maps to transaction_id in database.
    /// </summary>
    public string? TransactionId { get; set; }
    
    /// <summary>
    /// Version number for versioning support.
    /// </summary>
    public int? Version { get; set; }
}