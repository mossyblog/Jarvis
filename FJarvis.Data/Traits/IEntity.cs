namespace FJarvis.Data.Traits;

public interface IEntity
{
    EntityId Id { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}