namespace FJarvis.Data.Traits;

public struct Entity : IEntity
{
    public EntityId Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}