using FastEndpoints;
using core.jarvis.api.Models;
using core.jarvis.api.Attributes;
using core.jarvis.Data;
using core.jarvis.Data.Query;

namespace core.jarvis.api.Features.Roles.Index;

[RequirePermission("admin.roles.read")]
public class Endpoint : EndpointWithoutRequest<List<Role>>
{
    public IDataContext DataContext { get; set; } = null!;

    public override void Configure()
    {
        Get("/security/roles");
        Description(d => d
            .WithTags("Security")
            .Produces<List<Role>>(200)
            .Produces(401)
            .Produces(403));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var allRoles = await DataContext.Query()
            .WithAll<Role>(Filter<Role>.All())
            .ToEntityComponents();

        var roles = allRoles
            .Select(kvp => kvp.Value.Get<Role>())
            .Where(r => r != null)
            .Select(r => r!)
            .OrderBy(r => r.Name)
            .ToList();

        await SendAsync(roles, cancellation: ct);
    }
}
