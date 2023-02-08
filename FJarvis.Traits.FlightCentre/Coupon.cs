using FJarvis.Data.Traits;

namespace FJarvis.Traits.FlightCentre;

public struct Coupon : ITrait
{
    public Coupon()
    {
        Id = Guid.NewGuid();
        LastUpdated = DateTime.UtcNow;
    }
    
    public int Index => (int)FCBitmaskIndex.Coupon;
    public Guid Id { get; set; }
    public DateTime LastUpdated { get; set; }
}