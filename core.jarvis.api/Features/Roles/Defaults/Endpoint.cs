using FastEndpoints;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.api.Attributes;
using core.jarvis.Data;
using core.jarvis.Data.Query;

namespace core.jarvis.api.Features.Roles.Defaults;

[RequirePermission("admin.roles.write")]
public class Endpoint : EndpointWithoutRequest<List<Role>>
{
    public IDataContext DataContext { get; set; } = null!;

    public override void Configure()
    {
        Post("/security/roles/ensure-defaults");
        Description(d => d
            .WithTags("Security")
            .Produces<List<Role>>(200)
            .Produces(401)
            .Produces(403));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var defaultRoles = new[]
        {
            new { Name = "superuser", Description = "Full system access" },
            new { Name = "dev", Description = "Developer access" },
            new { Name = "default", Description = "Default user role" }
        };

        var createdRoles = new List<Role>();

        foreach (var defaultRole in defaultRoles)
        {
            // Check if role already exists
            var existingRoles = await DataContext.Query()
                .WithAll<Role>(Filter<Role>.Eq(r => r.Name, defaultRole.Name))
                .ToEntityComponents();

            if (existingRoles.Any())
            {
                Logger.LogInformation("Role {RoleName} already exists", defaultRole.Name);
                createdRoles.Add(existingRoles.First().Value.Get<Role>()!);
                continue;
            }

            // Create the role
            var roleEntityId = Guid.NewGuid();
            var roleHandler = DataContext.For<RoleHandler>(roleEntityId);
            var role = await roleHandler.CreateRole(new Role
            {
                Name = defaultRole.Name,
                Description = defaultRole.Description,
                PermissionIds = Array.Empty<string>()
            });

            Logger.LogInformation("Created default role: {RoleName}", defaultRole.Name);
            createdRoles.Add(role);
        }

        await SendAsync(createdRoles, cancellation: ct);
    }
}
