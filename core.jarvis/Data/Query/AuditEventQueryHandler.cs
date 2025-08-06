using core.jarvis.Data.Components;
using Microsoft.Extensions.Logging;

namespace core.jarvis.Data.Query;

/// <summary>
/// Query handler for AuditEvent components.
/// </summary>
public class AuditEventQueryHandler : ComponentQueryHandler<AuditEvent>
{
    public AuditEventQueryHandler(IPgClient pgClient, ILogger<ComponentQueryHandler<AuditEvent>> logger) : base(pgClient, logger)
    {
    }
}