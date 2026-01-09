using FastEndpoints;
using core.jarvis.api.Extensions;
using core.jarvis.api.Models;
using core.jarvis.Data;
using core.jarvis.Data.Query;

namespace core.jarvis.api.Features.Account.Profile;

public class Endpoint : EndpointWithoutRequest<SecurityProfile>
{
    public IDataContext DataContext { get; set; } = null!;

    public override void Configure()
    {
        Get("/accounts/me");
        Description(d => d
            .WithTags("Account")
            .Produces<SecurityProfile>(200)
            .Produces(401)
            .Produces(404));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var profiles = await DataContext.Query()
            .WithAll<SecurityProfile>(Filter<SecurityProfile>.Eq(p => p.OwnerEntityId, userId))
            .ToEntityComponents();

        var profile = profiles.Values.FirstOrDefault()?.Get<SecurityProfile>();

        if (profile == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(profile, cancellation: ct);
    }
}
