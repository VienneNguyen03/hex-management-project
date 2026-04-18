using HexManager.Models;

namespace HexManager.Services;

public interface ITrafficSignalService
{
    Task<List<TrafficSignal>> GetAllAsync();
    Task<PagedResult<TrafficSignal>> GetPagedAsync(SignalFilterOptions options);
    Task<List<TrafficSignal>> GetByBoroAsync(string boro);
    Task<TrafficSignal?> GetByIdAsync(int id);
    Task<TrafficSignal?> GetByHexAddressAsync(string hexAddress);
    Task<TrafficSignal> CreateAsync(TrafficSignal signal);
    Task<TrafficSignal> UpdateAsync(TrafficSignal signal);
    Task<bool> DeleteAsync(int id);
    Task<int> DeleteBatchAsync(List<int> ids);
    Task<bool> HexAddressExistsAsync(string hexAddress);
    Task<List<string>> GetAllHexAddressesAsync();
    Task<int> GetTotalCountAsync();
    Task<int> ImportFromCsvAsync(string filePath);
    Task<List<NearbySignal>> FindNearbySignalsAsync(double latitude, double longitude, double radiusKm = 20.0);
    Task<(double Latitude, double Longitude)?> GetCoordinatesByStreetNamesAsync(string street1, string street2);
}
