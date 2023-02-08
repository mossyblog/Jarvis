namespace FJarvis.Data.Traits;

public interface IEntityId
{
    int Index { get; set; }
    int Version { get; set; }
}