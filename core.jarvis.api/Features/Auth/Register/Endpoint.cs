using FastEndpoints;
using core.jarvis.api.Systems;
using core.jarvis.Data;
using core.jarvis.Exceptions;

namespace core.jarvis.api.Features.Auth.Register;

public class Endpoint : Endpoint<RegistrationRequest, List<IComponent>>
{
    public RegistrationSystem RegistrationSystem { get; set; } = null!;

    public override void Configure()
    {
        Post("/auth/register");
        AllowAnonymous();
        Description(d => d
            .WithTags("Auth")
            .Produces<List<IComponent>>(201)
            .Produces(400)
            .Produces(409));
    }

    public override async Task HandleAsync(RegistrationRequest req, CancellationToken ct)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var requestBody = System.Text.Json.JsonSerializer.Serialize(req);
            var components = await RegistrationSystem.RegisterUser(requestBody, ipAddress);
            await SendAsync(components, 201, ct);
        }
        catch (BusinessRuleException ex) when (ex.Code == "EMAIL_EXISTS")
        {
            AddError("Email already registered");
            await SendErrorsAsync(409, ct);
        }
        catch (ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                foreach (var message in error.Value)
                {
                    AddError(message, error.Key);
                }
            }
            await SendErrorsAsync(400, ct);
        }
    }
}
