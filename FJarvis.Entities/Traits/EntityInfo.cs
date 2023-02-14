using System.Collections;
using Serilog;

namespace FJarvis.Data.Traits;

/// <summary>
/// This class is used to help determine what Traits are registered to an Entity and what traits are
/// not registered to an Entity.
/// 
/// This class will not retain the Trait itself, but will retain the index of the Trait.
/// 
/// Example:
/// <GUID:TRAIT>
/// "JournalHeaderTraits":  "a929d1ac-bdb6-4dd2-9124-8c945e2f0a55:[Flight(4), Ticket(2), Train(5), Coupon(1), Customer(63)]"
/// 
/// <GUID:Bitmask>
/// "JournalHeaderBitflags:"a929d1ac-bdb6-4dd2-9124-8c945e2f0a55:111111101111111000000111111111111110111111111111111111";
/// </summary>
public class EntityInfo
{

    // If the Entity is not passed a ILogger, throw an exception
    public EntityInfo()
    {
        throw new NotImplementedException( "This constructor is not implemented. Please use the constructor that takes a ILogger parameter.");
    }
    
    // Constructor
    public EntityInfo(ILogger logger)
    {
        // Set the size of the TraitData Bitmask to 64 bits
        Resize(64);

        // Store the reference to the Logger
        _logger = logger;
    }
    
    // By default the TraitData bitmask will be 64 bits
    BitArray bitFlags = new BitArray(64);
    
    private readonly ILogger _logger;

    // Identify the Entity this TraitData is registered to
    internal Guid EntityId { get; set; }
    private Dictionary<int, HashSet<Guid>> TraitsRegistry = new Dictionary<int, HashSet<Guid>>();

    // Get the size of the TraitData Bitmask
    internal int GetSize()
    {
        return bitFlags.Length;
    }

    /// <summary>
    ///  This method will Resize the Bitmask to the specified size.
    /// </summary>
    /// <param name="size"></param>
    /// <exception cref="Exception"></exception>
    internal void Resize(int size)
    {
        // Check to see if the size is greater than 64 bits
        if (size < 64)
        {
            // If the size is less than 64 bits, set the size to 64 bits
            size = 64;
        }
        
        // Check to see if the size is a multiple of 64 bits
        if (size % 64 != 0)
        {
            // If the size is not a multiple of 64 bits, increase the size to the next multiple of 64 bits
            size = ((size / 64) + 1) * 64;
        }
        
        // Check to see if the Size is less than the current size of the Bitmask
        if (size < bitFlags.Length)
        {
            // If the size is less than the current size of the Bitmask, return the current size of the Bitmask
            _logger.Error(
                ($"Cannot decrease the size of the Bitmask. The current size of the Bitmask is {bitFlags.Length}."));
            throw new Exception("Cannot decrease the size of the Bitmask");
        }
        
        // Set the size of the Bitmask
        bitFlags.Length = size;
    }
    
    /// <summary>
    /// Returns the Bitmask for this Entity
    /// </summary>
    /// <returns></returns>
    internal ulong GetBitmask()
    {
        ulong actualMask = 0;
        foreach (bool bit in bitFlags)
        {
            ulong bitNum = (ulong)(bit ? 1 : 0);
            actualMask = (actualMask << 1) | bitNum;
        }

        return actualMask;
    }
    
    /// <summary>
    ///  This method will remove all the Traits from the Entity and set all the bitmasks back to default state.
    /// </summary>
    internal void Clear()
    {
        // Clear the Bitmask
        bitFlags.SetAll(false);
        
        // Clear the Traits Registry
        TraitsRegistry.Clear();
    }
    
    /// <summary>
    ///  This method will register the Trait to the Entity
    /// </summary>
    /// <param name="id"></param>
    /// <param name="trait"></param>
    internal void RegisterTrait(Entity id, ITrait trait)
    {
        // Register the Entity
        SetEntityId(id);
        
        // Update the Traits Bitflag on _traits
        bitFlags.Set(trait.Index, true);
        
        // Determine if the Trait Index has been registered
        if (!TraitsRegistry.ContainsKey(trait.Index))
            // Add the Trait Index to the Traits Registry
            TraitsRegistry.Add(trait.Index, new HashSet<Guid>());

        // Determine if the Trait Id has already been added
        if (!TraitsRegistry[trait.Index].Contains(trait.Id))
            // Add the Trait Id to the Trait Index
            TraitsRegistry[trait.Index].Add(trait.Id);

    }


    /// <summary>
    ///  This method will remove the Trait from the Entity and will update the BitFlag for the Trait should there be no more Traits registered to the Entity.
    /// </summary>
    /// <param name="trait"></param>
    internal void RemoveTrait(ITrait trait)
    {
        // Determine if the Trait Index has been registered
        if (TraitsRegistry.ContainsKey(trait.Index))
        {
            // Determine if the Trait Id has already been added
            if (TraitsRegistry[trait.Index].Contains(trait.Id))
            {
                // Remove the Trait Id from the Trait Index
                TraitsRegistry[trait.Index].Remove(trait.Id);
            }
            // Set the Bitflag to true or false depending on if their are still traits remaining in the TraitRegistry
            bitFlags.Set(trait.Index, TraitsRegistry[trait.Index].Count > 0);
        }
        
        
    }
    
    /// <summary>
    ///  This method will set the EntityId to the EntityInfo
    /// </summary>
    /// <param name="entityId"></param>
    /// <exception cref="Exception"></exception>
    internal void SetEntityId(Entity entityId)
    {
        // Check to see if the Entity is already registered
        if (entityId.Id.Equals(Guid.Empty))
        {
            _logger.Fatal("An attempt to register an Entity to the Bitmask was made, but the Entity has no Id.");
            // If the Entity is already registered, return
            throw new Exception("An attempt to register an Entity to the Bitmask was made, but the Entity has no Id.");
        }
        // Register the Entity to the Bitmask
        EntityId = entityId.Id;
    }

    /// <summary>
    /// This method will validate that at least one Trait has been registered to an entity, or that the entity has been registered.
    /// </summary>
    /// <returns></returns>
    internal bool Validate()
    {
        return bitFlags.Cast<bool>().Any(b => b);
    }
    
    /// <summary>
    ///  This method the Bitflag for the Trait
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal bool GetBitFlag<T>() where T : ITrait
    {
        var trait = default(T);
        
        return bitFlags.Get(trait.Index);
    }

    /// <summary>
    /// Returns whether or not any traits have been assigned to this Entity.
    /// </summary>
    /// <returns></returns>
    internal bool HasTraits()
    {
        return bitFlags.Cast<bool>().Any(b => b);
    }

    /// <summary>
    /// Returns whether or not this Entity has any Traits with the specified Trait type
    /// </summary>
    /// <param name="slot"></param>
    /// <returns></returns>
    internal bool HasTraits(int slot)
    { 
        // How many traits are registered to the Entity via TraitsRegistry based on the Trait type
        return TraitsRegistry.Any(e => e.Key == slot);
    }
    
   
    /// <summary>
    /// Returns a count of the number of Traits that have been registered to the Entity
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal int Count<T>() where T : ITrait
    { 
        // Determine Slot index from Trait type
        var trait = default(T);
        
        // Determine if the Trait has even been registered

        if (!HasTraits(trait.Index))
            // If the Trait has not been registered, return 0
            return 0;
        
        // Determine how many Trait Ids are registered against a Trait Index
        return TraitsRegistry[trait.Index].Count;
        
    }
    
    /// <summary>
    ///  This method will return whether or not the Trait has been registered to the Entity
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    internal bool HasTrait(Guid id)
    {
        // Determine if the Trait Id has already been added
        return TraitsRegistry.Any(e => e.Value.Contains(id));
    }
    
    /// <summary>
    ///  This method will return whether or not the Trait has been registered to the Entity
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal HashSet<Guid> GetTraits<T>() where T : ITrait
    {
        // Determine Slot index from Trait type
        var trait = default(T);
        
        // Determine if the Trait has even been registered
        if (!HasTraits(trait.Index))
        {
            _logger.Warning($"The Trait {trait.GetType().Name} has not been registered to the Entity {EntityId}");
            
            // NOTE: I am not sure if this is the best way to handle this, but I am returning an empty HashSet<Guid>
            // to prevent a null reference exception.
            return new HashSet<Guid>();
        }

        // Return all traits of type T that are found in the TraitsRegistry
        return TraitsRegistry[trait.Index];
    }
}