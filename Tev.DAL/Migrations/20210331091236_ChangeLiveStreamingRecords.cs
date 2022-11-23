using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class ChangeLiveStreamingRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LiveStreamingRecords",
                table: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "StartedAtUTC",
                table: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "StartedByEmail",
                table: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "StoppedAtUTC",
                table: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "StoppedByEmail",
                table: "LiveStreamingRecords");

            migrationBuilder.AlterColumn<string>(
                name: "LogicalDeviceId",
                table: "LiveStreamingRecords",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondsLiveStreamed",
                table: "LiveStreamingRecords",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedUTC",
                table: "LiveStreamingRecords",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_LiveStreamingRecords",
                table: "LiveStreamingRecords",
                column: "LogicalDeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LiveStreamingRecords",
                table: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "SecondsLiveStreamed",
                table: "LiveStreamingRecords");

            migrationBuilder.DropColumn(
                name: "StartedUTC",
                table: "LiveStreamingRecords");

            migrationBuilder.AlterColumn<string>(
                name: "LogicalDeviceId",
                table: "LiveStreamingRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "LiveStreamingRecords",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUTC",
                table: "LiveStreamingRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "StartedByEmail",
                table: "LiveStreamingRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StoppedAtUTC",
                table: "LiveStreamingRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "StoppedByEmail",
                table: "LiveStreamingRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LiveStreamingRecords",
                table: "LiveStreamingRecords",
                column: "Id");
        }
    }
}
