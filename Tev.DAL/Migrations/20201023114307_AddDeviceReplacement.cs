using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddDeviceReplacement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceReplacement",
                columns: table => new
                {
                    DeviceReplacementId = table.Column<string>(nullable: false),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    DeviceId = table.Column<string>(nullable: false),
                    OrgId = table.Column<string>(nullable: true),
                    RaiseBy = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    ExpectedDate = table.Column<DateTime>(nullable: false),
                    ReplaceStatus = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceReplacement", x => x.DeviceReplacementId);
                    table.ForeignKey(
                        name: "FK_DeviceReplacement_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "PhysicalDeviceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceReplacement_DeviceId",
                table: "DeviceReplacement",
                column: "DeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceReplacement");
        }
    }
}
