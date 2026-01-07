using core.jarvis.api.Services;
using core.jarvis.Data;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for security audit events.
/// </summary>
public class SecurityAuditHandler : ComponentHandler<SecurityAuditEvent>
{
    public SecurityAuditHandler(IDataContext dataContext, ILogger<SecurityAuditHandler> logger)
        : base(dataContext, logger) { }

    public async Task LogAuthenticationFailed(string email, string ipAddress, string? userAgent, string? reason)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OwnerEntityId = OwnerEntityId,
            EventType = "AUTHENTICATION_FAILED",
            EventTime = DateTime.UtcNow,
            TargetEmail = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Reason = reason,
            Severity = "MEDIUM"
        };
        await TryCommit(auditEvent);
    }

    public async Task LogAuthenticationSuccess(string email, string ipAddress, string? userAgent)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OwnerEntityId = OwnerEntityId,
            EventType = "AUTHENTICATION_SUCCESS",
            EventTime = DateTime.UtcNow,
            TargetEmail = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Severity = "INFO"
        };
        await TryCommit(auditEvent);
    }

    public async Task LogPasswordChanged(string ipAddress)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OwnerEntityId = OwnerEntityId,
            EventType = "PASSWORD_CHANGED",
            EventTime = DateTime.UtcNow,
            IpAddress = ipAddress,
            Severity = "MEDIUM"
        };
        await TryCommit(auditEvent);
    }

    public async Task LogAccountLocked(string email, int failedAttempts, DateTime lockedUntil)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OwnerEntityId = OwnerEntityId,
            EventType = "ACCOUNT_LOCKED",
            EventTime = DateTime.UtcNow,
            TargetEmail = email,
            FailedAttempts = failedAttempts,
            LockedUntil = lockedUntil,
            Severity = "HIGH"
        };
        await TryCommit(auditEvent);
    }

    public async Task LogTokenRefreshed(Guid sessionId, string ipAddress)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OwnerEntityId = OwnerEntityId,
            EventType = "TOKEN_REFRESHED",
            EventTime = DateTime.UtcNow,
            SessionId = sessionId,
            IpAddress = ipAddress,
            Severity = "INFO"
        };
        await TryCommit(auditEvent);
    }

    public async Task LogTokenRevoked(Guid sessionId, string reason)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OwnerEntityId = OwnerEntityId,
            EventType = "TOKEN_REVOKED",
            EventTime = DateTime.UtcNow,
            SessionId = sessionId,
            Reason = reason,
            Severity = "MEDIUM"
        };
        await TryCommit(auditEvent);
    }

    public async Task LogSuspiciousActivity(string activityType, string details, string? ipAddress)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OwnerEntityId = OwnerEntityId,
            EventType = $"SUSPICIOUS_{activityType.ToUpperInvariant()}",
            EventTime = DateTime.UtcNow,
            IpAddress = ipAddress,
            Reason = details,
            Severity = "CRITICAL"
        };
        await TryCommit(auditEvent);
    }

    public async Task LogRoleChanged(Guid targetUserId, string action, string roleId)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OwnerEntityId = OwnerEntityId,
            EventType = $"ROLE_{action.ToUpperInvariant()}",
            EventTime = DateTime.UtcNow,
            TargetUserId = targetUserId,
            RoleId = roleId,
            Severity = "MEDIUM"
        };
        await TryCommit(auditEvent);
    }

    public async Task LogPermissionChanged(string action, string permissionId, string targetType, Guid targetId)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OwnerEntityId = OwnerEntityId,
            EventType = $"PERMISSION_{action.ToUpperInvariant()}",
            EventTime = DateTime.UtcNow,
            TargetUserId = targetId,
            PermissionId = permissionId,
            Reason = $"Target: {targetType}",
            Severity = "MEDIUM"
        };
        await TryCommit(auditEvent);
    }

    public async Task LogRoleUpdated(Guid roleId, string roleName, int permissionsChangedCount)
    {
        var auditEvent = new SecurityAuditEvent
        {
            OwnerEntityId = OwnerEntityId,
            EventType = "ROLE_UPDATED",
            EventTime = DateTime.UtcNow,
            RoleId = roleId.ToString(),
            Reason = $"Role '{roleName}' updated, permissions changed: {permissionsChangedCount}",
            Severity = "MEDIUM"
        };
        await TryCommit(auditEvent);
    }
}
