using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class DeviceFactoryDataNewTableCreated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PromoSubscriptionDevices",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedDate",
                table: "PromoSubscriptionDevices",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "PromoSubscriptionDevices",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ModifiedDate",
                table: "PromoSubscriptionDevices",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceFactoryData",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    DeviceName = table.Column<string>(nullable: true),
                    Result = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceFactoryData", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceFactoryData");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PromoSubscriptionDevices");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "PromoSubscriptionDevices");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "PromoSubscriptionDevices");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "PromoSubscriptionDevices");
        }
    }
}
