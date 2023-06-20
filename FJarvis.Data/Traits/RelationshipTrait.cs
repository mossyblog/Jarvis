namespace FJarvis.Data.Traits;

public class RelationshipTrait : ITrait
{
    public RelationshipTrait()
    {
        Id = Guid.NewGuid();
        LastUpdated = DateTime.UtcNow;
    }
    
    // ⚠️ CAUTION 
    // ----------------------------------------------------- //
    // Relationship Trait is Always Reserved to Slot 0. 
    public int Index => 0;
    // ----------------------------------------------------- // 
    
    public Guid Id { get; set; }
    public DateTime LastUpdated { get; set; }
    
    public Guid Parent { get; set; }
    public Guid Child { get; set; }
}