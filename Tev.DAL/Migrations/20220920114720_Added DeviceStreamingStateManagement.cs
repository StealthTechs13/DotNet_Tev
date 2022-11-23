using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddedDeviceStreamingStateManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropColumn(
            //    name: "IsUSserActive",
            //    table: "StreamingUserStateManagement");

            //migrationBuilder.DropColumn(
            //    name: "LastUodatedDate",
            //    table: "StreamingUserStateManagement");

            //migrationBuilder.RenameColumn(
            //    name: "USerTokenId",
            //    table: "StreamingUserStateManagement",
            //    newName: "UserTokenId");

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsUserActive",
            //    table: "StreamingUserStateManagement",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "LastUpdatedDate",
            //    table: "StreamingUserStateManagement",
            //    nullable: false,
            //    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "DeviceStreamingStateManagement",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    UserName = table.Column<string>(nullable: true),
                    LogicalDeviceId = table.Column<string>(nullable: true),
                    UserTokenId = table.Column<string>(nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(nullable: false),
                    LiveStreamingState = table.Column<bool>(nullable: false),
                    PlaybackStreamingState = table.Column<bool>(nullable: false),
                    IsUserActive = table.Column<bool>(nullable: false),
                    OrgId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceStreamingStateManagement", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "DeviceStreamingStateManagement");

            //migrationBuilder.DropColumn(
            //    name: "IsUserActive",
            //    table: "StreamingUserStateManagement");

            //migrationBuilder.DropColumn(
            //    name: "LastUpdatedDate",
            //    table: "StreamingUserStateManagement");

            //migrationBuilder.RenameColumn(
            //    name: "UserTokenId",
            //    table: "StreamingUserStateManagement",
            //    newName: "USerTokenId");

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsUSserActive",
            //    table: "StreamingUserStateManagement",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "LastUodatedDate",
            //    table: "StreamingUserStateManagement",
            //    type: "datetime2",
            //    nullable: false,
            //    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
