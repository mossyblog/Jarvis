using core.jarvis.Data;

namespace core.jarvis.tests.Components;

/// <summary>
/// Velocity component for testing multi-component scenarios.
/// </summary>
public record VelocityComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }

    public double Vx { get; set; }
    public double Vy { get; set; }
    public double Vz { get; set; }
    public double DeltaX { get; set; }
    public double DeltaY { get; set; }
}
