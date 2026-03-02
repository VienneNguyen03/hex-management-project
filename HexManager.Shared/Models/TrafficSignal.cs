using System.ComponentModel.DataAnnotations;

namespace HexManager.Models;

public class TrafficSignal
{
    public int Id { get; set; }
    
    // Key identification fields
    public string PoliceCode { get; set; } = string.Empty;
    public string KeyBIP { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Borough is required")]
    [RegularExpression("^[1-5]$", ErrorMessage = "Borough must be 1-5")]
    public string Boro { get; set; } = string.Empty;
    
    // GeoKey Street information
    public string GeoKeyStreet1 { get; set; } = string.Empty;
    public string GeoKeyStreet2 { get; set; } = string.Empty;
    
    // Administrative fields
    public string SequenceNumber { get; set; } = string.Empty;
    public string AreaNumber { get; set; } = string.Empty;
    
    // Street information
    public string StreetCode1 { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Street Name 1 is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Street Name 1 must be between 2 and 200 characters")]
    public string StreetName1 { get; set; } = string.Empty;
    
    public string StreetCode2 { get; set; } = string.Empty;
    public string StreetName2 { get; set; } = string.Empty;
    public string StreetCode3 { get; set; } = string.Empty;
    public string StreetName3 { get; set; } = string.Empty;
    public string StreetCode4 { get; set; } = string.Empty;
    public string StreetName4 { get; set; } = string.Empty;
    
    // GeoKey and Location identifiers
    public string GeoKey1 { get; set; } = string.Empty;
    public string OnLine { get; set; } = string.Empty;
    public string MachineNumber { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string LogicalNumber { get; set; } = string.Empty;
    
    // Controller information
    public string ControllerNumber { get; set; } = string.Empty;
    public string ControllerType { get; set; } = string.Empty;
    public string ControllerSerialNumber { get; set; } = string.Empty;
    
    // MOXA information
    public string MoxaIMEI { get; set; } = string.Empty;
    public string MoxaCMWN { get; set; } = string.Empty;
    public string ComputerFlag { get; set; } = string.Empty;
    
    // HEX Address - The main field we're working with
    [Required(ErrorMessage = "HEX Address is required")]
    [RegularExpression("^[0-9A-Fa-f]{4}$", ErrorMessage = "HEX Address must be exactly 4 hexadecimal characters (0-9, A-F)")]
    [StringLength(4, MinimumLength = 4, ErrorMessage = "HEX Address must be exactly 4 characters")]
    public string HexAddress { get; set; } = string.Empty;
    
    // Equipment type
    public string EquipmentType { get; set; } = string.Empty;
    
    // ATCS Serial Numbers
    public string AtcsCurrentCabSerialNum { get; set; } = string.Empty;
    public string AtcsCurrentBiuSerialNum { get; set; } = string.Empty;
    public string AtcsCurrentCmSerialNum { get; set; } = string.Empty;
    public string AtcsCurrentCpuSerialNum { get; set; } = string.Empty;
    public string AtcsCurrentPdaSerialNum { get; set; } = string.Empty;
    public string AtcsCurrentDciSerialNum { get; set; } = string.Empty;
    
    // Maintenance dates - Painting
    public DateTime? PaintingDueDate { get; set; }
    public DateTime? PaintingScheduledDate { get; set; }
    public DateTime? PaintingCompletedDate { get; set; }
    public DateTime? PaintStartDate { get; set; }
    
    // Maintenance dates - Relamping
    public DateTime? RelampingDueDate { get; set; }
    public DateTime? RelampingScheduledDate { get; set; }
    public DateTime? RelampingCompletedDate { get; set; }
    
    // Maintenance dates - Controller
    public DateTime? ControllerDueDate { get; set; }
    public DateTime? ControllerScheduledDate { get; set; }
    public DateTime? ControllerCompletedDate { get; set; }
    
    // Maintenance dates - Stray Voltage
    public DateTime? StrayVoltageCheckDueDate { get; set; }
    public DateTime? StrayVoltageCheckScheduledDate { get; set; }
    public DateTime? StrayVoltageCheckCompletedDate { get; set; }
    
    // Work Order and Contract information
    public string TsmpIntersectionControlNumber { get; set; } = string.Empty;
    public string ContractorReferenceNumber { get; set; } = string.Empty;
    public string StrayVoltageOpenWorkOrder { get; set; } = string.Empty;
    public string StrayVoltageInspectionComments { get; set; } = string.Empty;
    
    // Employee tracking
    public string ControllerCleaningEmployee { get; set; } = string.Empty;
    public string PaintingEmployee { get; set; } = string.Empty;
    public string PaintingStartEmployee { get; set; } = string.Empty;
    public string LampingEmployee { get; set; } = string.Empty;
    
    // Controller Loop information
    public string ControllerLoopType { get; set; } = string.Empty;
    public string ControllerLoopNumber { get; set; } = string.Empty;
    public string LoopQty { get; set; } = string.Empty;
    
    // Pole information
    public string M2Poles { get; set; } = string.Empty;
    public string S1Poles { get; set; } = string.Empty;
    public string PoleType { get; set; } = string.Empty;
    
    // Painting hardware
    public string PaintingRods { get; set; } = string.Empty;
    public string PaintingHardware { get; set; } = string.Empty;
    
    // Lamp counts
    public string L67Lamps { get; set; } = string.Empty;
    public string L150Lamps { get; set; } = string.Empty;
    public string L68Lamps { get; set; } = string.Empty;
    public string L116Lamps { get; set; } = string.Empty;
    public string D5LedLamps { get; set; } = string.Empty;
    public string D14LedLamps { get; set; } = string.Empty;
    public string D78InLedLamps { get; set; } = string.Empty;
    public string D712InLedLamps { get; set; } = string.Empty;
    
    // Pedestrian signals
    public string LedPeds { get; set; } = string.Empty;
    public string CountdownPeds { get; set; } = string.Empty;
    
    // Problem and remarks
    public string ProblemCode { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    
    // Additional equipment
    public string AuxiliaryCabinetsNo { get; set; } = string.Empty;
    public string ControllerDialNo { get; set; } = string.Empty;
    public string UID { get; set; } = string.Empty;
    
    // Location coordinates
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public decimal? Latitude { get; set; }
    
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public decimal? Longitude { get; set; }
    
    public decimal? MappingX { get; set; }
    public decimal? MappingY { get; set; }
    
    // Node and Segment
    public string NodeId { get; set; } = string.Empty;
    public string SegmentId { get; set; } = string.Empty;
    
    // Signal information
    public string SignalType { get; set; } = string.Empty;
    public DateTime? SignalInstallDate { get; set; }
    
    // Red Light Camera
    public string RedlightCameraType { get; set; } = string.Empty;
    public DateTime? RedlightCameraInstallDate { get; set; }
    public DateTime? RedlightCameraRemoveDate { get; set; }
    
    // Battery Backup System (BBS)
    public string BatteryBackupModel { get; set; } = string.Empty;
    public DateTime? BatteryLastChanged { get; set; }
    public string BbsMake { get; set; } = string.Empty;
    public string BbsModel { get; set; } = string.Empty;
    public DateTime? BbsNextChangeDate { get; set; }
    public string BbsScheduleGroup { get; set; } = string.Empty;
    public DateTime? BbsChangeScheduledDate { get; set; }
    
    // Other equipment
    public string Cylinders { get; set; } = string.Empty;
    public string CoastalFoundations { get; set; } = string.Empty;
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
