using core.jarvis.Data;

namespace core.jarvis.tests.Fixtures.Components;

/// <summary>
/// Test component for unit tests.
/// Table: test_component (automatic snake_case mapping)
/// </summary>
public record TestComponent : IComponent, IVersionedComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid OwnerEntityId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }

    public string Status { get; set; } = "ACTIVE";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int? Version { get; set; }
}