using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddEscalationTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EscalationMatrices",
                columns: table => new
                {
                    EscalationMatrixId = table.Column<string>(nullable: false),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    OrganizationId = table.Column<int>(nullable: false),
                    ReceiverName = table.Column<string>(nullable: true),
                    ReceiverDescription = table.Column<string>(nullable: true),
                    ReceiverPhone = table.Column<string>(nullable: true),
                    EscalationLevel = table.Column<int>(nullable: false),
                    SmokeValue = table.Column<int>(nullable: false),
                    SenderPhone = table.Column<string>(nullable: true),
                    SmokeStatus = table.Column<int>(nullable: false),
                    AttentionTime = table.Column<decimal>(nullable: false),
                    DeviceId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationMatrices", x => x.EscalationMatrixId);
                    table.ForeignKey(
                        name: "FK_EscalationMatrices_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "PhysicalDeviceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscalationMatrices_DeviceId",
                table: "EscalationMatrices",
                column: "DeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EscalationMatrices");
        }
    }
}
