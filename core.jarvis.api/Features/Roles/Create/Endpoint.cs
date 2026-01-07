using FastEndpoints;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Attributes;
using core.jarvis.Data;

namespace core.jarvis.api.Features.Roles.Create;

[RequirePermission("admin.roles.write")]
public class Endpoint : Endpoint<Role, Role>
{
    public IDataContext DataContext { get; set; } = null!;

    public override void Configure()
    {
        Post("/security/roles");
        Validator<Validator>();
        Description(d => d
            .WithTags("Security")
            .Produces<Role>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403));
    }

    public override async Task HandleAsync(Role req, CancellationToken ct)
    {
        var roleEntityId = Guid.NewGuid();
        var roleHandler = DataContext.For<RoleHandler>(roleEntityId);
        var role = await roleHandler.CreateRole(req);

        await SendAsync(role, 201, ct);
    }
}
