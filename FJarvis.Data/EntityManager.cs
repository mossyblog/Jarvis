
using System.Runtime.CompilerServices;
using Autofac;
using FJarvis.Data.Data;
using FJarvis.Data.Traits;
using Serilog;

[assembly: InternalsVisibleTo("Jarvis.Entities.Tests")]

namespace FJarvis.Data;


public class EntityManager
{
    
    
    // This is a list of all the relationships between Traits
    private LinkedList<RelationshipTrait> relationshipList = new LinkedList<RelationshipTrait>();

    private readonly IComponentContext _context;
    private readonly ILogger _logger;
    private readonly JournalInfo _journalInfo;

    public EntityManager(IComponentContext context, ILogger logger, JournalInfo journalInfo)
    {
        _context = context;
        _logger = logger;
        _journalInfo = journalInfo;
    }

    /// <summary>
    ///  Creates a new Entity and registers it with the system.
    /// </summary>
    /// <param name="components"></param>
    /// <returns></returns>
    public Entity CreatEntity(params TraitDefinition[] components)
    {
        Entity entity = new Entity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Using IoC generates a new instance of EntityInfo
        var entityInfo = _context.Resolve<EntityInfo>();
        entityInfo.EntityId = entity.Id;
        entityInfo.UpdatedAt = DateTime.UtcNow;
        entityInfo.CreatedAt = DateTime.UtcNow;
        _journalInfo.Entities.Add(entityInfo);
        
        // Generate the HeaderInfo
        var headerInfo = new HeaderInfo();
        headerInfo.Bitmask = entityInfo.GetBitmask();
        headerInfo.EntityId = entityInfo.EntityId;
        _journalInfo.Headers.Add(headerInfo);
        
        _logger.Information("Entity Registered with the System: {EntityId}", entity.Id);
        return entity;
    }
    
    /// <summary>
    ///  Determines if the Entity has been registered with the system.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool EntityExists(Entity entity)
    {
        return _journalInfo.Entities.Any(e => e.EntityId == entity.Id);
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
            if (EntityExists(entity))
                entityInfo = _journalInfo.Entities.FirstOrDefault(e => e.EntityId == entity.Id);
           
            // Update the Bitmask to reflect that this entity has at least one Trait registered.
            // ⚠️ Important for network RPC calls.⚠️
            entityInfo.RegisterTrait(entity, trait);

            // A Trait can be attached to multiple Entities, therefore we only need to register the Trait once.
            if (!_journalInfo.Traits.Any(t => t.Id.Equals( trait.Id)))
            {
                // Register the Trait
                _journalInfo.Traits.Add(trait);
            }
        }

        UpdateJournal(entityInfo);

    }

    private void UpdateJournal(EntityInfo entityInfo)
    {
        // MEH.. do not like this.. 
        _journalInfo.Headers.RemoveWhere(e => e.EntityId == entityInfo.EntityId);
        var headerInfo = new HeaderInfo();
        headerInfo.EntityId = entityInfo.EntityId;
        headerInfo.Bitmask = entityInfo.GetBitmask();
        _journalInfo.Headers.Add(headerInfo);
    }


    /// <summary>
    ///  Removes all the Traits from an Entity. If the Entity has no Traits, It's bitfield is set to 0.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="traits"></param>
    public void RemoveTraitData(Entity entity, params ITrait[] traits)
    {
        var entityInfo = GetEntityInfo(entity);
        foreach (var trait in traits) 
            entityInfo.RemoveTrait(trait);
        
        UpdateJournal(entityInfo);
        
    }
    
    /// <summary>
    ///  Removes all the Traits from an Entity. If the Entity has no Traits, It's bitfield is set to 0.
    /// </summary>
    /// <param name="entity"></param>
    public void RemoveAllTraitData(Entity entity)
    {
        var entityInfo = GetEntityInfo(entity);
        entityInfo.Clear();
        
        UpdateJournal(entityInfo);
    }

    /// <summary>
    /// Returns all the Traits registered to an Entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public HashSet<T> GetTraits<T>(Entity entity) where T: ITrait
    {
        var entityInfo = GetEntityInfo(entity);
        var traitIds = entityInfo.GetTraits<T>();
        return _journalInfo.Traits.Where(t => traitIds.Contains(t.Id)).Cast<T>().ToHashSet();
    }
    
    // Set a Traits Parent
    public void SetParentTrait(ITrait trait, ITrait parent)
    {
        // Check to see if the Trait exists in the system
        if (!_journalInfo.Traits.Any(t => t.Id.Equals(trait.Id)))
            _logger.Error("SetParentTrait :: The Trait {TraitId} does not exist in the system.", trait.Id);

        // Check to see if the Parent Trait exists in the system
        if (!_journalInfo.Traits.Any(t => t.Id.Equals(parent.Id)))
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
        var entityInfo = GetEntityInfo(entity);

        // Check to see if the Entity has the Trait
        return entityInfo.GetBitFlag<T>();
    }

    // Returns the EntityInfo for an Entity if it exists in the system.
    internal EntityInfo GetEntityInfo(Entity entity)
    {
        return GetEntityInfo(entity.Id);
    }
    
    public EntityInfo GetEntityInfo(Guid id)
    {
        if (_journalInfo.Entities.Any(e => e.EntityId == id))
            return _journalInfo.Entities.First(e => e.EntityId == id);

        // If no Entity Found, log the error and throw an general exception.
        _logger.Warning($"GetEntityInfo ::  Entity {id} does not exist in the system.", id);
        throw new Exception($"GetEntityInfo ::  The Entity {id} does not exist in the system.");
    }
    
    public HashSet<IEntity> GetEntityQuery(EntityQueryDesc queryDesc)
    {
        var matchingEntities = new HashSet<IEntity>();

        foreach (var entityInfo in _journalInfo.Entities)
        {
            bool matchesAllTraits = queryDesc.All.Length >0;
            bool matchesAnyTraits = false;
            bool matchesNoneTraits = true;
            
            // Checks to see if the entity has ALL the traits required (ie AND statement)
            foreach (var traitType in queryDesc.All)
            {
                if (!entityInfo.HasTrait(traitType.Type))
                {
                    matchesAllTraits = false;
                    break;
                }
            }
            
            // Checks to see if the entity has ANY of the traits required (ie OR statement)
            foreach (var traitType in queryDesc.Any)
            {
                if (entityInfo.HasTrait(traitType.Type))
                {
                    matchesAnyTraits = true;
                }
            }
            
             
            // Checks to see if the entity has ANY of the traits required (ie OR statement)
            foreach (var traitType in queryDesc.None)
            {
                if (entityInfo.HasTrait(traitType.Type))
                {
                    matchesNoneTraits = false;
                    break;
                }
            }

            if ((matchesAllTraits || matchesAnyTraits) && matchesNoneTraits)
            {
                // TODO: Get the IEntity that corresponds to this EntityInfo.
                // Add it to the matchingEntities set.

                matchingEntities.Add(entityInfo.GetEntity());
            }
        }

        return matchingEntities;
    }


    /// <summary>
    ///  Returns all the Entities in the system.
    /// </summary>
    /// <returns></returns>
    public HashSet<IEntity> GetEntities()
    {
       // Return all the Entities in the system as Entity instead of EntityInfo
       return _journalInfo.Entities.Select(e => e.GetEntity()).ToHashSet();
    }

    /// <summary>
    ///  Returns the number of Entities in the system.
    /// </summary>
    /// <returns></returns>
    public int Count()
    {
        return _journalInfo.Entities.Count;
    }
}