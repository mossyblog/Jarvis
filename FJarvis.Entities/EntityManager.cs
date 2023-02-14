
using System.Runtime.CompilerServices;
using Autofac;
using FJarvis.Data.Traits;
using Serilog;

[assembly: InternalsVisibleTo("Jarvis.Entities.Tests")]

namespace FJarvis.Data;


public class EntityManager
{
    
    // This is a list of all the Entities registered in the system
    private  HashSet<EntityInfo> entities = new HashSet<EntityInfo>();
    
    // This is a list of all the Traits registered in the system
    private  HashSet<ITrait> traits = new HashSet<ITrait>();
    
    // This is a list of all the relationships between Traits
    private LinkedList<RelationshipTrait> relationshipList = new LinkedList<RelationshipTrait>();

    private readonly IComponentContext _context;
    private readonly ILogger _logger;

    public EntityManager(IComponentContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // Generates an empty Entity and registers it with the system.
    public Entity CreatEntity(params Archetype[] components)
    {
        Entity entity = new Entity();
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
    
        // Using IoC generates a new instance of EntityInfo
        var entityInfo = _context.Resolve<EntityInfo>();
        entityInfo.EntityId = entity;
        entities.Add(entityInfo);
        
        _logger.Information("Entity Registered with the System: {EntityId}", entity.Id);
        return entity;
    }
    
    // Returns whether the Entity has been registered with the system.
    public bool EntityExists(Entity entity)
    {
        return entities.Any(e => e.EntityId.Id == entity.Id);
    }


    /// <summary>
    ///   Adds a Trait to an Entity. An Entity can have more than one Trait.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="traits"></param>
    public void AddTraitData(Entity entity, params ITrait[]  traits)
    {
        // Using IoC generates a new instance of EntityInfo
        var entityInfo = _context.Resolve<EntityInfo>();
        
        // Register the Entity in the TraitMask
        foreach (var trait in traits)
        {
            // Check to see if the Entity itself exists in the EntityInfo
            if (entities.Any(e => e.EntityId.Id == entity.Id))
                entityInfo = entities.FirstOrDefault(e => e.EntityId.Id == entity.Id);
           
            // Update the Bitmask to reflect that this entity has at least one Trait registered.
            // ⚠️ Important for network RPC calls.⚠️
            entityInfo.SetBitFlag(entity, trait);
            
            
            // A Trait can be attached to multiple Entities, therefore we only need to register the Trait once.
            if (!this.traits.Any(t => t.Id.Equals( trait.Id)))
            {
                // Register the Trait
                this.traits.Add(trait);
            }
        }
    }
    
    // Set a Traits Parent
    public void SetParentTrait(ITrait trait, ITrait parent)
    {
        // Check to see if the Trait exists in the system
        if (!traits.Any(t => t.Id.Equals(trait.Id)))
            _logger.Error("SetParentTrait :: The Trait {TraitId} does not exist in the system.", trait.Id);

        // Check to see if the Parent Trait exists in the system
        if (!traits.Any(t => t.Id.Equals(parent.Id)))
            _logger.Error("SetParentTrait :: The Parent Trait {TraitId} does not exist in the system.", parent.Id);

        // Check to see if the Parent Trait is already a Parent of the Trait
        if (relationshipList.Any(r => r.Parent.Equals(parent.Id) && r.Child.Equals(trait.Id)))
            _logger.Warning("SetParentTrait :: The Parent Trait {TraitId} is already a Parent of the Trait {TraitId}.",
                parent.Id, trait.Id);

        // Create a new RelationshipTrait
        var relationshipTrait = new RelationshipTrait();
        relationshipTrait.Parent = parent.Id;
        relationshipTrait.Child = trait.Id;
        
        // Add the RelationshipTrait to the LinkedList
        relationshipList.AddLast(relationshipTrait);        
     
    }

    // Checks to see if an Entity has a Trait
    public bool HasTrait<T>(Entity entity) where T : ITrait
    {
        // Check to see if the Entity exists in the system
        if (entities.Any(e => e.EntityId.Id == entity.Id))
        {
            var entityInfo = entities.FirstOrDefault(e => e.EntityId.Id == entity.Id);

            // Check to see if the Entity has the Trait
            return entityInfo.HasBitFlag<T>();
        }

        // Get the EntityInfo for the Entity
        _logger.Warning("HasTrait :: Entity {EntityId} does not exist in the system.", entity.Id);
        return false;
    }

    // Returns the EntityInfo for an Entity if it exists in the system.
    internal EntityInfo GetEntityInfo(Entity entity)
    {
        if (entities.Any(e => e.EntityId.Id == entity.Id))
            return entities.First(e => e.EntityId.Id == entity.Id);

        // If no Entity Found, log the error and throw an general exception.
        _logger.Warning($"GetEntityInfo ::  Entity {entity.Id} does not exist in the system.", entity.Id);
        throw new Exception($"GetEntityInfo ::  The Entity {entity.Id} does not exist in the system.");
    }
}