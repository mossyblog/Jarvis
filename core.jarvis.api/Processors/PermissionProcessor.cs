using FastEndpoints;
using core.jarvis.api.Attributes;
using core.jarvis.api.Extensions;
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

        // Get user ID from claims using safe extension that handles claim mapping
        if (!httpContext.User.TryGetUserId(out var userId))
        {
            await ctx.HttpContext.Response.SendUnauthorizedAsync(ct);
            return;
        }

        // Get permission service
        var permissionService = httpContext.Resolve<IPermissionService>();

        // Check permissions based on operator
        // If ANY attribute specifies And operator, require ALL permissions
        var permissions = permissionAttributes.Select(a => a.Permission).ToArray();
        var requireAll = permissionAttributes.Any(a => a.Operator == PermissionOperator.And);

        bool hasPermission;
        if (requireAll)
        {
            hasPermission = await permissionService.HasAllPermissionsAsync(userId, permissions);
        }
        else
        {
            hasPermission = await permissionService.HasAnyPermissionAsync(userId, permissions);
        }

        if (!hasPermission)
        {
            await ctx.HttpContext.Response.SendForbiddenAsync(ct);
            return;
        }
    }
}
