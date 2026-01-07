using FastEndpoints;
using core.jarvis.api.Models;
using core.jarvis.api.Systems;
using core.jarvis.api.Processors;
using core.jarvis.Exceptions;
using AccountModel = core.jarvis.api.Models.Account;

namespace core.jarvis.api.Features.Auth.Login;

public class Endpoint : Endpoint<AccountModel, AuthToken>
{
    public AuthSystem AuthSystem { get; set; } = null!;

    public override void Configure()
    {
        Post("/security/auth");
        AllowAnonymous();
        Description(d => d
            .WithTags("Security")
            .Produces<AuthToken>(200)
            .Produces(401)
            .Produces(400));
    }

    public override async Task HandleAsync(AccountModel req, CancellationToken ct)
    {
        // Check account lockout
        if (!string.IsNullOrEmpty(req.Email) &&
            await RateLimitingPreProcessor.IsAccountLockedAsync(req.Email, Logger))
        {
            await SendAsync(new AuthToken(), 429, ct);
            return;
        }

        // Enrich with request metadata
        req = req with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
        };

        try
        {
            var authToken = await AuthSystem.AuthenticateUser(req);
            await SendAsync(authToken, cancellation: ct);
        }
        catch (UnauthorizedException)
        {
            // Track failed attempt
            if (!string.IsNullOrEmpty(req.Email))
            {
                RateLimitingPreProcessor.TrackFailedAttempt(req.Email, Logger);
            }
            await SendUnauthorizedAsync(ct);
        }
    }
}
