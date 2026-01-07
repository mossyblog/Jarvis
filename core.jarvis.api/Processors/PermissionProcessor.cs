using FastEndpoints;
using core.jarvis.api.Attributes;
using core.jarvis.api.Services;

namespace core.jarvis.api.Processors;

/// <summary>
/// Pre-processor that checks RequirePermission attributes on endpoints.
/// </summary>
public class PermissionPreProcessor : IGlobalPreProcessor
{
    public async Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
    {
        var httpContext = ctx.HttpContext;
        var endpoint = httpContext.GetEndpoint();

        if (endpoint == null)
            return;

        // Get all RequirePermission attributes from the endpoint
        var permissionAttributes = endpoint.Metadata.GetOrderedMetadata<RequirePermissionAttribute>();
        if (permissionAttributes == null || !permissionAttributes.Any())
            return;

        // Must be authenticated
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            await ctx.HttpContext.Response.SendUnauthorizedAsync(ct);
            return;
        }

        // Get user ID from claims
        var userIdClaim = httpContext.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            await ctx.HttpContext.Response.SendUnauthorizedAsync(ct);
            return;
        }

        // Get permission service
        var permissionService = httpContext.Resolve<IPermissionService>();

        // Check permissions based on operator
        var permissions = permissionAttributes.Select(a => a.Permission).ToArray();
        var hasAnyPermission = await permissionService.HasAnyPermissionAsync(userId, permissions);

        if (!hasAnyPermission)
        {
            await ctx.HttpContext.Response.SendForbiddenAsync(ct);
        }
    }
}
