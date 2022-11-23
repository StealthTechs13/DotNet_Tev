using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class RemoveDeviceTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceReplacement_Devices_DeviceId",
                table: "DeviceReplacement");

            migrationBuilder.DropForeignKey(
                name: "FK_EmergencyCallHistories_Devices_DeviceId",
                table: "EmergencyCallHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EscalationMatrices_Devices_DeviceId",
                table: "EscalationMatrices");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_EscalationMatrices_DeviceId",
                table: "EscalationMatrices");

            migrationBuilder.DropIndex(
                name: "IX_EmergencyCallHistories_DeviceId",
                table: "EmergencyCallHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceReplacement",
                table: "DeviceReplacement");

            migrationBuilder.DropIndex(
                name: "IX_DeviceReplacement_DeviceId",
                table: "DeviceReplacement");

            migrationBuilder.RenameTable(
                name: "DeviceReplacement",
                newName: "DeviceReplacements");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "EscalationMatrices",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "EmergencyCallHistories",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "DeviceReplacements",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceReplacements",
                table: "DeviceReplacements",
                column: "DeviceReplacementId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceReplacements",
                table: "DeviceReplacements");

            migrationBuilder.RenameTable(
                name: "DeviceReplacements",
                newName: "DeviceReplacement");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "EscalationMatrices",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "EmergencyCallHistories",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "DeviceReplacement",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceReplacement",
                table: "DeviceReplacement",
                column: "DeviceReplacementId");

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    PhysicalDeviceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LogicalDeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrgId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.PhysicalDeviceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscalationMatrices_DeviceId",
                table: "EscalationMatrices",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyCallHistories_DeviceId",
                table: "EmergencyCallHistories",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceReplacement_DeviceId",
                table: "DeviceReplacement",
                column: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceReplacement_Devices_DeviceId",
                table: "DeviceReplacement",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "PhysicalDeviceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyCallHistories_Devices_DeviceId",
                table: "EmergencyCallHistories",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "PhysicalDeviceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EscalationMatrices_Devices_DeviceId",
                table: "EscalationMatrices",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "PhysicalDeviceId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
