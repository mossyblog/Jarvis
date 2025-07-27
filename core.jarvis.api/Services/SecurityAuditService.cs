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
    /// <summary>
    /// Logs a failed authentication attempt with contextual information.
    /// </summary>
    /// <param name="email">The email address used in the failed authentication attempt</param>
    /// <param name="ipAddress">The IP address of the client making the attempt</param>
    /// <param name="userAgent">Optional user agent string of the client</param>
    /// <param name="reason">Optional specific reason for the authentication failure</param>
    Task LogFailedAuthentication(string email, string ipAddress, string? userAgent = null, string? reason = null);
    
    /// <summary>
    /// Logs a successful authentication event.
    /// </summary>
    /// <param name="userId">The ID of the successfully authenticated user</param>
    /// <param name="email">The email address used for authentication</param>
    /// <param name="ipAddress">The IP address of the client</param>
    /// <param name="userAgent">Optional user agent string of the client</param>
    Task LogSuccessfulAuthentication(Guid userId, string email, string ipAddress, string? userAgent = null);
    
    /// <summary>
    /// Logs a password change event for security monitoring.
    /// </summary>
    /// <param name="userId">The ID of the user whose password was changed</param>
    /// <param name="ipAddress">The IP address where the password change originated</param>
    Task LogPasswordChange(Guid userId, string ipAddress);
    
    /// <summary>
    /// Logs an account lockout event due to excessive failed attempts.
    /// </summary>
    /// <param name="email">The email address of the locked account</param>
    /// <param name="failedAttempts">The number of failed attempts that triggered the lockout</param>
    /// <param name="lockedUntil">The timestamp when the account lockout expires</param>
    Task LogAccountLocked(string email, int failedAttempts, DateTime lockedUntil);
    
    /// <summary>
    /// Logs a token refresh event for session management monitoring.
    /// </summary>
    /// <param name="userId">The ID of the user whose token was refreshed</param>
    /// <param name="sessionId">The session ID associated with the refresh</param>
    /// <param name="ipAddress">The IP address where the refresh occurred</param>
    Task LogTokenRefresh(Guid userId, Guid sessionId, string ipAddress);
    
    /// <summary>
    /// Logs a token revocation event for security monitoring.
    /// </summary>
    /// <param name="userId">The ID of the user whose token was revoked</param>
    /// <param name="sessionId">The session ID that was revoked</param>
    /// <param name="reason">The reason for token revocation</param>
    Task LogTokenRevoked(Guid userId, Guid sessionId, string reason);
    
    /// <summary>
    /// Logs suspicious activity that may indicate security threats.
    /// </summary>
    /// <param name="activityType">The type of suspicious activity detected</param>
    /// <param name="details">Detailed description of the suspicious activity</param>
    /// <param name="ipAddress">Optional IP address associated with the activity</param>
    /// <param name="userId">Optional user ID if the activity is associated with a specific user</param>
    Task LogSuspiciousActivity(string activityType, string details, string? ipAddress = null, Guid? userId = null);
    
    /// <summary>
    /// Logs role changes for administrative audit trails.
    /// </summary>
    /// <param name="userId">The ID of the user making the role change</param>
    /// <param name="targetUserId">The ID of the user whose role is being changed</param>
    /// <param name="action">The action being performed (grant, revoke, etc.)</param>
    /// <param name="roleId">The ID of the role being modified</param>
    Task LogRoleChange(Guid userId, Guid targetUserId, string action, string roleId);
    
    /// <summary>
    /// Logs permission changes for detailed security auditing.
    /// </summary>
    /// <param name="userId">The ID of the user making the permission change</param>
    /// <param name="action">The action being performed (grant, revoke, etc.)</param>
    /// <param name="permissionId">The ID of the permission being modified</param>
    /// <param name="targetType">The type of target (user, role, etc.)</param>
    /// <param name="targetId">The ID of the target entity</param>
    Task LogPermissionChange(Guid userId, string action, string permissionId, string targetType, Guid targetId);
}

public class SecurityAuditService : ISecurityAuditService
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly bool _isTestEnvironment;

    public SecurityAuditService(IDataContext dataContext, ILogger<SecurityAuditService> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
        _isTestEnvironment = false; // Always enable audit logging for consistency
    }

    public async Task LogFailedAuthentication(string email, string ipAddress, string? userAgent = null, string? reason = null)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = "AUTHENTICATION_FAILED",
                EventTime = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Severity = "MEDIUM",
                TargetEmail = email,
                Reason = reason ?? "Invalid credentials"
            };

            await _dataContext.Commit(auditEvent);
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
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Severity = "INFO",
                TargetEmail = email
            };

            await _dataContext.Commit(auditEvent);
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
                Severity = "HIGH",
                TargetEmail = email,
                FailedAttempts = failedAttempts,
                LockedUntil = lockedUntil
            };

            await _dataContext.Commit(auditEvent);
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
                IpAddress = ipAddress,
                Severity = "INFO",
                SessionId = sessionId
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
                Severity = "MEDIUM",
                SessionId = sessionId,
                Reason = reason
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
                IpAddress = ipAddress,
                Severity = "CRITICAL",
                Reason = $"{activityType}: {details}"
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
                Severity = "HIGH",
                TargetUserId = targetUserId,
                RoleId = roleId
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
                Severity = "HIGH",
                PermissionId = permissionId,
                TargetUserId = targetId,
                Reason = $"{action} on {targetType}"
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
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public string EventType { get; init; } = string.Empty;
    public DateTime EventTime { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string Severity { get; init; } = "INFO"; // INFO, MEDIUM, HIGH, CRITICAL
    public string? Reason { get; init; } // For failed auth or other events
    public string? TargetEmail { get; init; } // Email being accessed
    public Guid? SessionId { get; init; } // For token operations
    public Guid? TargetUserId { get; init; } // For role/permission changes
    public string? RoleId { get; init; } // For role changes
    public string? PermissionId { get; init; } // For permission changes
    public int? FailedAttempts { get; init; } // For account lock events
    public DateTime? LockedUntil { get; init; } // For account lock events
}