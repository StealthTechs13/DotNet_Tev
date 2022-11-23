using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class ChangeDeviceReplacementKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RaiseBy",
                table: "DeviceReplacements");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RaiseBy",
                table: "DeviceReplacements",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
