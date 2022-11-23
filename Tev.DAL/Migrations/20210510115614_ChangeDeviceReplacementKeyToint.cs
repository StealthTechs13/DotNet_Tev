using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class ChangeDeviceReplacementKeyToint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceReplacements",
                table: "DeviceReplacements");

            migrationBuilder.DropColumn(
                name: "DeviceReplacementId",
                table: "DeviceReplacements");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "DeviceReplacements");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DeviceReplacements",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceReplacements",
                table: "DeviceReplacements",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceReplacements",
                table: "DeviceReplacements");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DeviceReplacements");

            migrationBuilder.AddColumn<string>(
                name: "DeviceReplacementId",
                table: "DeviceReplacements",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "DeviceReplacements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceReplacements",
                table: "DeviceReplacements",
                column: "DeviceReplacementId");
        }
    }
}
