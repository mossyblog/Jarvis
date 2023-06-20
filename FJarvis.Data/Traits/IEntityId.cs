namespace FJarvis.Data.Traits;

public interface IEntityId
{
    Guid Id { get; set; }
    int Index { get; set; }
    int Version { get; set; }
}