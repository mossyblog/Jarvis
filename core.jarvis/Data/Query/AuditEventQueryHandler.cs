using core.jarvis.Data.Components;

namespace core.jarvis.Data.Query;

/// <summary>
/// Query handler for AuditEvent components.
/// </summary>
public class AuditEventQueryHandler : ComponentQueryHandler<AuditEvent>
{
    public AuditEventQueryHandler(IPgClient pgClient) : base(pgClient)
    {
    }
}