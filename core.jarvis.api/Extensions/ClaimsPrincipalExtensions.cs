using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace core.jarvis.api.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to handle JWT claim extraction safely.
///
/// SECURITY NOTE: JWT libraries may map short-form claim names to long-form URIs:
/// - "sub" -> ClaimTypes.NameIdentifier (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier)
/// - "email" -> ClaimTypes.Email (http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress)
/// - "role" -> ClaimTypes.Role (http://schemas.microsoft.com/ws/2008/06/identity/claims/role)
///
/// This mapping depends on the JWT handler configuration (MapInboundClaims setting).
/// These extension methods check BOTH forms to ensure claims are found regardless of mapping.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from JWT claims, checking both short-form (sub) and long-form (NameIdentifier).
    /// </summary>
    /// <param name="principal">The claims principal to extract from</param>
    /// <returns>The user ID string if found, null otherwise</returns>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Gets the user ID as a Guid from JWT claims.
    /// </summary>
    /// <param name="principal">The claims principal to extract from</param>
    /// <param name="userId">The parsed Guid if successful</param>
    /// <returns>True if a valid Guid was found, false otherwise</returns>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        var userIdString = principal.GetUserId();
        if (string.IsNullOrEmpty(userIdString))
        {
            userId = Guid.Empty;
            return false;
        }
        return Guid.TryParse(userIdString, out userId);
    }

    /// <summary>
    /// Gets the email from JWT claims, checking both short-form (email) and long-form (Email).
    /// </summary>
    /// <param name="principal">The claims principal to extract from</param>
    /// <returns>The email string if found, null otherwise</returns>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the roles from JWT claims, checking both custom (roles) and standard (Role) claim types.
    /// </summary>
    /// <param name="principal">The claims principal to extract from</param>
    /// <returns>Comma-separated roles string if found, null otherwise</returns>
    public static string? GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("roles")?.Value
            ?? principal.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Gets the session ID from JWT claims.
    /// Session ID is a custom claim and should not be mapped by JWT handlers.
    /// </summary>
    /// <param name="principal">The claims principal to extract from</param>
    /// <returns>The session ID string if found, null otherwise</returns>
    public static string? GetSessionId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("session_id")?.Value
            ?? principal.FindFirst("sessionId")?.Value;
    }

    /// <summary>
    /// Gets the expiration timestamp from JWT claims.
    /// </summary>
    /// <param name="principal">The claims principal to extract from</param>
    /// <returns>The expiration Unix timestamp string if found, null otherwise</returns>
    public static string? GetExpiration(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value
            ?? principal.FindFirst("exp")?.Value;
    }
}
