using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class ChangesInZohoSubscriptionHistoriesV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextBillingAt",
                table: "ZohoSubscriptionHistories");

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "ZohoSubscriptionHistories",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ZohoSubscriptionHistories",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "ZohoSubscriptionHistories");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "ZohoSubscriptionHistories");

            migrationBuilder.AddColumn<DateTime>(
                name: "NextBillingAt",
                table: "ZohoSubscriptionHistories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
