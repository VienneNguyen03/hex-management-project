namespace HexManager.Models;

public class NearbySignal
{
    public TrafficSignal Signal { get; set; } = null!;
    public double DistanceKm { get; set; }
}
