using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddcolInvoiceIdZohoSubscriptionHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "ZohoSubscriptionHistories",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "ZohoSubscriptionHistories",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IntervalUnit",
                table: "ZohoSubscriptionHistories",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceId",
                table: "ZohoSubscriptionHistories",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "ZohoSubscriptionHistories");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "ZohoSubscriptionHistories");

            migrationBuilder.DropColumn(
                name: "IntervalUnit",
                table: "ZohoSubscriptionHistories");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "ZohoSubscriptionHistories");
        }
    }
}
