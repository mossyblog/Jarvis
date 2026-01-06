using core.jarvis.Data;

namespace core.jarvis.tests.Components;

/// <summary>
/// Test component with required fields for validation testing.
/// </summary>
public record RequiredFieldTestComponent : IComponent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerEntityId { get; set; }
    public DateTime LastUpdated { get; set; }

    public string RequiredName { get; set; } = string.Empty;
    public string OptionalDescription { get; set; } = string.Empty;
}
