using FastEndpoints;
using core.jarvis.api.Models;
using core.jarvis.api.Systems;
using core.jarvis.Exceptions;

namespace core.jarvis.api.Features.Auth.Refresh;

public class Endpoint : Endpoint<RefreshTokenRequest, AuthToken>
{
    public AuthSystem AuthSystem { get; set; } = null!;

    public override void Configure()
    {
        Post("/security/refresh");
        AllowAnonymous();
        Description(d => d
            .WithTags("Security")
            .Produces<AuthToken>(200)
            .Produces(401));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        try
        {
            var newToken = await AuthSystem.RefreshToken(req.RefreshToken);
            await SendAsync(newToken, cancellation: ct);
        }
        catch (UnauthorizedException)
        {
            await SendUnauthorizedAsync(ct);
        }
    }
}
