using core.jarvis.Data;

namespace core.jarvis.tests.Components;

/// <summary>
/// Versioned test component for testing optimistic concurrency.
/// </summary>
public record VersionedTestComponent : IVersionedComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }
    public int? Version { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
