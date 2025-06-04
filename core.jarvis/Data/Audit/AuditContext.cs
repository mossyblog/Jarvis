using System.Security.Claims;

namespace core.jarvis.Data.Audit;

/// <summary>
/// Provides context information for audit operations.
/// </summary>
public class AuditContext
{
    /// <summary>
    /// Gets the current user ID from claims or system context.
    /// </summary>
    public string UserId { get; init; } = "SYSTEM";
    
    /// <summary>
    /// Gets the current user's email if available.
    /// </summary>
    public string? UserEmail { get; init; }
    
    /// <summary>
    /// Gets the current user's roles.
    /// </summary>
    public string[]? UserRoles { get; init; }
    
    /// <summary>
    /// Gets the tenant ID if in a multi-tenant context.
    /// </summary>
    public string? TenantId { get; init; }
    
    /// <summary>
    /// Gets the correlation ID for tracking related operations.
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets the source IP address if available.
    /// </summary>
    public string? SourceIp { get; init; }
    
    /// <summary>
    /// Gets the user agent if this is a web request.
    /// </summary>
    public string? UserAgent { get; init; }
    
    /// <summary>
    /// Creates an audit context from the current claims principal.
    /// </summary>
    public static AuditContext FromClaims(ClaimsPrincipal? principal)
    {
        if (principal == null)
            return new AuditContext();
            
        return new AuditContext
        {
            UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? principal.FindFirst("sub")?.Value 
                    ?? "SYSTEM",
            UserEmail = principal.FindFirst(ClaimTypes.Email)?.Value,
            UserRoles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
            TenantId = principal.FindFirst("tenant_id")?.Value
        };
    }
    
    /// <summary>
    /// Creates an audit context from JWT claims.
    /// </summary>
    public static AuditContext FromJwtClaims(IDictionary<string, string>? claims)
    {
        if (claims == null)
            return new AuditContext();
            
        return new AuditContext
        {
            UserId = claims.TryGetValue("sub", out var sub) ? sub : "SYSTEM",
            UserEmail = claims.TryGetValue("email", out var email) ? email : null,
            UserRoles = claims.TryGetValue("roles", out var roles) ? roles?.Split(',') : null,
            TenantId = claims.TryGetValue("tenant_id", out var tenantId) ? tenantId : null
        };
    }
}