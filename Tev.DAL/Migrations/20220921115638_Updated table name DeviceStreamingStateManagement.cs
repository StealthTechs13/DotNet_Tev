using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class UpdatedtablenameDeviceStreamingStateManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_deviceStreamingStateManagement",
                table: "deviceStreamingStateManagement");

            migrationBuilder.RenameTable(
                name: "deviceStreamingStateManagement",
                newName: "DeviceStreamingTypeManagement");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceStreamingTypeManagement",
                table: "DeviceStreamingTypeManagement",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceStreamingTypeManagement",
                table: "DeviceStreamingTypeManagement");

            migrationBuilder.RenameTable(
                name: "DeviceStreamingTypeManagement",
                newName: "deviceStreamingStateManagement");

            migrationBuilder.AddPrimaryKey(
                name: "PK_deviceStreamingStateManagement",
                table: "deviceStreamingStateManagement",
                column: "Id");
        }
    }
}
