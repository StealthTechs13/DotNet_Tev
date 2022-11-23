using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddedStreamingUserStateManagementtable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateTable(
            //    name: "SDCardHistory",
            //    columns: table => new
            //    {
            //        Id = table.Column<string>(nullable: false),
            //        CreatedDate = table.Column<long>(nullable: false),
            //        CreatedBy = table.Column<string>(nullable: true),
            //        ModifiedDate = table.Column<long>(nullable: true),
            //        ModifiedBy = table.Column<string>(nullable: true),
            //        deviceId = table.Column<string>(nullable: true),
            //        date = table.Column<string>(nullable: true),
            //        startTime = table.Column<string>(nullable: true),
            //        endTime = table.Column<string>(nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_SDCardHistory", x => x.Id);
            //    });

            migrationBuilder.CreateTable(
                name: "StreamingUserStateManagement",
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
                    USerTokenId = table.Column<string>(nullable: true),
                    LastUodatedDate = table.Column<DateTime>(nullable: false),
                    LiveStreamingState = table.Column<bool>(nullable: false),
                    PlaybackStreamingState = table.Column<bool>(nullable: false),
                    IsUSserActive = table.Column<bool>(nullable: false),
                    OrgId = table.Column<string>(nullable: true)

                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamingUserStateManagement", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "SDCardHistory");

            migrationBuilder.DropTable(
                name: "StreamingUserStateManagement");
        }
    }
}
