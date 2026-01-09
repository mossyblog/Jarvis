using Microsoft.Extensions.Logging;

namespace core.jarvis.Data;

/// <summary>
/// Manages audit logging for component changes.
/// </summary>
public class AuditManager : IAuditManager
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditManager> _logger;

    public AuditManager(IAuditService auditService, ILogger<AuditManager> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task LogChange<TComponent>(string eventType, Guid entityId, TComponent existing, TComponent updated)
        where TComponent : class
    {
        try
        {
            // Use LogChange to properly set OldValue and NewValue fields
            await _auditService.LogChange(eventType, entityId, existing, updated, new
            {
                ComponentType = typeof(TComponent).Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log change for {ComponentType}", typeof(TComponent).Name);
        }
    }

    public async Task LogEvent(string eventType, Guid entityId, object data)
    {
        try
        {
            await _auditService.LogEvent(eventType, entityId, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log event {EventType}", eventType);
        }
    }
}
