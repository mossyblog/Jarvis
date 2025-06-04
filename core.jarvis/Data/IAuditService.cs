using core.jarvis.Data.Audit;

namespace core.jarvis.Data;

/// <summary>
/// Service for logging audit events.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs a simple audit event without value changes.
    /// </summary>
    /// <param name="eventType">The type of event.</param>
    /// <param name="entityId">The entity ID related to the event.</param>
    /// <param name="metadata">Optional metadata about the event.</param>
    Task LogEvent(string eventType, Guid entityId, object? metadata = null);

    /// <summary>
    /// Logs an audit event with before and after values.
    /// </summary>
    /// <typeparam name="T">The type of the values being tracked.</typeparam>
    /// <param name="eventType">The type of event.</param>
    /// <param name="entityId">The entity ID related to the event.</param>
    /// <param name="oldValue">The value before the change.</param>
    /// <param name="newValue">The value after the change.</param>
    /// <param name="metadata">Optional metadata about the event.</param>
    Task LogChange<T>(string eventType, Guid entityId, T oldValue, T newValue, object? metadata = null);
    
    /// <summary>
    /// Logs a security-related audit event.
    /// </summary>
    /// <param name="eventType">The type of security event.</param>
    /// <param name="context">The audit context with user information.</param>
    /// <param name="metadata">Optional metadata about the event.</param>
    Task LogSecurityEvent(string eventType, AuditContext context, object? metadata = null);
    
    /// <summary>
    /// Logs an error or exception as an audit event.
    /// </summary>
    /// <param name="eventType">The type of error event.</param>
    /// <param name="entityId">The entity ID related to the error.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="metadata">Optional metadata about the error.</param>
    Task LogError(string eventType, Guid entityId, Exception exception, object? metadata = null);
    
    /// <summary>
    /// Logs a batch operation audit event.
    /// </summary>
    /// <param name="eventType">The type of batch event.</param>
    /// <param name="entityIds">The entity IDs affected by the batch operation.</param>
    /// <param name="metadata">Optional metadata about the batch operation.</param>
    Task LogBatchEvent(string eventType, IEnumerable<Guid> entityIds, object? metadata = null);
}