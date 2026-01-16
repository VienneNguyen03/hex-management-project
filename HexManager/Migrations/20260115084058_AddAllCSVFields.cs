using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HexManager.Migrations
{
    /// <inheritdoc />
    public partial class AddAllCSVFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrafficSignals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PoliceCode = table.Column<string>(type: "TEXT", nullable: false),
                    KeyBIP = table.Column<string>(type: "TEXT", nullable: false),
                    Boro = table.Column<string>(type: "TEXT", nullable: false),
                    GeoKeyStreet1 = table.Column<string>(type: "TEXT", nullable: false),
                    GeoKeyStreet2 = table.Column<string>(type: "TEXT", nullable: false),
                    SequenceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    AreaNumber = table.Column<string>(type: "TEXT", nullable: false),
                    StreetCode1 = table.Column<string>(type: "TEXT", nullable: false),
                    StreetName1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StreetCode2 = table.Column<string>(type: "TEXT", nullable: false),
                    StreetName2 = table.Column<string>(type: "TEXT", nullable: false),
                    StreetCode3 = table.Column<string>(type: "TEXT", nullable: false),
                    StreetName3 = table.Column<string>(type: "TEXT", nullable: false),
                    StreetCode4 = table.Column<string>(type: "TEXT", nullable: false),
                    StreetName4 = table.Column<string>(type: "TEXT", nullable: false),
                    GeoKey1 = table.Column<string>(type: "TEXT", nullable: false),
                    OnLine = table.Column<string>(type: "TEXT", nullable: false),
                    MachineNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Section = table.Column<string>(type: "TEXT", nullable: false),
                    LogicalNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ControllerNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ControllerType = table.Column<string>(type: "TEXT", nullable: false),
                    ControllerSerialNumber = table.Column<string>(type: "TEXT", nullable: false),
                    MoxaIMEI = table.Column<string>(type: "TEXT", nullable: false),
                    MoxaCMWN = table.Column<string>(type: "TEXT", nullable: false),
                    ComputerFlag = table.Column<string>(type: "TEXT", nullable: false),
                    HexAddress = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    EquipmentType = table.Column<string>(type: "TEXT", nullable: false),
                    AtcsCurrentCabSerialNum = table.Column<string>(type: "TEXT", nullable: false),
                    AtcsCurrentBiuSerialNum = table.Column<string>(type: "TEXT", nullable: false),
                    AtcsCurrentCmSerialNum = table.Column<string>(type: "TEXT", nullable: false),
                    AtcsCurrentCpuSerialNum = table.Column<string>(type: "TEXT", nullable: false),
                    AtcsCurrentPdaSerialNum = table.Column<string>(type: "TEXT", nullable: false),
                    AtcsCurrentDciSerialNum = table.Column<string>(type: "TEXT", nullable: false),
                    PaintingDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaintingScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaintingCompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaintStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RelampingDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RelampingScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RelampingCompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ControllerDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ControllerScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ControllerCompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StrayVoltageCheckDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StrayVoltageCheckScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StrayVoltageCheckCompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TsmpIntersectionControlNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ContractorReferenceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    StrayVoltageOpenWorkOrder = table.Column<string>(type: "TEXT", nullable: false),
                    StrayVoltageInspectionComments = table.Column<string>(type: "TEXT", nullable: false),
                    ControllerCleaningEmployee = table.Column<string>(type: "TEXT", nullable: false),
                    PaintingEmployee = table.Column<string>(type: "TEXT", nullable: false),
                    PaintingStartEmployee = table.Column<string>(type: "TEXT", nullable: false),
                    LampingEmployee = table.Column<string>(type: "TEXT", nullable: false),
                    ControllerLoopType = table.Column<string>(type: "TEXT", nullable: false),
                    ControllerLoopNumber = table.Column<string>(type: "TEXT", nullable: false),
                    LoopQty = table.Column<string>(type: "TEXT", nullable: false),
                    M2Poles = table.Column<string>(type: "TEXT", nullable: false),
                    S1Poles = table.Column<string>(type: "TEXT", nullable: false),
                    PoleType = table.Column<string>(type: "TEXT", nullable: false),
                    PaintingRods = table.Column<string>(type: "TEXT", nullable: false),
                    PaintingHardware = table.Column<string>(type: "TEXT", nullable: false),
                    L67Lamps = table.Column<string>(type: "TEXT", nullable: false),
                    L150Lamps = table.Column<string>(type: "TEXT", nullable: false),
                    L68Lamps = table.Column<string>(type: "TEXT", nullable: false),
                    L116Lamps = table.Column<string>(type: "TEXT", nullable: false),
                    D5LedLamps = table.Column<string>(type: "TEXT", nullable: false),
                    D14LedLamps = table.Column<string>(type: "TEXT", nullable: false),
                    D78InLedLamps = table.Column<string>(type: "TEXT", nullable: false),
                    D712InLedLamps = table.Column<string>(type: "TEXT", nullable: false),
                    LedPeds = table.Column<string>(type: "TEXT", nullable: false),
                    CountdownPeds = table.Column<string>(type: "TEXT", nullable: false),
                    ProblemCode = table.Column<string>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: false),
                    AuxiliaryCabinetsNo = table.Column<string>(type: "TEXT", nullable: false),
                    ControllerDialNo = table.Column<string>(type: "TEXT", nullable: false),
                    UID = table.Column<string>(type: "TEXT", nullable: false),
                    Latitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    Longitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    MappingX = table.Column<decimal>(type: "TEXT", nullable: true),
                    MappingY = table.Column<decimal>(type: "TEXT", nullable: true),
                    NodeId = table.Column<string>(type: "TEXT", nullable: false),
                    SegmentId = table.Column<string>(type: "TEXT", nullable: false),
                    SignalType = table.Column<string>(type: "TEXT", nullable: false),
                    SignalInstallDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RedlightCameraType = table.Column<string>(type: "TEXT", nullable: false),
                    RedlightCameraInstallDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RedlightCameraRemoveDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BatteryBackupModel = table.Column<string>(type: "TEXT", nullable: false),
                    BatteryLastChanged = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BbsMake = table.Column<string>(type: "TEXT", nullable: false),
                    BbsModel = table.Column<string>(type: "TEXT", nullable: false),
                    BbsNextChangeDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BbsScheduleGroup = table.Column<string>(type: "TEXT", nullable: false),
                    BbsChangeScheduledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Cylinders = table.Column<string>(type: "TEXT", nullable: false),
                    CoastalFoundations = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficSignals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrafficSignals_Boro",
                table: "TrafficSignals",
                column: "Boro");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficSignals_HexAddress",
                table: "TrafficSignals",
                column: "HexAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrafficSignals_Streets",
                table: "TrafficSignals",
                columns: new[] { "StreetName1", "StreetName2" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrafficSignals");
        }
    }
}
