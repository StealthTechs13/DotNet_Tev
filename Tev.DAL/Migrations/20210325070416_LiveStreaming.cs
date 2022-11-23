using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class LiveStreaming : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrgId",
                table: "InvoiceHistories",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LiveStreamingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogicalDeviceId = table.Column<string>(nullable: true),
                    OrgId = table.Column<string>(nullable: true),
                    StartedByEmail = table.Column<string>(nullable: true),
                    StoppedByEmail = table.Column<string>(nullable: true),
                    StartedAtUTC = table.Column<DateTime>(nullable: false),
                    StoppedAtUTC = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveStreamingRecords", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "InvoiceHistories");
        }
    }
}
