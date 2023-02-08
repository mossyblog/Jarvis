namespace FJarvis.Data.Traits;

/// <summary>
///  Anytime a Trait is Created in Jarvis, it needs to be registered in this enum to reserve its slot in the EntityInfo
///  bitflag/bitmask.
///
///  This is important, as the EntityInfo is used to determine what Traits are registered to an Entity
///  and are used when JournalHeaders are sent via the network into various services.
///
///  ⚠️ If no Trait is registered to an Entity, the Entity will be deemed orphan once its transported into the network. ⚠️
/// </summary>
public enum FCBitmaskIndex
{
    // ⚠️ If this enum is selected, exceptions will be thrown. ⚠️
    Unknown = -1,
    
    // Reserved for RelationShipTrait
    RelationShip = 0,
    
    // Internal Flight Centre Trait Registrations:
    Flight = 1,
    Ticket = 2,
    Train  = 3,
    Coupon = 4,
    Customer = 5
}