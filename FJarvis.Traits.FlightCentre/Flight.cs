using FJarvis.Data.Traits;

namespace FJarvis.Data;

public struct Flight : ITrait
{
    public Flight()
    {
    }

    public int FlightNumber { get; set; } = 0;
    public string Departure { get; set; } = null;
    public string Arrival { get; set; } = null;
    public string DepartureTime { get; set; } = null;
    public string ArrivalTime { get; set; } = null;
    public string Airline { get; set; } = null;
    public string Gate { get; set; } = null;
    public string Seat { get; set; } = null;
    public string Status { get; set; } = null;

    // This is the index of the Bitmask this trait is registered to
    public int Index => (int)FCBitmaskIndex.Flight;
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Version { get; set; } = 0;
    public DateTime LastUpdated { get; set; } = default;
}