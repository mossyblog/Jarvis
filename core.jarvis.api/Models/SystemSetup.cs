using core.jarvis.Data;

namespace core.jarvis.api.Models;

/// <summary>
/// Component that tracks system setup state.
/// </summary>
public record SystemSetup : IComponent
{
    /// <inheritdoc/>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <inheritdoc/>
    public Guid OwnerEntityId { get; set; }
    
    /// <inheritdoc/>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if default roles have been created.
    /// </summary>
    public bool DefaultRolesCreated { get; init; }

    /// <summary>
    /// Indicates if default navigation has been created.
    /// </summary>
    public bool DefaultNavigationCreated { get; init; }

    /// <summary>
    /// Date when setup was last run.
    /// </summary>
    public DateTime LastSetupDate { get; init; } = DateTime.UtcNow;
}