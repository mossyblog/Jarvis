namespace core.jarvis.Data;

/// <summary>
/// Base class for all components, replacing Supabase.Postgrest.Models.BaseModel.
/// Properties are automatically mapped to snake_case by PgTable.
/// </summary>
public abstract class BaseComponent : IComponent
{
    /// <inheritdoc/>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <inheritdoc/>
    public Guid OwnerEntityId { get; set; }
    
    /// <inheritdoc/>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}