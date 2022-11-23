using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class DeviceReplacementRemovedExpectedDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedDate",
                table: "DeviceReplacements");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDate",
                table: "DeviceReplacements",
                type: "datetime2",
                nullable: true);
        }
    }
}
