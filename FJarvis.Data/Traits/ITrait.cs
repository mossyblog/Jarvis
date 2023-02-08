namespace FJarvis.Data.Traits;

public interface ITrait
{
    int Index { get; }
    int Version { get; set; }
    
    DateTime LastUpdated { get; set; }
}