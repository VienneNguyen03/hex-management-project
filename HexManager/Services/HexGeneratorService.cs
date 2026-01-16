using System.Text.RegularExpressions;
using HexManager.Data;
using Microsoft.EntityFrameworkCore;

namespace HexManager.Services;

public class HexGeneratorService : IHexGeneratorService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HexGeneratorService> _logger;
    private static readonly Regex HexPattern = new Regex(@"^[0-9A-Fa-f]{4}$", RegexOptions.Compiled);

    public HexGeneratorService(ApplicationDbContext context, ILogger<HexGeneratorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public bool IsValidHexAddress(string hexAddress)
    {
        if (string.IsNullOrWhiteSpace(hexAddress))
            return false;

        return HexPattern.IsMatch(hexAddress);
    }

    public async Task<string> GenerateNextHexAddressAsync()
    {
        var existingHexes = await _context.TrafficSignals
            .Select(s => s.HexAddress)
            .Where(h => !string.IsNullOrEmpty(h))
            .ToListAsync();

        // Filter valid hex addresses and convert to integers
        var validHexValues = existingHexes
            .Where(h => IsValidHexAddress(h))
            .Select(h => Convert.ToInt32(h, 16))
            .OrderBy(v => v)
            .ToList();

        if (validHexValues.Count == 0)
        {
            // Start from 0001 if no valid hex exists
            return "0001";
        }

        // Get the highest value and increment
        var maxValue = validHexValues.Max();
        var nextValue = maxValue + 1;

        // Check if we've exceeded the 4-character hex limit (FFFF = 65535)
        if (nextValue > 0xFFFF)
        {
            // Find gaps in the sequence
            var nextAvailable = FindFirstGap(validHexValues);
            if (nextAvailable.HasValue)
            {
                return nextAvailable.Value.ToString("X4");
            }
            
            throw new InvalidOperationException("No available HEX addresses. All 4-character combinations are used.");
        }

        var nextHex = nextValue.ToString("X4");
        _logger.LogInformation("Generated next HEX address: {HexAddress}", nextHex);
        
        return nextHex;
    }

    public async Task<string> GetNextAvailableHexAsync()
    {
        return await GenerateNextHexAddressAsync();
    }

    public async Task<List<string>> SuggestAvailableHexAddressesAsync(int count = 5)
    {
        var existingHexes = await _context.TrafficSignals
            .Select(s => s.HexAddress)
            .Where(h => !string.IsNullOrEmpty(h))
            .ToListAsync();

        var validHexValues = existingHexes
            .Where(h => IsValidHexAddress(h))
            .Select(h => Convert.ToInt32(h, 16))
            .OrderBy(v => v)
            .ToHashSet();

        var suggestions = new List<string>();
        
        if (validHexValues.Count == 0)
        {
            // Suggest starting from 0001
            for (int i = 1; i <= count && i <= 0xFFFF; i++)
            {
                suggestions.Add(i.ToString("X4"));
            }
            return suggestions;
        }

        var maxValue = validHexValues.Max();
        
        // First, try to find gaps in the existing sequence
        for (int i = 1; i <= maxValue && suggestions.Count < count; i++)
        {
            if (!validHexValues.Contains(i))
            {
                suggestions.Add(i.ToString("X4"));
            }
        }

        // If we need more, add sequential values after the max
        var nextValue = maxValue + 1;
        while (suggestions.Count < count && nextValue <= 0xFFFF)
        {
            if (!validHexValues.Contains(nextValue))
            {
                suggestions.Add(nextValue.ToString("X4"));
            }
            nextValue++;
        }

        _logger.LogInformation("Suggested {Count} available HEX addresses", suggestions.Count);
        
        return suggestions;
    }

    private static int? FindFirstGap(List<int> sortedValues)
    {
        for (int i = 1; i < sortedValues.Count; i++)
        {
            var expectedNext = sortedValues[i - 1] + 1;
            if (sortedValues[i] != expectedNext)
            {
                // Found a gap
                return expectedNext;
            }
        }
        
        return null;
    }
}
