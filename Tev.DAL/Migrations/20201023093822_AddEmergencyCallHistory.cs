using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddEmergencyCallHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmergencyCallHistories",
                columns: table => new
                {
                    EmergencyCallHistoryId = table.Column<string>(nullable: false),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    Number = table.Column<string>(nullable: true),
                    Time = table.Column<DateTime>(nullable: false),
                    CallingPurpose = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyCallHistories", x => x.EmergencyCallHistoryId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyCallHistories");
        }
    }
}
