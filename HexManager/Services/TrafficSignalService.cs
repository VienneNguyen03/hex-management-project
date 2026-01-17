using Microsoft.EntityFrameworkCore;
using HexManager.Data;
using HexManager.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace HexManager.Services;

public class TrafficSignalService : ITrafficSignalService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TrafficSignalService> _logger;

    public TrafficSignalService(ApplicationDbContext context, ILogger<TrafficSignalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TrafficSignal>> GetAllAsync()
    {
        return await _context.TrafficSignals
            .OrderBy(s => s.HexAddress)
            .ToListAsync();
    }

    public async Task<PagedResult<TrafficSignal>> GetPagedAsync(SignalFilterOptions options)
    {
        var query = _context.TrafficSignals.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(options.SearchTerm))
        {
            var searchLower = options.SearchTerm.ToLower();
            query = query.Where(s => 
                s.HexAddress.ToLower().Contains(searchLower) ||
                s.PoliceCode.ToLower().Contains(searchLower) ||
                s.StreetName1.ToLower().Contains(searchLower) ||
                s.StreetName2.ToLower().Contains(searchLower) ||
                s.ControllerNumber.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrWhiteSpace(options.Boro))
        {
            query = query.Where(s => s.Boro == options.Boro);
        }

        if (!string.IsNullOrWhiteSpace(options.ControllerType))
        {
            query = query.Where(s => s.ControllerType == options.ControllerType);
        }

        if (!string.IsNullOrWhiteSpace(options.EquipmentType))
        {
            query = query.Where(s => s.EquipmentType == options.EquipmentType);
        }

        if (!string.IsNullOrWhiteSpace(options.SignalType))
        {
            query = query.Where(s => s.SignalType == options.SignalType);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySorting(query, options.SortBy, options.SortDescending);

        // Apply pagination
        var items = await query
            .Skip((options.PageNumber - 1) * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync();

        return new PagedResult<TrafficSignal>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = options.PageNumber,
            PageSize = options.PageSize
        };
    }

    private IQueryable<TrafficSignal> ApplySorting(IQueryable<TrafficSignal> query, string? sortBy, bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query.OrderBy(s => s.HexAddress);
        }

        query = sortBy.ToLower() switch
        {
            "hexaddress" => descending ? query.OrderByDescending(s => s.HexAddress) : query.OrderBy(s => s.HexAddress),
            "policecode" => descending ? query.OrderByDescending(s => s.PoliceCode) : query.OrderBy(s => s.PoliceCode),
            "boro" => descending ? query.OrderByDescending(s => s.Boro) : query.OrderBy(s => s.Boro),
            "streetname1" => descending ? query.OrderByDescending(s => s.StreetName1) : query.OrderBy(s => s.StreetName1),
            "streetname2" => descending ? query.OrderByDescending(s => s.StreetName2) : query.OrderBy(s => s.StreetName2),
            "controllertype" => descending ? query.OrderByDescending(s => s.ControllerType) : query.OrderBy(s => s.ControllerType),
            "controllernumber" => descending ? query.OrderByDescending(s => s.ControllerNumber) : query.OrderBy(s => s.ControllerNumber),
            _ => query.OrderBy(s => s.HexAddress)
        };

        return query;
    }

    public async Task<List<TrafficSignal>> GetByBoroAsync(string boro)
    {
        return await _context.TrafficSignals
            .Where(s => s.Boro == boro)
            .OrderBy(s => s.HexAddress)
            .ToListAsync();
    }

    public async Task<TrafficSignal?> GetByIdAsync(int id)
    {
        return await _context.TrafficSignals.FindAsync(id);
    }

    public async Task<TrafficSignal?> GetByHexAddressAsync(string hexAddress)
    {
        return await _context.TrafficSignals
            .FirstOrDefaultAsync(s => s.HexAddress == hexAddress);
    }

    public async Task<TrafficSignal> CreateAsync(TrafficSignal signal)
    {
        // Validate and normalize HEX address
        ValidateAndNormalizeHexAddress(ref signal);
        
        // Check for duplicate HEX address
        var existing = await _context.TrafficSignals
            .FirstOrDefaultAsync(s => s.HexAddress == signal.HexAddress);
        
        if (existing != null)
        {
            throw new InvalidOperationException($"HEX address '{signal.HexAddress}' already exists. Please use a different HEX address.");
        }
        
        // Validate required fields
        ValidateRequiredFields(signal);
        
        // Validate optional fields
        ValidateOptionalFields(signal);

        signal.CreatedAt = DateTime.UtcNow;
        _context.TrafficSignals.Add(signal);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created new traffic signal with HEX: {HexAddress}", signal.HexAddress);
        return signal;
    }

    public async Task<TrafficSignal> UpdateAsync(TrafficSignal signal)
    {
        var existing = await GetByIdAsync(signal.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"Traffic signal with ID {signal.Id} not found.");
        }

        // Validate and normalize HEX address
        ValidateAndNormalizeHexAddress(ref signal);

        // Check for duplicate HEX address (excluding current record)
        var duplicate = await _context.TrafficSignals
            .FirstOrDefaultAsync(s => s.HexAddress == signal.HexAddress && s.Id != signal.Id);
        
        if (duplicate != null)
        {
            throw new InvalidOperationException($"HEX address '{signal.HexAddress}' already exists on another signal (ID: {duplicate.Id}). Please use a different HEX address.");
        }
        
        // Validate required fields
        ValidateRequiredFields(signal);
        
        // Validate optional fields
        ValidateOptionalFields(signal);

        signal.UpdatedAt = DateTime.UtcNow;
        _context.Entry(existing).CurrentValues.SetValues(signal);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated traffic signal ID: {Id}, HEX: {HexAddress}", signal.Id, signal.HexAddress);
        return signal;
    }

    private void ValidateAndNormalizeHexAddress(ref TrafficSignal signal)
    {
        // Check if HEX address is provided
        if (string.IsNullOrWhiteSpace(signal.HexAddress))
        {
            throw new InvalidOperationException("HEX Address is required");
        }
        
        // Normalize: trim and uppercase
        signal.HexAddress = signal.HexAddress.ToUpper().Trim();
        
        // Validate HEX format (must be exactly 4 hexadecimal characters)
        if (!System.Text.RegularExpressions.Regex.IsMatch(signal.HexAddress, "^[0-9A-F]{4}$"))
        {
            throw new InvalidOperationException("HEX Address must be exactly 4 hexadecimal characters (0-9, A-F)");
        }
    }

    private void ValidateRequiredFields(TrafficSignal signal)
    {
        // Validate Borough
        if (string.IsNullOrWhiteSpace(signal.Boro))
        {
            throw new InvalidOperationException("Borough is required");
        }
        
        if (!new[] { "1", "2", "3", "4", "5" }.Contains(signal.Boro))
        {
            throw new InvalidOperationException("Borough must be 1 (Manhattan), 2 (Bronx), 3 (Brooklyn), 4 (Queens), or 5 (Staten Island)");
        }
        
        // Validate Street Name 1
        if (string.IsNullOrWhiteSpace(signal.StreetName1))
        {
            throw new InvalidOperationException("Street Name 1 is required");
        }
        
        if (signal.StreetName1.Length < 2)
        {
            throw new InvalidOperationException("Street Name 1 must be at least 2 characters");
        }
        
        if (signal.StreetName1.Length > 200)
        {
            throw new InvalidOperationException("Street Name 1 must not exceed 200 characters");
        }
    }

    private void ValidateOptionalFields(TrafficSignal signal)
    {
        // Validate coordinates if provided
        if (signal.Latitude.HasValue && (signal.Latitude.Value < -90 || signal.Latitude.Value > 90))
        {
            throw new InvalidOperationException("Latitude must be between -90 and 90");
        }
        
        if (signal.Longitude.HasValue && (signal.Longitude.Value < -180 || signal.Longitude.Value > 180))
        {
            throw new InvalidOperationException("Longitude must be between -180 and 180");
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var signal = await GetByIdAsync(id);
        if (signal == null)
        {
            return false;
        }

        _context.TrafficSignals.Remove(signal);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted traffic signal ID: {Id}, HEX: {HexAddress}", id, signal.HexAddress);
        return true;
    }

    public async Task<int> DeleteBatchAsync(List<int> ids)
    {
        if (ids == null || !ids.Any())
        {
            return 0;
        }

        var signals = await _context.TrafficSignals
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        if (!signals.Any())
        {
            return 0;
        }

        _context.TrafficSignals.RemoveRange(signals);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Batch deleted {Count} traffic signals", signals.Count);
        return signals.Count;
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.TrafficSignals.CountAsync();
    }

    public async Task<bool> HexAddressExistsAsync(string hexAddress)
    {
        return await _context.TrafficSignals
            .AnyAsync(s => s.HexAddress == hexAddress.ToUpper());
    }

    public async Task<List<string>> GetAllHexAddressesAsync()
    {
        return await _context.TrafficSignals
            .Select(s => s.HexAddress)
            .OrderBy(h => h)
            .ToListAsync();
    }

    public async Task<int> ImportFromCsvAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV file not found: {filePath}");
        }

        var records = new List<TrafficSignal>();
        
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        }))
        {
            csv.Context.RegisterClassMap<CsvTrafficSignalMap>();
            var csvRecords = csv.GetRecords<CsvTrafficSignalRecord>();
            
            foreach (var record in csvRecords)
            {
                try
                {
                    var signal = MapCsvRecordToSignal(record);
                    records.Add(signal);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Skipped invalid record. Error: {Error}", ex.Message);
                }
            }
        }

        var existingHexAddresses = await GetAllHexAddressesAsync();
        var existingHexSet = existingHexAddresses.Select(h => h.ToUpper().Trim()).ToHashSet();
        
        _logger.LogInformation("Loaded {Count} existing HEX addresses from database for duplicate check", existingHexSet.Count);

        var imported = 0;
        var skippedDuplicates = 0;
        foreach (var signal in records)
        {
            try
            {
                // Skip if HEX is empty or invalid
                if (string.IsNullOrWhiteSpace(signal.HexAddress))
                    continue;
                
                // Normalize HEX
                signal.HexAddress = signal.HexAddress.ToUpper().Trim();
                
                // Skip if not valid HEX format
                if (!System.Text.RegularExpressions.Regex.IsMatch(signal.HexAddress, "^[0-9A-F]{4}$"))
                    continue;
                
                // Check if HEX already exists in database
                if (existingHexSet.Contains(signal.HexAddress))
                {
                    skippedDuplicates++;
                    continue;
                }
                
                // Add to set to avoid duplicates within the same import batch
                existingHexSet.Add(signal.HexAddress);
                
                if (string.IsNullOrWhiteSpace(signal.Boro))
                    signal.Boro = "1"; // Default to Manhattan
                
                if (string.IsNullOrWhiteSpace(signal.StreetName1))
                    signal.StreetName1 = "Unknown Street";
                
                signal.CreatedAt = DateTime.UtcNow;
                _context.TrafficSignals.Add(signal);
                imported++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Skipped signal with HEX {HexAddress}. Error: {Error}", signal.HexAddress, ex.Message);
            }
        }

        if (imported > 0)
        {
            await _context.SaveChangesAsync();
        }
        
        _logger.LogInformation("Imported {Count} traffic signals from CSV. Skipped {Skipped} duplicates.", imported, skippedDuplicates);
        
        return imported;
    }

    private TrafficSignal MapCsvRecordToSignal(CsvTrafficSignalRecord record)
    {
        return new TrafficSignal
        {
            // Key identification fields
            HexAddress = record.HexAddress?.ToUpper() ?? "",
            PoliceCode = record.PoliceCode ?? "",
            KeyBIP = record.KeyBIP ?? "",
            Boro = record.Boro ?? "",
            
            // GeoKey and Administrative
            GeoKeyStreet1 = record.GeoKeyStreet1 ?? "",
            GeoKeyStreet2 = record.GeoKeyStreet2 ?? "",
            SequenceNumber = record.SequenceNumber ?? "",
            AreaNumber = record.AreaNumber ?? "",
            
            // Street information
            StreetCode1 = record.StreetCode1 ?? "",
            StreetName1 = record.StreetName1 ?? "",
            StreetCode2 = record.StreetCode2 ?? "",
            StreetName2 = record.StreetName2 ?? "",
            StreetCode3 = record.StreetCode3 ?? "",
            StreetName3 = record.StreetName3 ?? "",
            StreetCode4 = record.StreetCode4 ?? "",
            StreetName4 = record.StreetName4 ?? "",
            
            // GeoKey and Location identifiers
            GeoKey1 = record.GeoKey1 ?? "",
            OnLine = record.OnLine ?? "",
            MachineNumber = record.MachineNumber ?? "",
            Section = record.Section ?? "",
            LogicalNumber = record.LogicalNumber ?? "",
            
            // Controller information
            ControllerNumber = record.ControllerNumber ?? "",
            ControllerType = record.ControllerType ?? "",
            ControllerSerialNumber = record.ControllerSerialNumber ?? "",
            
            // MOXA information
            MoxaIMEI = record.MoxaIMEI ?? "",
            MoxaCMWN = record.MoxaCMWN ?? "",
            ComputerFlag = record.ComputerFlag ?? "",
            
            // Equipment
            EquipmentType = record.EquipmentType ?? "",
            
            // ATCS Serial Numbers
            AtcsCurrentCabSerialNum = record.AtcsCurrentCabSerialNum ?? "",
            AtcsCurrentBiuSerialNum = record.AtcsCurrentBiuSerialNum ?? "",
            AtcsCurrentCmSerialNum = record.AtcsCurrentCmSerialNum ?? "",
            AtcsCurrentCpuSerialNum = record.AtcsCurrentCpuSerialNum ?? "",
            AtcsCurrentPdaSerialNum = record.AtcsCurrentPdaSerialNum ?? "",
            AtcsCurrentDciSerialNum = record.AtcsCurrentDciSerialNum ?? "",
            
            // Painting dates
            PaintingDueDate = ParseDate(record.PaintingDueDate),
            PaintingScheduledDate = ParseDate(record.PaintingScheduledDate),
            PaintingCompletedDate = ParseDate(record.PaintingCompletedDate),
            PaintStartDate = ParseDate(record.PaintStartDate),
            
            // Relamping dates
            RelampingDueDate = ParseDate(record.RelampingDueDate),
            RelampingScheduledDate = ParseDate(record.RelampingScheduledDate),
            RelampingCompletedDate = ParseDate(record.RelampingCompletedDate),
            
            // Controller dates
            ControllerDueDate = ParseDate(record.ControllerDueDate),
            ControllerScheduledDate = ParseDate(record.ControllerScheduledDate),
            ControllerCompletedDate = ParseDate(record.ControllerCompletedDate),
            
            // Stray Voltage dates
            StrayVoltageCheckDueDate = ParseDate(record.StrayVoltageCheckDueDate),
            StrayVoltageCheckScheduledDate = ParseDate(record.StrayVoltageCheckScheduledDate),
            StrayVoltageCheckCompletedDate = ParseDate(record.StrayVoltageCheckCompletedDate),
            
            // Work Order and Contract
            TsmpIntersectionControlNumber = record.TsmpIntersectionControlNumber ?? "",
            ContractorReferenceNumber = record.ContractorReferenceNumber ?? "",
            StrayVoltageOpenWorkOrder = record.StrayVoltageOpenWorkOrder ?? "",
            StrayVoltageInspectionComments = record.StrayVoltageInspectionComments ?? "",
            
            // Employee tracking
            ControllerCleaningEmployee = record.ControllerCleaningEmployee ?? "",
            PaintingEmployee = record.PaintingEmployee ?? "",
            PaintingStartEmployee = record.PaintingStartEmployee ?? "",
            LampingEmployee = record.LampingEmployee ?? "",
            
            // Controller Loop
            ControllerLoopType = record.ControllerLoopType ?? "",
            ControllerLoopNumber = record.ControllerLoopNumber ?? "",
            LoopQty = record.LoopQty ?? "",
            
            // Poles
            M2Poles = record.M2Poles ?? "",
            S1Poles = record.S1Poles ?? "",
            PoleType = record.PoleType ?? "",
            
            // Painting hardware
            PaintingRods = record.PaintingRods ?? "",
            PaintingHardware = record.PaintingHardware ?? "",
            
            // Lamp counts
            L67Lamps = record.L67Lamps ?? "",
            L150Lamps = record.L150Lamps ?? "",
            L68Lamps = record.L68Lamps ?? "",
            L116Lamps = record.L116Lamps ?? "",
            D5LedLamps = record.D5LedLamps ?? "",
            D14LedLamps = record.D14LedLamps ?? "",
            D78InLedLamps = record.D78InLedLamps ?? "",
            D712InLedLamps = record.D712InLedLamps ?? "",
            
            // Pedestrian signals
            LedPeds = record.LedPeds ?? "",
            CountdownPeds = record.CountdownPeds ?? "",
            
            // Problem and remarks
            ProblemCode = record.ProblemCode ?? "",
            Remarks = record.Remarks ?? "",
            
            // Additional equipment
            AuxiliaryCabinetsNo = record.AuxiliaryCabinetsNo ?? "",
            ControllerDialNo = record.ControllerDialNo ?? "",
            UID = record.UID ?? "",
            
            // Location coordinates
            Latitude = ParseDecimal(record.Latitude),
            Longitude = ParseDecimal(record.Longitude),
            MappingX = ParseDecimal(record.MappingX),
            MappingY = ParseDecimal(record.MappingY),
            
            // Node and Segment
            NodeId = record.NodeId ?? "",
            SegmentId = record.SegmentId ?? "",
            
            // Signal information
            SignalType = record.SignalType ?? "",
            SignalInstallDate = ParseDate(record.SignalInstallDate),
            
            // Red Light Camera
            RedlightCameraType = record.RedlightCameraType ?? "",
            RedlightCameraInstallDate = ParseDate(record.RedlightCameraInstallDate),
            RedlightCameraRemoveDate = ParseDate(record.RedlightCameraRemoveDate),
            
            // Battery Backup System
            BatteryBackupModel = record.BatteryBackupModel ?? "",
            BatteryLastChanged = ParseDate(record.BatteryLastChanged),
            BbsMake = record.BbsMake ?? "",
            BbsModel = record.BbsModel ?? "",
            BbsNextChangeDate = ParseDate(record.BbsNextChangeDate),
            BbsScheduleGroup = record.BbsScheduleGroup ?? "",
            BbsChangeScheduledDate = ParseDate(record.BbsChangeScheduledDate),
            
            // Other equipment
            Cylinders = record.Cylinders ?? "",
            CoastalFoundations = record.CoastalFoundations ?? ""
        };
    }

    private decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        
        return null;
    }

    private DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        
        // Try parsing with specific formats used in CSV (MM/dd/yy)
        string[] formats = { 
            "MM/dd/yy", 
            "M/d/yy", 
            "MM/dd/yyyy", 
            "M/d/yyyy",
            "yyyy-MM-dd",
            "MM-dd-yyyy"
        };
        
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result;
        }
        
        // Fallback to general parse
        if (DateTime.TryParse(value, out result))
        {
            return result;
        }
        
        return null;
    }

    // CSV Mapping classes
    private class CsvTrafficSignalRecord
    {
        [CsvHelper.Configuration.Attributes.Name("POLICE CODE")]
        public string? PoliceCode { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("KEY (B/I/P)")]
        public string? KeyBIP { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("BORO")]
        public string? Boro { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("s0) GEOKEY STREET 1")]
        public string? GeoKeyStreet1 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("s0) GEOKEY STREET 2")]
        public string? GeoKeyStreet2 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("SEQUENCE NUMBER")]
        public string? SequenceNumber { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("AREA NUMBER")]
        public string? AreaNumber { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("CONTROLLER NUMBER")]
        public string? ControllerNumber { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("CONTROLLER TYPE")]
        public string? ControllerType { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("CONTROLLER SERIAL NUMBER")]
        public string? ControllerSerialNumber { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("MOXA IMEI")]
        public string? MoxaIMEI { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("MOXA CMWN")]
        public string? MoxaCMWN { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("COMPUTER FLAG?")]
        public string? ComputerFlag { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("a0) STREET CODE 1")]
        public string? StreetCode1 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("a1) STREET NAME 1")]
        public string? StreetName1 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("a0) STREET CODE 2")]
        public string? StreetCode2 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("a1) STREET NAME 2")]
        public string? StreetName2 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("a0) STREET CODE 3")]
        public string? StreetCode3 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("a1) STREET NAME 3")]
        public string? StreetName3 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("a0) STREET CODE 4")]
        public string? StreetCode4 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("a1) STREET NAME 4")]
        public string? StreetName4 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("GEOKEY1")]
        public string? GeoKey1 { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("ON_LINE")]
        public string? OnLine { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("MACHINE NUMBER")]
        public string? MachineNumber { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("SECTION")]
        public string? Section { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("LOGICAL NUMBER")]
        public string? LogicalNumber { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d0) PAINTING DUE DATE")]
        public string? PaintingDueDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d1) PAINTING SCHEDULED DATE")]
        public string? PaintingScheduledDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d2) PAINTING COMPLETED DATE")]
        public string? PaintingCompletedDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d0) RELAMPING DUE DATE")]
        public string? RelampingDueDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d1) RELAMPING SCHEDULED DATE")]
        public string? RelampingScheduledDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d2) RELAMPING COMPLETED DATE")]
        public string? RelampingCompletedDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d0) CONTROLLER DUE DATE")]
        public string? ControllerDueDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d1) CONTROLLER SCHEDULED DATE")]
        public string? ControllerScheduledDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d2) CONTROLLER COMPLETED DATE")]
        public string? ControllerCompletedDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("TSMP Intersection Control Number")]
        public string? TsmpIntersectionControlNumber { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("CONTRACTOR REFERENCE NUMBER")]
        public string? ContractorReferenceNumber { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("CONTROLLER CLEANING EMPLOYEE")]
        public string? ControllerCleaningEmployee { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("CONTROLLER LOOP TYPE")]
        public string? ControllerLoopType { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("CONTROLLER LOOP NUMBER")]
        public string? ControllerLoopNumber { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("M2 POLES")]
        public string? M2Poles { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("S1 POLES")]
        public string? S1Poles { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("PAINT START DATE")]
        public string? PaintStartDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("PAINTING EMPLOYEE")]
        public string? PaintingEmployee { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("PAINTING START EMPLOYEE")]
        public string? PaintingStartEmployee { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("PAINTING RODS")]
        public string? PaintingRods { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("PAINTING HARDWARE")]
        public string? PaintingHardware { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("LAMPING EMPLOYEE")]
        public string? LampingEmployee { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("L67 LAMPS")]
        public string? L67Lamps { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("L150 LAMPS")]
        public string? L150Lamps { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("LED PEDS")]
        public string? LedPeds { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("PROBLEM CODE")]
        public string? ProblemCode { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("REMARKS")]
        public string? Remarks { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("AUXILIARY CABINETS NO")]
        public string? AuxiliaryCabinetsNo { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("CONTROLLER DIAL NO")]
        public string? ControllerDialNo { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("UID")]
        public string? UID { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("LOOP QTY")]
        public string? LoopQty { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("D5 LED LAMPS")]
        public string? D5LedLamps { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("D14 LED LAMPS")]
        public string? D14LedLamps { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("D7 8 IN LED LAMPS")]
        public string? D78InLedLamps { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("D7 12 IN LED LAMPS")]
        public string? D712InLedLamps { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("L68 LAMPS")]
        public string? L68Lamps { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("L116 LAMPS")]
        public string? L116Lamps { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("COUNTDOWN PEDS")]
        public string? CountdownPeds { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("ATCS_CABINET_ADDRESS_HEX")]
        public string? HexAddress { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("ATCS_EQUIP_TYPE")]
        public string? EquipmentType { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("v0) ATCS_CURRENT_CAB_SERIAL_NUM")]
        public string? AtcsCurrentCabSerialNum { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("v0) ATCS_CURRENT_BIU_SERIAL_NUM")]
        public string? AtcsCurrentBiuSerialNum { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("v0) ATCS_CURRENT_CM_SERIAL_NUM")]
        public string? AtcsCurrentCmSerialNum { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("v0) ATCS_CURRENT_CPU_SERIAL_NUM")]
        public string? AtcsCurrentCpuSerialNum { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("v0) ATCS_CURRENT_PDA_SERIAL_NUM")]
        public string? AtcsCurrentPdaSerialNum { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("v0) ATCS_CURRENT_DCI_SERIAL_NUM")]
        public string? AtcsCurrentDciSerialNum { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d0) STRAY VOLTAGE CHECK DUE DATE")]
        public string? StrayVoltageCheckDueDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d1) STRAY VOLTAGE CHECK SCHEDULED DATE")]
        public string? StrayVoltageCheckScheduledDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("d2) STRAY VOLTAGE CHECK COMPLETED DATE")]
        public string? StrayVoltageCheckCompletedDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("STRAY VOLTAGE OPEN WORK ORDER #")]
        public string? StrayVoltageOpenWorkOrder { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("STRAY VOLTAGE INSPECTION COMMENTS")]
        public string? StrayVoltageInspectionComments { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Signal Type")]
        public string? SignalType { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Longitude")]
        public string? Longitude { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Latitude")]
        public string? Latitude { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Mapping X Coordinates")]
        public string? MappingX { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Mapping Y Coordinates")]
        public string? MappingY { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Node_ID")]
        public string? NodeId { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Segment_ID")]
        public string? SegmentId { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Redlight Camera Type")]
        public string? RedlightCameraType { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Redlight Camera Install Date")]
        public string? RedlightCameraInstallDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Redlight Camera Remove Date")]
        public string? RedlightCameraRemoveDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Battery Backup Model")]
        public string? BatteryBackupModel { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Battery Last Changed")]
        public string? BatteryLastChanged { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Cylinders")]
        public string? Cylinders { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Coastal Foundations")]
        public string? CoastalFoundations { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("BBS Make")]
        public string? BbsMake { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("BBS Model")]
        public string? BbsModel { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("BBS Next Change Date")]
        public string? BbsNextChangeDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("Signal Install Date")]
        public string? SignalInstallDate { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("CB Pole Type")]
        public string? PoleType { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("BBS Schedule Group")]
        public string? BbsScheduleGroup { get; set; }
        
        [CsvHelper.Configuration.Attributes.Name("BBS Change Scheduled Date")]
        public string? BbsChangeScheduledDate { get; set; }
    }

    private class CsvTrafficSignalMap : ClassMap<CsvTrafficSignalRecord>
    {
        public CsvTrafficSignalMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
        }
    }
}
