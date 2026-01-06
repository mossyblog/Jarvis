using core.jarvis.Data;

namespace core.jarvis.tests.Components;

/// <summary>
/// Basic test component for unit and integration tests.
/// </summary>
public record TestComponent : IVersionedComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsActive { get; set; }
}
