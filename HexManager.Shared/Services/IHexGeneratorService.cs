namespace HexManager.Services;

public interface IHexGeneratorService
{
    /// <summary>
    /// Generate a new unique HEX address based on existing ones
    /// </summary>
    Task<string> GenerateNextHexAddressAsync();
    
    /// <summary>
    /// Generate a new unique HEX address from a specific CSV file path
    /// </summary>
    Task<string> GenerateNextHexAddressFromFileAsync(string csvFilePath);
    
    /// <summary>
    /// Validate if a HEX address is valid format (4 characters hex)
    /// </summary>
    bool IsValidHexAddress(string hexAddress);
    
    /// <summary>
    /// Get the next available HEX in sequence
    /// </summary>
    Task<string> GetNextAvailableHexAsync();
    
    /// <summary>
    /// Suggest multiple available HEX addresses
    /// </summary>
    Task<List<string>> SuggestAvailableHexAddressesAsync(int count = 5);

    /// <summary>
    /// Sets the external CSV path to check for duplicate HEX addresses
    /// </summary>
    Task SetExternalCsvPathAsync(string? path);

    /// <summary>
    /// Gets the current external CSV path
    /// </summary>
    Task<string?> GetExternalCsvPathAsync();
}
