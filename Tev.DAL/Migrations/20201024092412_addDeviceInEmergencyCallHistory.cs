using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class addDeviceInEmergencyCallHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "EmergencyCallHistories",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyCallHistories_DeviceId",
                table: "EmergencyCallHistories",
                column: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyCallHistories_Devices_DeviceId",
                table: "EmergencyCallHistories",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "PhysicalDeviceId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmergencyCallHistories_Devices_DeviceId",
                table: "EmergencyCallHistories");

            migrationBuilder.DropIndex(
                name: "IX_EmergencyCallHistories_DeviceId",
                table: "EmergencyCallHistories");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "EmergencyCallHistories");
        }
    }
}
