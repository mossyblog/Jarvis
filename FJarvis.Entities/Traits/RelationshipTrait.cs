namespace FJarvis.Data.Traits;

public class RelationshipTrait : ITrait
{
    public RelationshipTrait()
    {
        Id = Guid.NewGuid();
        LastUpdated = DateTime.UtcNow;
    }
    public int Index => 1;
    public Guid Id { get; set; }
    public DateTime LastUpdated { get; set; }
    
    public Guid Parent { get; set; }
    public Guid Child { get; set; }
}