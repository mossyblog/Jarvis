using FJarvis.Data.Traits;

namespace FJarvis.Data;

public struct Flight : ITrait
{
    public Flight()
    {
        Id = Guid.NewGuid();
    }
    
    public int FlightNumber { get; set; }
    public string Departure { get; set; }
    public string Arrival { get; set; }
    public string DepartureTime { get; set; }
    public string ArrivalTime { get; set; }
    public string Airline { get; set; }
    public string Gate { get; set; }
    public string Seat { get; set; }
    public string Status { get; set; }

    // This is the index of the Bitmask this trait is registered to
    public int Index => (int)FCBitmaskIndex.Flight;
    public Guid Id { get; set; }
    public int Version { get; set; }
    public DateTime LastUpdated { get; set; }
}