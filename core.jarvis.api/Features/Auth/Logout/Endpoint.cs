using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using core.jarvis.api.Extensions;
using core.jarvis.api.Handlers;
using core.jarvis.Data;

namespace core.jarvis.api.Features.Auth.Logout;

public class LogoutRequest
{
    public Guid SessionId { get; set; }
}

[Authorize]
public class Endpoint : Endpoint<LogoutRequest>
{
    public IDataContext DataContext { get; set; } = null!;

    public override void Configure()
    {
        Post("/security/deauth");
        Description(d => d
            .WithTags("Security")
            .Produces(200)
            .Produces(401));
    }

    public override async Task HandleAsync(LogoutRequest req, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var tokenHandler = DataContext.For<AuthTokenHandler>(userId);
        await tokenHandler.Deauthenticate();

        await SendOkAsync(ct);
    }
}
