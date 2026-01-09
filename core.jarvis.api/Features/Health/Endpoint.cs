using FastEndpoints;

namespace core.jarvis.api.Features.Health;

/// <summary>
/// Health check endpoint
/// </summary>
public class Endpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
        Description(x => x
            .WithName("Health")
            .WithTags("System")
            .Produces<HealthResponse>(200));
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        return SendAsync(new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        }, cancellation: ct);
    }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
