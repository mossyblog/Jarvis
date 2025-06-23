using System;
using System.Threading.Tasks;
using core.jarvis.api.Models;
using core.jarvis.Data;
using Microsoft.Extensions.Logging;

namespace core.jarvis.api.Services;

/// <summary>
/// Service for logging security-related events and audit trails.
/// </summary>
public interface ISecurityAuditService
{
    Task LogFailedAuthentication(string email, string ipAddress, string? userAgent = null, string? reason = null);
    Task LogSuccessfulAuthentication(Guid userId, string email, string ipAddress, string? userAgent = null);
    Task LogPasswordChange(Guid userId, string ipAddress);
    Task LogAccountLocked(string email, int failedAttempts, DateTime lockedUntil);
    Task LogTokenRefresh(Guid userId, Guid sessionId, string ipAddress);
    Task LogTokenRevoked(Guid userId, Guid sessionId, string reason);
    Task LogSuspiciousActivity(string activityType, string details, string? ipAddress = null, Guid? userId = null);
    Task LogRoleChange(Guid userId, Guid targetUserId, string action, string roleId);
    Task LogPermissionChange(Guid userId, string action, string permissionId, string targetType, Guid targetId);
}

public class SecurityAuditService : ISecurityAuditService
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(IDataContext dataContext, ILogger<SecurityAuditService> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    public async Task LogFailedAuthentication(string email, string ipAddress, string? userAgent = null, string? reason = null)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = "AUTHENTICATION_FAILED",
                EventTime = DateTime.UtcNow,
                Details = new
                {
                    email,
                    ipAddress,
                    userAgent,
                    reason = reason ?? "Invalid credentials"
                },
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Severity = "MEDIUM"
            };

            await _dataContext.Commit(auditEvent);
            
            _logger.LogWarning("Failed authentication attempt for {Email} from {IpAddress}", email, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log authentication failure");
        }
    }

    public async Task LogSuccessfulAuthentication(Guid userId, string email, string ipAddress, string? userAgent = null)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                OwnerEntityId = userId,
                EventType = "AUTHENTICATION_SUCCESS",
                EventTime = DateTime.UtcNow,
                Details = new
                {
                    email,
                    ipAddress,
                    userAgent
                },
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Severity = "INFO"
            };

            await _dataContext.Commit(auditEvent);
            
            _logger.LogInformation("Successful authentication for {Email} from {IpAddress}", email, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log successful authentication");
        }
    }

    public async Task LogPasswordChange(Guid userId, string ipAddress)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                OwnerEntityId = userId,
                EventType = "PASSWORD_CHANGED",
                EventTime = DateTime.UtcNow,
                Details = new
                {
                    ipAddress
                },
                IpAddress = ipAddress,
                Severity = "HIGH"
            };

            await _dataContext.Commit(auditEvent);
            
            _logger.LogInformation("Password changed for user {UserId} from {IpAddress}", userId, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log password change");
        }
    }

    public async Task LogAccountLocked(string email, int failedAttempts, DateTime lockedUntil)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = "ACCOUNT_LOCKED",
                EventTime = DateTime.UtcNow,
                Details = new
                {
                    email,
                    failedAttempts,
                    lockedUntil
                },
                Severity = "HIGH"
            };

            await _dataContext.Commit(auditEvent);
            
            _logger.LogWarning("Account {Email} locked until {LockedUntil} after {Attempts} failed attempts", 
                email, lockedUntil, failedAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log account lock");
        }
    }

    public async Task LogTokenRefresh(Guid userId, Guid sessionId, string ipAddress)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                OwnerEntityId = userId,
                EventType = "TOKEN_REFRESHED",
                EventTime = DateTime.UtcNow,
                Details = new
                {
                    sessionId,
                    ipAddress
                },
                IpAddress = ipAddress,
                Severity = "INFO"
            };

            await _dataContext.Commit(auditEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log token refresh");
        }
    }

    public async Task LogTokenRevoked(Guid userId, Guid sessionId, string reason)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                OwnerEntityId = userId,
                EventType = "TOKEN_REVOKED",
                EventTime = DateTime.UtcNow,
                Details = new
                {
                    sessionId,
                    reason
                },
                Severity = "MEDIUM"
            };

            await _dataContext.Commit(auditEvent);
            
            _logger.LogInformation("Token revoked for user {UserId}, session {SessionId}: {Reason}", 
                userId, sessionId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log token revocation");
        }
    }

    public async Task LogSuspiciousActivity(string activityType, string details, string? ipAddress = null, Guid? userId = null)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                OwnerEntityId = userId ?? Guid.Empty,
                EventType = $"SUSPICIOUS_{activityType.ToUpperInvariant()}",
                EventTime = DateTime.UtcNow,
                Details = new
                {
                    activityType,
                    details,
                    ipAddress
                },
                IpAddress = ipAddress,
                Severity = "CRITICAL"
            };

            await _dataContext.Commit(auditEvent);
            
            _logger.LogCritical("Suspicious activity detected: {Type} - {Details} from {IP}", 
                activityType, details, ipAddress ?? "unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log suspicious activity");
        }
    }

    public async Task LogRoleChange(Guid userId, Guid targetUserId, string action, string roleId)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                OwnerEntityId = userId,
                EventType = $"ROLE_{action.ToUpperInvariant()}",
                EventTime = DateTime.UtcNow,
                Details = new
                {
                    targetUserId,
                    action,
                    roleId
                },
                Severity = "HIGH"
            };

            await _dataContext.Commit(auditEvent);
            
            _logger.LogInformation("Role {Action} by {UserId} on {TargetUserId} for role {RoleId}", 
                action, userId, targetUserId, roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log role change");
        }
    }

    public async Task LogPermissionChange(Guid userId, string action, string permissionId, string targetType, Guid targetId)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                OwnerEntityId = userId,
                EventType = $"PERMISSION_{action.ToUpperInvariant()}",
                EventTime = DateTime.UtcNow,
                Details = new
                {
                    action,
                    permissionId,
                    targetType,
                    targetId
                },
                Severity = "HIGH"
            };

            await _dataContext.Commit(auditEvent);
            
            _logger.LogInformation("Permission {Action} by {UserId} on {TargetType} {TargetId} for permission {PermissionId}", 
                action, userId, targetType, targetId, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission change");
        }
    }
}

/// <summary>
/// Component for security audit events.
/// </summary>
public record SecurityAuditEvent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; } = Guid.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public string EventType { get; init; } = string.Empty;
    public DateTime EventTime { get; init; }
    public object? Details { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string Severity { get; init; } = "INFO"; // INFO, MEDIUM, HIGH, CRITICAL
}