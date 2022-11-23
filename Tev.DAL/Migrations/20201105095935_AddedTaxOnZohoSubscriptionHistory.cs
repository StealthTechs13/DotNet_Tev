using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddedTaxOnZohoSubscriptionHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CGSTAmount",
                table: "ZohoSubscriptionHistories",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "CGSTName",
                table: "ZohoSubscriptionHistories",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SGSTAmount",
                table: "ZohoSubscriptionHistories",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "SGSTName",
                table: "ZohoSubscriptionHistories",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CGSTAmount",
                table: "ZohoSubscriptionHistories");

            migrationBuilder.DropColumn(
                name: "CGSTName",
                table: "ZohoSubscriptionHistories");

            migrationBuilder.DropColumn(
                name: "SGSTAmount",
                table: "ZohoSubscriptionHistories");

            migrationBuilder.DropColumn(
                name: "SGSTName",
                table: "ZohoSubscriptionHistories");
        }
    }
}
