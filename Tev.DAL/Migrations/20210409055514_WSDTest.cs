using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class WSDTest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WSDTestRecords",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    DeviceId = table.Column<string>(nullable: true),
                    GTemperatureSensorOffset2 = table.Column<int>(nullable: true),
                    GTemperatureSensorOffset = table.Column<int>(nullable: true),
                    ClearAir = table.Column<int>(nullable: true),
                    IREDCalibration = table.Column<int>(nullable: true),
                    PhotoOffset = table.Column<int>(nullable: true),
                    DriftLimit = table.Column<int>(nullable: true),
                    DriftBypass = table.Column<int>(nullable: true),
                    TransmitResolution = table.Column<int>(nullable: true),
                    TransmitThreshold = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WSDTestRecords", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WSDTestRecords");
        }
    }
}
