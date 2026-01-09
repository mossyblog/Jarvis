using FastEndpoints;
using core.jarvis.api.Models;
using core.jarvis.api.Systems;
using core.jarvis.Exceptions;

namespace core.jarvis.api.Features.Auth.Validate;

public class Endpoint : Endpoint<ValidateTokenRequest, TokenValidationResult>
{
    public AuthSystem AuthSystem { get; set; } = null!;

    public override void Configure()
    {
        Verbs(Http.GET, Http.POST);
        Routes("/security/validate");
        AllowAnonymous();
        Description(d => d
            .WithTags("Security")
            .Produces<TokenValidationResult>(200)
            .Produces(401));
    }

    public override async Task HandleAsync(ValidateTokenRequest req, CancellationToken ct)
    {
        // For GET requests, try to get token from Authorization header
        if (HttpContext.Request.Method == "GET" && string.IsNullOrEmpty(req.Token))
        {
            var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                req = new ValidateTokenRequest { Token = authHeader.Substring(7) };
            }
        }

        if (string.IsNullOrEmpty(req.Token))
        {
            AddError("Token is required");
            await SendErrorsAsync(400, ct);
            return;
        }

        try
        {
            var result = await AuthSystem.ValidateToken(req.Token);
            await SendAsync(result, cancellation: ct);
        }
        catch (UnauthorizedException)
        {
            await SendUnauthorizedAsync(ct);
        }
    }
}
