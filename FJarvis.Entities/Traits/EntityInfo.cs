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
    BitArray Bitmask = new BitArray(64);
    
    private readonly ILogger _logger;

    // Identify the Entity this TraitData is registered to
    public Entity EntityId { get; set; }
 
    // Get the size of the TraitData Bitmask
    public int GetSize()
    {
        return Bitmask.Length;
    }
    
    // Set the size of the TraitData Bitmask
    public void Resize(int size)
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
        if (size < Bitmask.Length)
        {
            // If the size is less than the current size of the Bitmask, return the current size of the Bitmask
            _logger.Error(
                ($"Cannot decrease the size of the Bitmask. The current size of the Bitmask is {Bitmask.Length}."));
            throw new Exception("Cannot decrease the size of the Bitmask");
        }
        
        // Set the size of the Bitmask
        Bitmask.Length = size;
    }
    
    /// <summary>
    /// Returns the Bitmask for this Entity
    /// </summary>
    /// <returns></returns>
    public ulong GetBitmask()
    {
        ulong actualMask = 0;
        foreach (bool bit in Bitmask)
        {
            ulong bitNum = (ulong)(bit ? 1 : 0);
            actualMask = (actualMask << 1) | bitNum;
        }

        return actualMask;
    }
    
    /// <summary>
    ///  This method will clear the Bitmask for this Entity, resulting in all Traits being BitFlagged as unselected.
    /// </summary>
    public void Clear()
    {
        // Clear the Bitmask
        Bitmask.SetAll(false);
    }
    
    /// <summary>
    ///  This method will register the Trait to the TraitData Bitmask.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="trait"></param>
    public void SetBitFlag(Entity id, ITrait trait)
    {
        // Register the Entity
        SetEntityId(id);
        
        // Update the Traits Bitflag on _traits
        Bitmask.Set(trait.Index, true);
    }
    
    /// <summary>
    ///  This method will set the EntityId to the EntityInfo
    /// </summary>
    /// <param name="entityId"></param>
    /// <exception cref="Exception"></exception>
    private void SetEntityId(Entity entityId)
    {
        // Check to see if the Entity is already registered
        if (entityId.Id.Equals(Guid.Empty))
        {
            _logger.Fatal("An attempt to register an Entity to the Bitmask was made, but the Entity has no Id.");
            // If the Entity is already registered, return
            throw new Exception("An attempt to register an Entity to the Bitmask was made, but the Entity has no Id.");
        }
        // Register the Entity to the Bitmask
        EntityId = entityId;
    }

    /// <summary>
    /// This method will validate that at least one Trait has been registered to an entity, or that the entity has been registered.
    /// </summary>
    /// <returns></returns>
    public bool Validate()
    {
        return Bitmask.Cast<bool>().Any(b => b);
    }
    
    /// <summary>
    ///  This method the Bitflag for the Trait
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool HasBitFlag<T>() where T : ITrait
    {
        var trait = default(T);
        return Bitmask.Get(trait.Index);
    }

    /// <summary>
    /// Returns whether or not any traits have been assigned to this Entity.
    /// </summary>
    /// <returns></returns>
    public bool HasTraits()
    {
        return Bitmask.Cast<bool>().Any(b => b);
    }
}