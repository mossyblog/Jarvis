using FastEndpoints;
using core.jarvis.api.Extensions;
using core.jarvis.api.Models;
using core.jarvis.api.Services;
using core.jarvis.Data;
using core.jarvis.Data.Query;

namespace core.jarvis.api.Features.Account.Navigation;

public class Endpoint : EndpointWithoutRequest<List<NavigationItem>>
{
    public IDataContext DataContext { get; set; } = null!;
    public IPermissionService PermissionService { get; set; } = null!;

    public override void Configure()
    {
        Get("/accounts/navigation");
        Description(d => d
            .WithTags("Account")
            .Produces<List<NavigationItem>>(200)
            .Produces(401));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        // Get all navigation items
        var allNavItems = await DataContext.Query()
            .WithAll<NavigationItem>(Filter<NavigationItem>.All())
            .ToEntityComponents();

        var navigationItems = allNavItems
            .Select(kvp => kvp.Value.Get<NavigationItem>())
            .Where(n => n != null && n.IsActive)
            .OrderBy(n => n!.SortOrder)
            .ToList();

        // Filter by permissions
        var userPermissions = await PermissionService.GetUserPermissionsAsync(userId);

        var accessibleItems = navigationItems
            .Where(n => n!.RequiredPermissionId == null ||
                       userPermissions.Any(p => p == n.RequiredPermissionId.Value.ToString()))
            .Select(n => n!)
            .ToList();

        await SendAsync(accessibleItems, cancellation: ct);
    }
}
