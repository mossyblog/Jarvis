using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component for audit logging of UIStudio operations.
/// Tracks all user actions, system events, and security-related activities.
/// Table: ui_studio_audit_log (automatic snake_case mapping)
/// </summary>
public record UIStudioAuditLog : IComponent
{
    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The entity this component belongs to.
    /// Required by IComponent interface.
    /// </summary>
    public Guid OwnerEntityId { get; set; }

    /// <summary>
    /// Entity ID of the user who performed the action.
    /// Maps to user_entity_id in database.
    /// </summary>
    public Guid? UserEntityId { get; init; }

    /// <summary>
    /// Type of action performed: "create", "update", "delete", "view", "share", "publish".
    /// Maps to action_type in database.
    /// </summary>
    public string ActionType { get; init; } = string.Empty;

    /// <summary>
    /// Entity ID of the resource that was acted upon.
    /// Maps to resource_entity_id in database.
    /// </summary>
    public Guid? ResourceEntityId { get; init; }

    /// <summary>
    /// Type of resource: "page", "layout", "component_binding", "template", "permission".
    /// Maps to resource_type in database.
    /// </summary>
    public string? ResourceType { get; init; }

    /// <summary>
    /// Human-readable description of the action.
    /// Maps to action_description in database.
    /// </summary>
    public string ActionDescription { get; init; } = string.Empty;

    /// <summary>
    /// Details of the action stored as JSON.
    /// Can include before/after values, metadata, etc.
    /// Maps to action_details in database.
    /// </summary>
    public Dictionary<string, object>? ActionDetails { get; init; }

    /// <summary>
    /// IP address from which the action was performed.
    /// Maps to ip_address in database.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// User agent string of the client.
    /// Maps to user_agent in database.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Session ID if available.
    /// Maps to session_id in database.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Whether the action was successful.
    /// Maps to is_success in database.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Error message if the action failed.
    /// Maps to error_message in database.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Security level of the action: "low", "medium", "high", "critical".
    /// Maps to security_level in database.
    /// </summary>
    public string SecurityLevel { get; init; } = "low";

    /// <summary>
    /// Whether this action requires security review.
    /// Maps to requires_review in database.
    /// </summary>
    public bool RequiresReview { get; init; } = false;

    /// <summary>
    /// Source of the action: "web", "api", "system", "import".
    /// Maps to action_source in database.
    /// </summary>
    public string ActionSource { get; init; } = "web";

    /// <summary>
    /// Duration of the operation in milliseconds.
    /// Maps to duration_ms in database.
    /// </summary>
    public long? DurationMs { get; init; }

    /// <summary>
    /// Additional context or metadata stored as JSON.
    /// Maps to context_data in database.
    /// </summary>
    public Dictionary<string, object>? ContextData { get; init; }

    /// <summary>
    /// Correlation ID for tracking related actions.
    /// Maps to correlation_id in database.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// When the action occurred.
    /// Maps to occurred_at in database.
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When this audit log entry was last updated.
    /// Required by IComponent interface.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Retention period for this log entry in days.
    /// Maps to retention_days in database.
    /// </summary>
    public int RetentionDays { get; init; } = 365;

    /// <summary>
    /// When this log entry should be archived or deleted.
    /// Maps to delete_after in database.
    /// </summary>
    public DateTime DeleteAfter { get; init; } = DateTime.UtcNow.AddDays(365);
}