namespace FJarvis.Data.Traits;

public struct EntityId : IEntityId
{
    public int Index { get; set; }
    public int Version { get; set; }

    public EntityId(int index, int version)
    {
        Index = index;
        Version = version;
    }
   
}