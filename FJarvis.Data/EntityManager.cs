using Autofac;
using FJarvis.Data.Traits;
using Serilog;

namespace FJarvis.Data;

public class EntityManager
{
    
    // Registry of EntityRegInfo objects
    private  HashSet<TraitData> traitdata = new HashSet<TraitData>();
    private  HashSet<ITrait> traits = new HashSet<ITrait>();
    
    private readonly IComponentContext _context;
    private static int Index;
    private static int Version;
    private readonly ILogger _logger;

    public EntityManager(IComponentContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // Genereates an empty Entity
    public Entity CreatEntity(params Archetype[] components)
    {
        Entity entity = new Entity();
        entity.Id = GetNextEntityId();
        _logger.Information("Entity Created: {EntityId}", entity.Id);
        return entity;
    }


    // Adds a Trait(s) to an Entity
    public void SetTraitData(Entity entity, params ITrait[]  traits)
    {
        // Create Empty TraitMask
        var traitInfo = _context.Resolve<TraitData>();
        
        // Register the Entity in the TraitMask
        foreach (var trait in traits)
        {
            // Check to see if the Entity exists in traitmasks
            if (traitdata.Any(e => e.EntityId.Index == entity.Id.Index))
                traitInfo = traitdata.FirstOrDefault(e => e.EntityId.Index == entity.Id.Index);
           
            // Update the Bitmask to reflect the Trait and Entity registration.
            traitInfo.SetBitFlag(entity.Id, trait);
            
            
            // A Trait can be attached to multiple Entities, therefore we only need to register the Trait once.
            if (!this.traits.Any(t => t.Index == trait.Index))
            {
                this.traits.Add(trait);
            }
            
        }
    }
    
    public EntityId GetNextEntityId()
    {
        EntityId id = new EntityId(EntityManager.Index, EntityManager.Version);
        EntityManager.Index++;
        EntityManager.Version++;
        return id;
    }

    public TraitData GetTraitData(Entity entity)
    {
        // Todo: Implement this
        return _context.Resolve<TraitData>();
    }
}