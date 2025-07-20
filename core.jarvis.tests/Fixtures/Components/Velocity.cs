using core.jarvis.Data;

namespace core.jarvis.tests.Fixtures.Components;

/// <summary>
/// Velocity component for integration tests.
/// Table: velocity_component (automatic snake_case mapping)
/// </summary>
public record VelocityComponent : IComponent, IVersionedComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid OwnerEntityId { get; set; }

    public float DeltaX { get; set; }

    public float DeltaY { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    public int? Version { get; set; }
}