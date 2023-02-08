namespace FJarvis.Data.Traits;

public interface ITrait
{
    int Index { get; }

    
    Guid Id { get; set; }
    DateTime LastUpdated { get; set; }
}