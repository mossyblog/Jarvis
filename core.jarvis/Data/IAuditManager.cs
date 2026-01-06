namespace core.jarvis.Data;

/// <summary>
/// Audit management interface for logging component changes and events.
/// </summary>
public interface IAuditManager
{
    Task LogChange<TComponent>(string eventType, Guid entityId, TComponent existing, TComponent updated) where TComponent : class;
    Task LogEvent(string eventType, Guid entityId, object data);
}
