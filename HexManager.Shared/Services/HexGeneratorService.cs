using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;

namespace HexManager.Services;

public class HexGeneratorService : IHexGeneratorService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HexGeneratorService> _logger;
    private static readonly Regex HexPattern = new Regex(@"^[0-9A-Fa-f]{4}$", RegexOptions.Compiled);
    
    private readonly HashSet<string> _generatedHexesInSession = new();

    private string? _externalCsvPath;

    public HexGeneratorService(IConfiguration configuration, ILogger<HexGeneratorService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Initial load from configuration (if any)
        _externalCsvPath = _configuration["CsvSettings:ExternalFilePath"];
    }

    public Task SetExternalCsvPathAsync(string? path)
    {
        _externalCsvPath = path;
        return Task.CompletedTask;
    }

    public Task<string?> GetExternalCsvPathAsync() => Task.FromResult(_externalCsvPath);

    public bool IsValidHexAddress(string hexAddress)
    {
        if (string.IsNullOrWhiteSpace(hexAddress))
            return false;

        return HexPattern.IsMatch(hexAddress);
    }

    public async Task<string> GenerateNextHexAddressAsync()
    {
        // Read existing hex addresses from internal CSV (primary source)
        var existingHexes = await ReadHexAddressesFromCsvAsync();

        // Read HEXes from external CSV if configured
        if (!string.IsNullOrWhiteSpace(_externalCsvPath))
        {
            try 
            {
                var externalHexes = await ReadHexAddressesFromCsvFileAsync(_externalCsvPath);
                existingHexes = existingHexes.Union(externalHexes).ToList();
                _logger.LogInformation("Included HEXes from external CSV: {Path}", _externalCsvPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to read external CSV at {Path}: {Error}", _externalCsvPath, ex.Message);
            }
        }

        // Combine with session-generated hexes
        var allExistingHexes = existingHexes.Union(_generatedHexesInSession).ToList();
        
        // Filter valid hex addresses and convert to integers
        var validHexValues = allExistingHexes
            .Where(h => IsValidHexAddress(h))
            .Select(h => Convert.ToInt32(h.Trim(), 16))
            .OrderBy(v => v)
            .Distinct()
            .ToList();

        string nextHex;
        if (validHexValues.Count == 0)
        {
            nextHex = "0001";
        }
        else
        {
            var maxValue = validHexValues.Max();
            var nextValue = maxValue + 1;

            if (nextValue > 0xFFFF)
            {
                var nextAvailable = FindFirstGap(validHexValues);
                if (nextAvailable.HasValue)
                {
                    nextHex = nextAvailable.Value.ToString("X4");
                }
                else
                {
                    throw new InvalidOperationException("No available HEX addresses. All 4-character combinations are used.");
                }
            }
            else
            {
                nextHex = nextValue.ToString("X4");
            }
        }
        
        // Add to session cache to avoid duplicates in same session
        _generatedHexesInSession.Add(nextHex);
        
        _logger.LogInformation("Generated next HEX address: {HexAddress}", nextHex);
        
        return nextHex;
    }

    public async Task<string> GetNextAvailableHexAsync()
    {
        return await GenerateNextHexAddressAsync();
    }

    public async Task<string> GenerateNextHexAddressFromFileAsync(string csvFilePath)
    {
        // Read existing hex addresses from the specified CSV file
        var existingHexes = await ReadHexAddressesFromCsvFileAsync(csvFilePath);

        // Filter valid hex addresses and convert to integers
        var validHexValues = existingHexes
            .Where(h => IsValidHexAddress(h))
            .Select(h => Convert.ToInt32(h, 16))
            .OrderBy(v => v)
            .ToList();

        if (validHexValues.Count == 0)
        {
            // Start from 0001 if no valid hex exists
            var firstHex = "0001";
            _logger.LogInformation("Generated next HEX address from file: {HexAddress}", firstHex);
            return firstHex;
        }

        var maxValue = validHexValues.Max();
        var nextValue = maxValue + 1;

        string nextHex;
        
        if (nextValue > 0xFFFF)
        {
            var nextAvailable = FindFirstGap(validHexValues);
            if (nextAvailable.HasValue)
            {
                nextHex = nextAvailable.Value.ToString("X4");
            }
            else
            {
                throw new InvalidOperationException("No available HEX addresses. All 4-character combinations are used.");
            }
        }
        else
        {
            nextHex = nextValue.ToString("X4");
        }
        
        _logger.LogInformation("Generated next HEX address from file: {HexAddress}", nextHex);
        
        return nextHex;
    }

    public async Task<List<string>> SuggestAvailableHexAddressesAsync(int count = 5)
    {
        // Read existing hex addresses from CSV file
        var existingHexes = await ReadHexAddressesFromCsvAsync();

        // Combine CSV hexes with session-generated hexes
        var allExistingHexes = existingHexes.Union(_generatedHexesInSession).ToList();

        var validHexValues = allExistingHexes
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

    /// <summary>
    /// Read all HEX addresses from the configured CSV file
    /// </summary>
    private async Task<List<string>> ReadHexAddressesFromCsvAsync()
    {
        var csvFilePath = _configuration["CsvSettings:SourceFilePath"];
        
        if (string.IsNullOrWhiteSpace(csvFilePath))
        {
            _logger.LogWarning("CSV file path not configured. Returning empty list.");
            return new List<string>();
        }

        // Normalize path separators for cross-platform compatibility
        csvFilePath = csvFilePath.Replace('\\', Path.DirectorySeparatorChar);

        if (!File.Exists(csvFilePath))
        {
            _logger.LogWarning("CSV file not found at path: {FilePath}. Returning empty list.", csvFilePath);
            return new List<string>();
        }

        var hexAddresses = new List<string>();

        try
        {
            await Task.Run(() =>
            {
                using var reader = new StreamReader(csvFilePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null
                });

                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    try
                    {
                        var hexValue = csv.GetField("ATCS_CABINET_ADDRESS_HEX") 
                            ?? csv.GetField("ASTC_CABINET_ADDRESS_HEX");
                        
                        if (!string.IsNullOrWhiteSpace(hexValue))
                        {
                            // Normalize: trim and uppercase
                            hexValue = hexValue.Trim().ToUpper();
                            
                            // Only add valid hex addresses
                            if (IsValidHexAddress(hexValue))
                            {
                                hexAddresses.Add(hexValue);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid rows
                        _logger.LogDebug("Skipped invalid row while reading CSV: {Error}", ex.Message);
                    }
                }
            });

            _logger.LogInformation("Read {Count} HEX addresses from CSV file: {FilePath}", hexAddresses.Count, csvFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading CSV file from path: {FilePath}", csvFilePath);
            throw new InvalidOperationException($"Failed to read CSV file: {ex.Message}", ex);
        }

        return hexAddresses;
    }

    /// <summary>
    /// Read all HEX addresses from a specific CSV file path
    /// </summary>
    private async Task<List<string>> ReadHexAddressesFromCsvFileAsync(string csvFilePath)
    {
        if (string.IsNullOrWhiteSpace(csvFilePath))
        {
            _logger.LogWarning("CSV file path is empty. Returning empty list.");
            return new List<string>();
        }

        // Normalize path separators for cross-platform compatibility
        csvFilePath = csvFilePath.Replace('\\', Path.DirectorySeparatorChar);

        if (!File.Exists(csvFilePath))
        {
            _logger.LogWarning("CSV file not found at path: {FilePath}. Returning empty list.", csvFilePath);
            return new List<string>();
        }

        var hexAddresses = new List<string>();

        try
        {
            await Task.Run(() =>
            {
                using var reader = new StreamReader(csvFilePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null
                });

                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    try
                    {
                        // Priority: ASTC_CABINET_ADDRESS_HEX (as per customer requirement)
                        var hexValue = csv.GetField("ASTC_CABINET_ADDRESS_HEX") 
                            ?? csv.GetField("ATCS_CABINET_ADDRESS_HEX");
                        
                        if (!string.IsNullOrWhiteSpace(hexValue))
                        {
                            // Normalize: trim and uppercase
                            hexValue = hexValue.Trim().ToUpper();
                            
                            // Only add valid hex addresses
                            if (IsValidHexAddress(hexValue))
                            {
                                hexAddresses.Add(hexValue);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid rows
                        _logger.LogDebug("Skipped invalid row while reading CSV: {Error}", ex.Message);
                    }
                }
            });

            _logger.LogInformation("Read {Count} HEX addresses from CSV file: {FilePath}", hexAddresses.Count, csvFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading CSV file from path: {FilePath}", csvFilePath);
            throw new InvalidOperationException($"Failed to read CSV file: {ex.Message}", ex);
        }

        return hexAddresses;
    }
}
