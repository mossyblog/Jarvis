using core.jarvis.Data;

namespace core.jarvis.tests.Fixtures.Components;

/// <summary>
/// Position component for integration tests.
/// Table: position_component (automatic snake_case mapping)
/// </summary>
public record PositionComponent : IComponent, IVersionedComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
   
    public Guid OwnerEntityId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int? Version { get; set; }
}