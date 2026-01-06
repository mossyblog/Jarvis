using core.jarvis.Data;

namespace core.jarvis.tests.Components;

/// <summary>
/// Position component for testing multi-component scenarios.
/// </summary>
public record PositionComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}
