using FastEndpoints;
using core.jarvis.api.Models;
using core.jarvis.api.Handlers;
using core.jarvis.Data;

namespace core.jarvis.api.Features.Account.ProfileUpdate;

public class Endpoint : Endpoint<SecurityProfile, SecurityProfile>
{
    public IDataContext DataContext { get; set; } = null!;

    public override void Configure()
    {
        Put("/accounts/me");
        Description(d => d
            .WithTags("Account")
            .Produces<SecurityProfile>(200)
            .Produces(401)
            .Produces(404));
    }

    public override async Task HandleAsync(SecurityProfile req, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        // Only allow updating safe fields - user cannot change their own roles
        var safeUpdate = req with
        {
            OwnerEntityId = userId,
            RoleIds = Array.Empty<string>() // Clear role changes - protected field
        };

        var profileHandler = DataContext.For<AccountProfileHandler>(userId);
        var updated = await profileHandler.UpdateProfile(safeUpdate);

        await SendAsync(updated, cancellation: ct);
    }
}
