using core.jarvis.Data;
using core.jarvis.api.Models;
using core.jarvis.Exceptions;
using Microsoft.Extensions.Logging;
using core.jarvis.api.Functions.UIStudio;

namespace core.jarvis.api.Handlers;

/// <summary>
/// Handler for managing UIStudioAuditLog components.
/// Provides operations for creating and querying audit logs for UIStudio operations.
/// </summary>
public class UIStudioAuditLogHandler : ComponentHandler<UIStudioAuditLog>
{
    public UIStudioAuditLogHandler(
        IDataContext dataContext,
        ILogger<UIStudioAuditLogHandler> logger)
        : base(dataContext, logger)
    {
    }

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    /// <param name="auditLog">The audit log entry to create</param>
    /// <returns>The created audit log component</returns>
    /// <exception cref="ArgumentException">Thrown when audit log configuration is invalid</exception>
    public async Task<UIStudioAuditLog> CreateAuditLog(UIStudioAuditLog auditLog)
    {
        ValidateAuditLogConfiguration(auditLog);
        await DataContext.Commit(auditLog);
        return auditLog;
    }

    /// <summary>
    /// Logs a user action with detailed information.
    /// </summary>
    /// <param name="userEntityId">Entity ID of the user performing the action</param>
    /// <param name="actionType">Type of action performed</param>
    /// <param name="actionDescription">Description of the action</param>
    /// <param name="resourceEntityId">Entity ID of the resource acted upon (optional)</param>
    /// <param name="resourceType">Type of resource (optional)</param>
    /// <param name="actionDetails">Additional action details (optional)</param>
    /// <param name="ipAddress">IP address of the user (optional)</param>
    /// <param name="userAgent">User agent string (optional)</param>
    /// <param name="sessionId">Session ID (optional)</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="errorMessage">Error message if action failed (optional)</param>
    /// <param name="securityLevel">Security level of the action</param>
    /// <param name="actionSource">Source of the action</param>
    /// <param name="durationMs">Duration of the operation in milliseconds (optional)</param>
    /// <param name="contextData">Additional context data (optional)</param>
    /// <param name="correlationId">Correlation ID for tracking related actions (optional)</param>
    /// <returns>The created audit log component</returns>
    public async Task<UIStudioAuditLog> LogUserAction(
        Guid userEntityId,
        string actionType,
        string actionDescription,
        Guid? resourceEntityId = null,
        string? resourceType = null,
        Dictionary<string, object>? actionDetails = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? sessionId = null,
        bool isSuccess = true,
        string? errorMessage = null,
        string securityLevel = "low",
        string actionSource = "web",
        long? durationMs = null,
        Dictionary<string, object>? contextData = null,
        string? correlationId = null)
    {
        var auditLog = new UIStudioAuditLog
        {
            UserEntityId = userEntityId,
            ActionType = actionType,
            ActionDescription = actionDescription,
            ResourceEntityId = resourceEntityId,
            ResourceType = resourceType,
            ActionDetails = actionDetails,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionId = sessionId,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            SecurityLevel = securityLevel,
            ActionSource = actionSource,
            DurationMs = durationMs,
            ContextData = contextData,
            CorrelationId = correlationId
        };

        return await CreateAuditLog(auditLog);
    }

    /// <summary>
    /// Logs a system action.
    /// </summary>
    /// <param name="actionType">Type of system action</param>
    /// <param name="actionDescription">Description of the action</param>
    /// <param name="resourceEntityId">Entity ID of the resource acted upon (optional)</param>
    /// <param name="resourceType">Type of resource (optional)</param>
    /// <param name="actionDetails">Additional action details (optional)</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="errorMessage">Error message if action failed (optional)</param>
    /// <param name="securityLevel">Security level of the action</param>
    /// <param name="durationMs">Duration of the operation in milliseconds (optional)</param>
    /// <param name="contextData">Additional context data (optional)</param>
    /// <param name="correlationId">Correlation ID for tracking related actions (optional)</param>
    /// <returns>The created audit log component</returns>
    public async Task<UIStudioAuditLog> LogSystemAction(
        string actionType,
        string actionDescription,
        Guid? resourceEntityId = null,
        string? resourceType = null,
        Dictionary<string, object>? actionDetails = null,
        bool isSuccess = true,
        string? errorMessage = null,
        string securityLevel = "low",
        long? durationMs = null,
        Dictionary<string, object>? contextData = null,
        string? correlationId = null)
    {
        var auditLog = new UIStudioAuditLog
        {
            UserEntityId = null, // System actions don't have a user
            ActionType = actionType,
            ActionDescription = actionDescription,
            ResourceEntityId = resourceEntityId,
            ResourceType = resourceType,
            ActionDetails = actionDetails,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            SecurityLevel = securityLevel,
            ActionSource = "system",
            DurationMs = durationMs,
            ContextData = contextData,
            CorrelationId = correlationId
        };

        return await CreateAuditLog(auditLog);
    }

    /// <summary>
    /// Gets audit logs for a specific user.
    /// </summary>
    /// <param name="userEntityId">Entity ID of the user</param>
    /// <param name="startDate">Start date for filtering (optional)</param>
    /// <param name="endDate">End date for filtering (optional)</param>
    /// <param name="actionType">Action type filter (optional)</param>
    /// <param name="limit">Maximum number of records to return (optional)</param>
    /// <returns>List of audit logs for the user</returns>
    public async Task<List<UIStudioAuditLog>> GetUserAuditLogs(
        Guid userEntityId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? actionType = null,
        int? limit = null)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioAuditLog>(a => a.UserEntityId == userEntityId);

        if (startDate.HasValue)
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.OccurredAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.OccurredAt <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(actionType))
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.ActionType == actionType);
        }

        var results = await query.ToEntityComponents();
        var auditLogs = new List<UIStudioAuditLog>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioAuditLogHandler>(result.Key);
            var auditLog = await handler.Get();
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        var orderedLogs = auditLogs.OrderByDescending(a => a.OccurredAt);
        
        if (limit.HasValue)
        {
            return orderedLogs.Take(limit.Value).ToList();
        }

        return orderedLogs.ToList();
    }

    /// <summary>
    /// Gets audit logs for a specific resource.
    /// </summary>
    /// <param name="resourceEntityId">Entity ID of the resource</param>
    /// <param name="startDate">Start date for filtering (optional)</param>
    /// <param name="endDate">End date for filtering (optional)</param>
    /// <param name="actionType">Action type filter (optional)</param>
    /// <returns>List of audit logs for the resource</returns>
    public async Task<List<UIStudioAuditLog>> GetResourceAuditLogs(
        Guid resourceEntityId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? actionType = null)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioAuditLog>(a => a.ResourceEntityId == resourceEntityId);

        if (startDate.HasValue)
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.OccurredAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.OccurredAt <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(actionType))
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.ActionType == actionType);
        }

        var results = await query.ToEntityComponents();
        var auditLogs = new List<UIStudioAuditLog>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioAuditLogHandler>(result.Key);
            var auditLog = await handler.Get();
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        return auditLogs.OrderByDescending(a => a.OccurredAt).ToList();
    }

    /// <summary>
    /// Gets audit logs by security level.
    /// </summary>
    /// <param name="securityLevel">Security level to filter by</param>
    /// <param name="startDate">Start date for filtering (optional)</param>
    /// <param name="endDate">End date for filtering (optional)</param>
    /// <param name="requiresReview">Whether to only include logs requiring review (optional)</param>
    /// <returns>List of audit logs with the specified security level</returns>
    public async Task<List<UIStudioAuditLog>> GetAuditLogsBySecurityLevel(
        string securityLevel,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? requiresReview = null)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioAuditLog>(a => a.SecurityLevel == securityLevel);

        if (startDate.HasValue)
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.OccurredAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.OccurredAt <= endDate.Value);
        }

        if (requiresReview.HasValue)
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.RequiresReview == requiresReview.Value);
        }

        var results = await query.ToEntityComponents();
        var auditLogs = new List<UIStudioAuditLog>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioAuditLogHandler>(result.Key);
            var auditLog = await handler.Get();
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        return auditLogs.OrderByDescending(a => a.OccurredAt).ToList();
    }

    /// <summary>
    /// Gets failed audit logs (actions that were not successful).
    /// </summary>
    /// <param name="startDate">Start date for filtering (optional)</param>
    /// <param name="endDate">End date for filtering (optional)</param>
    /// <param name="actionType">Action type filter (optional)</param>
    /// <returns>List of failed audit logs</returns>
    public async Task<List<UIStudioAuditLog>> GetFailedAuditLogs(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? actionType = null)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioAuditLog>(a => !a.IsSuccess);

        if (startDate.HasValue)
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.OccurredAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.OccurredAt <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(actionType))
        {
            query = query.WithAll<UIStudioAuditLog>(a => a.ActionType == actionType);
        }

        var results = await query.ToEntityComponents();
        var auditLogs = new List<UIStudioAuditLog>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioAuditLogHandler>(result.Key);
            var auditLog = await handler.Get();
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        return auditLogs.OrderByDescending(a => a.OccurredAt).ToList();
    }

    /// <summary>
    /// Gets audit logs that are ready for cleanup based on retention policies.
    /// </summary>
    /// <returns>List of audit logs that can be archived or deleted</returns>
    public async Task<List<UIStudioAuditLog>> GetAuditLogsForCleanup()
    {
        var query = DataContext.Query()
            .WithAll<UIStudioAuditLog>(a => a.DeleteAfter <= DateTime.UtcNow);

        var results = await query.ToEntityComponents();
        var auditLogs = new List<UIStudioAuditLog>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioAuditLogHandler>(result.Key);
            var auditLog = await handler.Get();
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        return auditLogs.OrderBy(a => a.DeleteAfter).ToList();
    }

    /// <summary>
    /// Gets audit logs by correlation ID for tracking related actions.
    /// </summary>
    /// <param name="correlationId">Correlation ID to search for</param>
    /// <returns>List of related audit logs</returns>
    public async Task<List<UIStudioAuditLog>> GetAuditLogsByCorrelationId(string correlationId)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioAuditLog>(a => a.CorrelationId == correlationId);

        var results = await query.ToEntityComponents();
        var auditLogs = new List<UIStudioAuditLog>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioAuditLogHandler>(result.Key);
            var auditLog = await handler.Get();
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        return auditLogs.OrderBy(a => a.OccurredAt).ToList();
    }

    /// <summary>
    /// Gets user audit history with filters.
    /// </summary>
    /// <param name="userEntityId">Entity ID of the user</param>
    /// <param name="filters">Audit filters</param>
    /// <returns>List of audit logs for the user</returns>
    public async Task<List<UIStudioAuditLog>> GetUserAuditHistory(Guid userEntityId)
    {
        return await GetUserAuditLogs(
            userEntityId, 
            null, 
            null, 
            null, 
            100);
    }

    /// <summary>
    /// Gets resource audit history with filters.
    /// </summary>
    /// <param name="resourceEntityId">Entity ID of the resource</param>
    /// <param name="filters">Audit filters</param>
    /// <returns>List of audit logs for the resource</returns>
    public async Task<List<UIStudioAuditLog>> GetResourceAuditHistory(Guid resourceEntityId)
    {
        return await GetResourceAuditLogs(
            resourceEntityId, 
            null, 
            null, 
            null);
    }

    /// <summary>
    /// Gets security audit events with filters.
    /// </summary>
    /// <param name="filters">Security audit filters</param>
    /// <returns>List of security audit events</returns>
    public async Task<List<UIStudioAuditLog>> GetSecurityAuditEvents(SecurityAuditFilters filters)
    {
        return await GetAuditLogsBySecurityLevel(
            filters.SecurityLevel ?? "medium", 
            filters.StartDate, 
            filters.EndDate);
    }

    /// <summary>
    /// Generates an audit report.
    /// </summary>
    /// <param name="startDate">Start date for report</param>
    /// <param name="endDate">End date for report</param>
    /// <param name="includeUserActions">Include user actions</param>
    /// <param name="includeSystemActions">Include system actions</param>
    /// <param name="includeSecurityEvents">Include security events</param>
    /// <param name="actionTypes">Filter by action types</param>
    /// <param name="resourceTypes">Filter by resource types</param>
    /// <param name="userEntityIds">Filter by user entity IDs</param>
    /// <param name="securityLevels">Filter by security levels</param>
    /// <returns>Generated audit report</returns>
    public async Task<AuditReport> GenerateAuditReport(
        DateTime startDate,
        DateTime endDate,
        bool includeUserActions,
        bool includeSystemActions,
        bool includeSecurityEvents,
        List<string>? actionTypes = null,
        List<string>? resourceTypes = null,
        List<Guid>? userEntityIds = null,
        List<string>? securityLevels = null)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioAuditLog>(a => a.OccurredAt >= startDate && a.OccurredAt <= endDate);

        var results = await query.ToEntityComponents();
        var auditLogs = new List<UIStudioAuditLog>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioAuditLogHandler>(result.Key);
            var auditLog = await handler.Get();
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        // Apply filters
        var filteredLogs = auditLogs.Where(log =>
        {
            if (actionTypes?.Any() == true && !actionTypes.Contains(log.ActionType)) return false;
            if (resourceTypes?.Any() == true && !resourceTypes.Contains(log.ResourceType ?? "")) return false;
            if (userEntityIds?.Any() == true && (!log.UserEntityId.HasValue || !userEntityIds.Contains(log.UserEntityId.Value))) return false;
            if (securityLevels?.Any() == true && !securityLevels.Contains(log.SecurityLevel)) return false;
            return true;
        }).ToList();

        var report = new AuditReport
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEvents = filteredLogs.Count,
            GeneratedAt = DateTime.UtcNow,
            GeneratedByEntityId = Guid.Empty, // System generated
            Sections = new List<AuditReportSection>
            {
                new AuditReportSection
                {
                    SectionName = "All Events",
                    SectionType = "summary",
                    EventCount = filteredLogs.Count,
                    Events = filteredLogs.Take(100).ToList() // Limit to first 100 for performance
                }
            },
            Summary = new Dictionary<string, object>
            {
                { "total_events", filteredLogs.Count },
                { "successful_events", filteredLogs.Count(l => l.IsSuccess) },
                { "failed_events", filteredLogs.Count(l => !l.IsSuccess) },
                { "unique_users", filteredLogs.Where(l => l.UserEntityId.HasValue).Select(l => l.UserEntityId).Distinct().Count() }
            }
        };

        return report;
    }

    /// <summary>
    /// Gets audit statistics for a date range.
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Audit statistics</returns>
    public async Task<AuditStatistics> GetAuditStatistics(DateTime startDate, DateTime endDate)
    {
        var query = DataContext.Query()
            .WithAll<UIStudioAuditLog>(a => a.OccurredAt >= startDate && a.OccurredAt <= endDate);

        var results = await query.ToEntityComponents();
        var auditLogs = new List<UIStudioAuditLog>();

        foreach (var result in results)
        {
            var handler = DataContext.For<UIStudioAuditLogHandler>(result.Key);
            var auditLog = await handler.Get();
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        var statistics = new AuditStatistics
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEvents = auditLogs.Count,
            EventsByAction = auditLogs.GroupBy(l => l.ActionType).ToDictionary(g => g.Key, g => g.Count()),
            EventsByResourceType = auditLogs.Where(l => !string.IsNullOrEmpty(l.ResourceType)).GroupBy(l => l.ResourceType!).ToDictionary(g => g.Key, g => g.Count()),
            EventsBySecurityLevel = auditLogs.GroupBy(l => l.SecurityLevel).ToDictionary(g => g.Key, g => g.Count()),
            EventsByDay = auditLogs.GroupBy(l => l.OccurredAt.Date.ToString("yyyy-MM-dd")).ToDictionary(g => g.Key, g => g.Count()),
            UniqueUsers = auditLogs.Where(l => l.UserEntityId.HasValue).Select(l => l.UserEntityId!.Value).Distinct().Count(),
            SecurityEvents = auditLogs.Count(l => l.SecurityLevel != "low"),
            FailedOperations = auditLogs.Count(l => !l.IsSuccess)
        };

        return statistics;
    }

    /// <summary>
    /// Validates the audit log configuration.
    /// </summary>
    /// <param name="auditLog">Audit log to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    private static void ValidateAuditLogConfiguration(UIStudioAuditLog auditLog)
    {
        if (string.IsNullOrWhiteSpace(auditLog.ActionType))
        {
            throw new ArgumentException("Action type is required");
        }

        if (string.IsNullOrWhiteSpace(auditLog.ActionDescription))
        {
            throw new ArgumentException("Action description is required");
        }

        var validSecurityLevels = new[] { "low", "medium", "high", "critical" };
        if (!validSecurityLevels.Contains(auditLog.SecurityLevel.ToLowerInvariant()))
        {
            throw new ArgumentException($"Security level must be one of: {string.Join(", ", validSecurityLevels)}");
        }

        var validActionSources = new[] { "web", "api", "system", "import" };
        if (!validActionSources.Contains(auditLog.ActionSource.ToLowerInvariant()))
        {
            throw new ArgumentException($"Action source must be one of: {string.Join(", ", validActionSources)}");
        }

        if (auditLog.RetentionDays <= 0)
        {
            throw new ArgumentException("Retention days must be greater than 0");
        }
    }
}