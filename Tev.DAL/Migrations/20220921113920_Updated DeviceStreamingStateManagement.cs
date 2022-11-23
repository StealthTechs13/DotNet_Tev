using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class UpdatedDeviceStreamingStateManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StreamingUserStateManagement");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceStreamingStateManagement",
                table: "DeviceStreamingStateManagement");

            migrationBuilder.DropColumn(
                name: "IsUserActive",
                table: "DeviceStreamingStateManagement");

            migrationBuilder.DropColumn(
                name: "LastUpdatedDate",
                table: "DeviceStreamingStateManagement");

            migrationBuilder.DropColumn(
                name: "LiveStreamingState",
                table: "DeviceStreamingStateManagement");

            migrationBuilder.DropColumn(
                name: "PlaybackStreamingState",
                table: "DeviceStreamingStateManagement");

            migrationBuilder.RenameTable(
                name: "DeviceStreamingStateManagement",
                newName: "deviceStreamingStateManagement");

            migrationBuilder.AddColumn<bool>(
                name: "IsUserStreaming",
                table: "deviceStreamingStateManagement",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LiveStreamingActive",
                table: "deviceStreamingStateManagement",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PlaybackStreamingActive",
                table: "deviceStreamingStateManagement",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_deviceStreamingStateManagement",
                table: "deviceStreamingStateManagement",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_deviceStreamingStateManagement",
                table: "deviceStreamingStateManagement");

            migrationBuilder.DropColumn(
                name: "IsUserStreaming",
                table: "deviceStreamingStateManagement");

            migrationBuilder.DropColumn(
                name: "LiveStreamingActive",
                table: "deviceStreamingStateManagement");

            migrationBuilder.DropColumn(
                name: "PlaybackStreamingActive",
                table: "deviceStreamingStateManagement");

            migrationBuilder.RenameTable(
                name: "deviceStreamingStateManagement",
                newName: "DeviceStreamingStateManagement");

            migrationBuilder.AddColumn<bool>(
                name: "IsUserActive",
                table: "DeviceStreamingStateManagement",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedDate",
                table: "DeviceStreamingStateManagement",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "LiveStreamingState",
                table: "DeviceStreamingStateManagement",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PlaybackStreamingState",
                table: "DeviceStreamingStateManagement",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceStreamingStateManagement",
                table: "DeviceStreamingStateManagement",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "StreamingUserStateManagement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<long>(type: "bigint", nullable: false),
                    IsUserActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LiveStreamingState = table.Column<bool>(type: "bit", nullable: false),
                    LogicalDeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<long>(type: "bigint", nullable: true),
                    OrgId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlaybackStreamingState = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserTokenId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamingUserStateManagement", x => x.Id);
                });
        }
    }
}
