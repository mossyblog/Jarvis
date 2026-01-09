using FastEndpoints;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Attributes;
using core.jarvis.api.Extensions;
using core.jarvis.Data;
using core.jarvis.Data.Query;

namespace core.jarvis.api.Features.Roles.Update;

[RequirePermission("admin.roles.write")]
public class Endpoint : Endpoint<Role, Role>
{
    public IDataContext DataContext { get; set; } = null!;

    public override void Configure()
    {
        Put("/security/roles/{id}");
        Validator<Validator>();
        Description(d => d
            .WithTags("Security")
            .Produces<Role>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404));
    }

    public override async Task HandleAsync(Role req, CancellationToken ct)
    {
        var roleIdStr = Route<string>("id");
        if (string.IsNullOrEmpty(roleIdStr) || !Guid.TryParse(roleIdStr, out var roleId))
        {
            AddError("Invalid role ID");
            await SendErrorsAsync(400, ct);
            return;
        }

        // Check if role exists - query by role Id, not OwnerEntityId
        var existingRoles = await DataContext.Query()
            .WithAll<Role>(Filter<Role>.Eq(r => r.Id, roleId))
            .ToEntityComponents();

        if (!existingRoles.Any())
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // Verify the role belongs to the authenticated user's entity
        if (!User.TryGetUserId(out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        // Check that the authenticated user has authority over this role's entity
        var role = existingRoles.First().Value.Get<Role>();
        if (role?.OwnerEntityId != userId)
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var roleHandler = DataContext.For<RoleHandler>(roleId);
        var updated = await roleHandler.UpdateRole(req);
        await SendAsync(updated, cancellation: ct);
    }
}
