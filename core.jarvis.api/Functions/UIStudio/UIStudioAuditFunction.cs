using System.Net;
using System.Text.Json;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Extensions;
using core.jarvis.api.Exceptions;
using core.jarvis.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace core.jarvis.api.Functions.UIStudio;

/// <summary>
/// Azure Functions for UIStudio audit logging and history tracking.
/// Provides comprehensive audit trail for all UIStudio operations.
/// </summary>
public class UIStudioAuditFunction
{
    private readonly IDataContext _dataContext;
    private readonly ILogger<UIStudioAuditFunction> _logger;

    public UIStudioAuditFunction(IDataContext dataContext, ILogger<UIStudioAuditFunction> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/uistudio/audit/users/{userEntityId}/actions
    /// Gets audit history for a specific user.
    /// </summary>
    [Function("GetUserAuditHistory")]
    [OpenApiOperation(operationId: "getUserAuditHistory", tags: new[] { "UIStudio", "Audit" }, Summary = "Get user audit history", Description = "Retrieves audit trail for actions performed by a specific user.")]
    [OpenApiParameter(name: "userEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the user")]
    [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Maximum number of entries to return")]
    [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Number of entries to skip")]
    [OpenApiParameter(name: "actionType", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter by action type")]
    [OpenApiParameter(name: "resourceType", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter by resource type")]
    [OpenApiParameter(name: "startDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "Start date for filtering")]
    [OpenApiParameter(name: "endDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "End date for filtering")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<UIStudioAuditLog>), Summary = "Audit history retrieved successfully")]
    public async Task<HttpResponseData> GetUserAuditHistory(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/audit/users/{userEntityId}/actions")] HttpRequestData req,
        string userEntityId)
    {
        try
        {
            if (!Guid.TryParse(userEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid userEntityId format");
            }

            // Parse query parameters
            var queryParams = req.Query;
            var filters = new AuditFilters
            {
                Limit = queryParams["limit"] != null && int.TryParse(queryParams["limit"], out var l) ? l : 100,
                Offset = queryParams["offset"] != null && int.TryParse(queryParams["offset"], out var o) ? o : 0,
                ActionType = queryParams["actionType"],
                ResourceType = queryParams["resourceType"],
                StartDate = queryParams["startDate"] != null && DateTime.TryParse(queryParams["startDate"], out var sd) ? sd : null,
                EndDate = queryParams["endDate"] != null && DateTime.TryParse(queryParams["endDate"], out var ed) ? ed : null
            };

            // Get audit logs
            var auditHandler = _dataContext.For<UIStudioAuditLogHandler>(Guid.NewGuid());
            var auditLogs = await auditHandler.GetUserAuditHistory(entityId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(auditLogs.Cast<IComponent>().ToList()));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user audit history for {UserEntityId}", userEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get user audit history");
        }
    }

    /// <summary>
    /// GET /api/uistudio/audit/resources/{resourceEntityId}/history
    /// Gets audit history for a specific resource.
    /// </summary>
    [Function("GetResourceAuditHistory")]
    [OpenApiOperation(operationId: "getResourceAuditHistory", tags: new[] { "UIStudio", "Audit" }, Summary = "Get resource audit history", Description = "Retrieves audit trail for actions performed on a specific resource.")]
    [OpenApiParameter(name: "resourceEntityId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Entity ID of the resource")]
    [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Maximum number of entries to return")]
    [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Number of entries to skip")]
    [OpenApiParameter(name: "actionType", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter by action type")]
    [OpenApiParameter(name: "startDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "Start date for filtering")]
    [OpenApiParameter(name: "endDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "End date for filtering")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<UIStudioAuditLog>), Summary = "Resource audit history retrieved successfully")]
    public async Task<HttpResponseData> GetResourceAuditHistory(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/audit/resources/{resourceEntityId}/history")] HttpRequestData req,
        string resourceEntityId)
    {
        try
        {
            if (!Guid.TryParse(resourceEntityId, out var entityId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid resourceEntityId format");
            }

            // Parse query parameters
            var queryParams = req.Query;
            var filters = new AuditFilters
            {
                Limit = queryParams["limit"] != null && int.TryParse(queryParams["limit"], out var l) ? l : 100,
                Offset = queryParams["offset"] != null && int.TryParse(queryParams["offset"], out var o) ? o : 0,
                ActionType = queryParams["actionType"],
                StartDate = queryParams["startDate"] != null && DateTime.TryParse(queryParams["startDate"], out var sd) ? sd : null,
                EndDate = queryParams["endDate"] != null && DateTime.TryParse(queryParams["endDate"], out var ed) ? ed : null
            };

            // Get audit logs
            var auditHandler = _dataContext.For<UIStudioAuditLogHandler>(Guid.NewGuid());
            var auditLogs = await auditHandler.GetResourceAuditHistory(entityId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(auditLogs));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource audit history for {ResourceEntityId}", resourceEntityId);
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get resource audit history");
        }
    }

    /// <summary>
    /// GET /api/uistudio/audit/security/events
    /// Gets security-related audit events.
    /// </summary>
    [Function("GetSecurityAuditEvents")]
    [OpenApiOperation(operationId: "getSecurityAuditEvents", tags: new[] { "UIStudio", "Audit", "Security" }, Summary = "Get security audit events", Description = "Retrieves security-related audit events for monitoring and compliance.")]
    [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Maximum number of entries to return")]
    [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Number of entries to skip")]
    [OpenApiParameter(name: "securityLevel", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Filter by security level (low, medium, high)")]
    [OpenApiParameter(name: "startDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "Start date for filtering")]
    [OpenApiParameter(name: "endDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "End date for filtering")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<UIStudioAuditLog>), Summary = "Security events retrieved successfully")]
    public async Task<HttpResponseData> GetSecurityAuditEvents(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/audit/security/events")] HttpRequestData req)
    {
        try
        {
            // Parse query parameters
            var queryParams = req.Query;
            var filters = new SecurityAuditFilters
            {
                Limit = queryParams["limit"] != null && int.TryParse(queryParams["limit"], out var l) ? l : 100,
                Offset = queryParams["offset"] != null && int.TryParse(queryParams["offset"], out var o) ? o : 0,
                SecurityLevel = queryParams["securityLevel"],
                StartDate = queryParams["startDate"] != null && DateTime.TryParse(queryParams["startDate"], out var sd) ? sd : null,
                EndDate = queryParams["endDate"] != null && DateTime.TryParse(queryParams["endDate"], out var ed) ? ed : null
            };

            // Get security audit logs
            var auditHandler = _dataContext.For<UIStudioAuditLogHandler>(Guid.NewGuid());
            var securityEvents = await auditHandler.GetSecurityAuditEvents(filters);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(securityEvents));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security audit events");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get security audit events");
        }
    }

    /// <summary>
    /// POST /api/uistudio/audit/reports/generate
    /// Generates an audit report for a specified time period and criteria.
    /// </summary>
    [Function("GenerateAuditReport")]
    [OpenApiOperation(operationId: "generateAuditReport", tags: new[] { "UIStudio", "Audit" }, Summary = "Generate audit report", Description = "Generates a comprehensive audit report for specified criteria and time period.")]
    [OpenApiRequestBody("application/json", typeof(GenerateAuditReportRequest), Required = true, Description = "Report generation parameters")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuditReport), Summary = "Audit report generated successfully")]
    public async Task<HttpResponseData> GenerateAuditReport(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uistudio/audit/reports/generate")] HttpRequestData req)
    {
        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            GenerateAuditReportRequest reportRequest;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                reportRequest = System.Text.Json.JsonSerializer.Deserialize<GenerateAuditReportRequest>(requestBody, options)
                    ?? throw new ValidationException("Invalid request body");
            }
            catch (System.Text.Json.JsonException ex)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON: {ex.Message}");
            }

            // Validate request
            if (reportRequest.RequestedByEntityId == Guid.Empty)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "RequestedByEntityId is required");
            }

            // Generate audit report
            var auditHandler = _dataContext.For<UIStudioAuditLogHandler>(Guid.NewGuid());
            var report = await auditHandler.GenerateAuditReport(
                reportRequest.StartDate ?? DateTime.UtcNow.AddDays(-30),
                reportRequest.EndDate ?? DateTime.UtcNow,
                reportRequest.IncludeUserActions ?? true,
                reportRequest.IncludeSystemActions ?? true,
                reportRequest.IncludeSecurityEvents ?? true,
                reportRequest.ActionTypes,
                reportRequest.ResourceTypes,
                reportRequest.UserEntityIds,
                reportRequest.SecurityLevels);

            // Log report generation
            await LogAuditReportGeneration(reportRequest.RequestedByEntityId, report);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(report));
            return response;
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Audit report generation validation failed: {Message}", vex.Message);
            return await req.CreateValidationErrorResponse(vex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating audit report");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to generate audit report");
        }
    }

    /// <summary>
    /// GET /api/uistudio/audit/statistics
    /// Gets audit statistics and metrics.
    /// </summary>
    [Function("GetAuditStatistics")]
    [OpenApiOperation(operationId: "getAuditStatistics", tags: new[] { "UIStudio", "Audit" }, Summary = "Get audit statistics", Description = "Retrieves audit statistics and metrics for dashboard and monitoring.")]
    [OpenApiParameter(name: "timeframe", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Timeframe for statistics (day, week, month, year)")]
    [OpenApiParameter(name: "startDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "Start date for custom timeframe")]
    [OpenApiParameter(name: "endDate", In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Description = "End date for custom timeframe")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuditStatistics), Summary = "Audit statistics retrieved successfully")]
    public async Task<HttpResponseData> GetAuditStatistics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uistudio/audit/statistics")] HttpRequestData req)
    {
        try
        {
            // Parse query parameters
            var queryParams = req.Query;
            var timeframe = queryParams["timeframe"] ?? "week";
            
            DateTime startDate, endDate;
            if (queryParams["startDate"] != null && queryParams["endDate"] != null)
            {
                if (!DateTime.TryParse(queryParams["startDate"], out startDate) ||
                    !DateTime.TryParse(queryParams["endDate"], out endDate))
                {
                    return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid date format");
                }
            }
            else
            {
                (startDate, endDate) = GetTimeframeDates(timeframe);
            }

            // Get audit statistics
            var auditHandler = _dataContext.For<UIStudioAuditLogHandler>(Guid.NewGuid());
            var statistics = await auditHandler.GetAuditStatistics(startDate, endDate);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(statistics));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit statistics");
            return await req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to get audit statistics");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Logs the generation of an audit report for tracking purposes.
    /// </summary>
    private async Task LogAuditReportGeneration(Guid requestedByEntityId, AuditReport report)
    {
        try
        {
            var auditEntity = _dataContext.NewEntity();
            var auditHandler = _dataContext.For<UIStudioAuditLogHandler>(auditEntity.Id);

            await auditHandler.LogUserAction(
                requestedByEntityId,
                "report_generate",
                $"Generated audit report covering {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}",
                actionDetails: new Dictionary<string, object>
                {
                    { "report_period_days", (report.EndDate - report.StartDate).TotalDays },
                    { "total_events_included", report.TotalEvents },
                    { "report_sections", report.Sections?.Count ?? 0 }
                },
                securityLevel: "medium");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit report generation");
            // Don't throw - audit logging failure shouldn't break the main operation
        }
    }

    /// <summary>
    /// Gets start and end dates for predefined timeframes.
    /// </summary>
    private static (DateTime startDate, DateTime endDate) GetTimeframeDates(string timeframe)
    {
        var now = DateTime.UtcNow;
        var endDate = now;
        
        var startDate = timeframe.ToLower() switch
        {
            "day" => now.Date,
            "week" => now.Date.AddDays(-7),
            "month" => now.Date.AddDays(-30),
            "year" => now.Date.AddDays(-365),
            _ => now.Date.AddDays(-7) // Default to week
        };

        return (startDate, endDate);
    }

    #endregion
}

#region Request/Response Models

public class AuditFilters
{
    public int Limit { get; set; } = 100;
    public int Offset { get; set; } = 0;
    public string? ActionType { get; set; }
    public string? ResourceType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class SecurityAuditFilters
{
    public int Limit { get; set; } = 100;
    public int Offset { get; set; } = 0;
    public string? SecurityLevel { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class GenerateAuditReportRequest
{
    public Guid RequestedByEntityId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IncludeUserActions { get; set; }
    public bool? IncludeSystemActions { get; set; }
    public bool? IncludeSecurityEvents { get; set; }
    public List<string>? ActionTypes { get; set; }
    public List<string>? ResourceTypes { get; set; }
    public List<Guid>? UserEntityIds { get; set; }
    public List<string>? SecurityLevels { get; set; }
}

public class AuditReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEvents { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Guid GeneratedByEntityId { get; set; }
    public List<AuditReportSection>? Sections { get; set; }
    public Dictionary<string, object>? Summary { get; set; }
}

public class AuditReportSection
{
    public string SectionName { get; set; } = string.Empty;
    public string SectionType { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public List<UIStudioAuditLog>? Events { get; set; }
    public Dictionary<string, object>? Metrics { get; set; }
}

public class AuditStatistics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventsByAction { get; set; } = new();
    public Dictionary<string, int> EventsByResourceType { get; set; } = new();
    public Dictionary<string, int> EventsBySecurityLevel { get; set; } = new();
    public Dictionary<string, int> EventsByUser { get; set; } = new();
    public Dictionary<string, int> EventsByDay { get; set; } = new();
    public int UniqueUsers { get; set; }
    public int SecurityEvents { get; set; }
    public int FailedOperations { get; set; }
}

#endregion