using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AlterLiveStreamingTbl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "LiveStreamingRecords",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedDate",
                table: "LiveStreamingRecords",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "LiveStreamingRecords",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ModifiedDate",
                table: "LiveStreamingRecords",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "LiveStreamingRecords");
        }
    }
}
