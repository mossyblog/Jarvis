namespace FJarvis.Data.Traits;

/// <summary>
///  Anytime a Trait is Created in Jarvis, it needs to be registered in this enum to reserve its slot in the TraitData
///  bitflag/bitmask.
///
///  This is important, as the TraitData is used to determine what Traits are registered to an Entity
///  and are used when JournalHeaders are sent via the network into various services.
///
///  If no Trait is registered to an Entity, the Entity will be deemed orphan once its transported into the network.
/// </summary>
public enum TraitDataIndex
{
    Unknown = -1,
    Flight = 0,
    Ticket = 1,
    Train  = 2,
    Coupon = 3,
    Customer = 4
}