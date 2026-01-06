using core.jarvis.Data;

namespace core.jarvis.tests.Components;

/// <summary>
/// Navigation item component for testing hierarchical relationships.
/// </summary>
public record NavigationItemTestComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int Order { get; set; }
}
