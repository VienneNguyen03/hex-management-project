using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using HexManager.Services;

namespace HexManager.Tests
{
    public class HexGeneratorTests : IDisposable
    {
        private readonly string _tempCsvPath;
        private readonly HexGeneratorService _service;

        public HexGeneratorTests()
        {
            _tempCsvPath = Path.Combine(Path.GetTempPath(), $"test_hex_{Guid.NewGuid()}.csv");
            
            // Create a mock CSV with some data.
            // Priority: ASTC_CABINET_ADDRESS_HEX or ATCS_CABINET_ADDRESS_HEX
            var csvContent = @"ID,ASTC_CABINET_ADDRESS_HEX
1,0001
2,0002
3,0004
4,000A
";
            File.WriteAllText(_tempCsvPath, csvContent);

            var configMock = new Mock<IConfiguration>();
            // No internal CSV configured
            configMock.Setup(c => c["CsvSettings:SourceFilePath"]).Returns("");
            configMock.Setup(c => c["CsvSettings:ExternalFilePath"]).Returns(_tempCsvPath);

            var logger = new NullLogger<HexGeneratorService>();

            _service = new HexGeneratorService(configMock.Object, logger);
        }

        [Fact]
        public async Task GenerateNextHexAddressAsync_WithExternalCsv_GeneratesNextHexValue()
        {
            var nextHex = await _service.GenerateNextHexAddressAsync();

            // Based on existing hex data: 0001, 0002, 0004, 000A
            // Max is 000A (10), next should be 000B (11) if NextValue logic is used
            Assert.Equal("000B", nextHex);
            
            // Generate again, should be 000C
            var nextHex2 = await _service.GenerateNextHexAddressAsync();
            Assert.Equal("000C", nextHex2);
        }

        [Fact]
        public async Task SetExternalCsvPathAsync_UpdatesPathAndReadsIt()
        {
            var newPath = Path.Combine(Path.GetTempPath(), $"test_hex_{Guid.NewGuid()}.csv");
            var csvContent = @"ID,ATCS_CABINET_ADDRESS_HEX
1,00FF
";
            File.WriteAllText(newPath, csvContent);

            await _service.SetExternalCsvPathAsync(newPath);
            
            var nextHex = await _service.GenerateNextHexAddressAsync();
            
            // Max is 00FF (255), Next is 0100 (256)
            Assert.Equal("0100", nextHex);

            File.Delete(newPath);
        }

        [Fact]
        public async Task GenerateNextHexAddressFromFileAsync_ReturnsNextSequence()
        {
            var nextHex = await _service.GenerateNextHexAddressFromFileAsync(_tempCsvPath);
            Assert.Equal("000B", nextHex);
        }

        [Fact]
        public async Task GenerateNextHexAddressAsync_WhenNoDataExists_Returns0001()
        {
            // Setup a service with NO external CSV and NO internal CSV
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["CsvSettings:SourceFilePath"]).Returns("");
            configMock.Setup(c => c["CsvSettings:ExternalFilePath"]).Returns("");

            var emptyService = new HexGeneratorService(configMock.Object, new NullLogger<HexGeneratorService>());
            
            // Should return the very first hex "0001"
            var firstHex = await emptyService.GenerateNextHexAddressAsync();
            Assert.Equal("0001", firstHex);
            
            // Calling it again in the same session should increment to "0002" seamlessly
            var secondHex = await emptyService.GenerateNextHexAddressAsync();
            Assert.Equal("0002", secondHex);
        }
        
        public void Dispose()
        {
            if (File.Exists(_tempCsvPath))
            {
                File.Delete(_tempCsvPath);
            }
        }
    }
}
