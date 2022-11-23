using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddOnCodeAddInFeatureSubscriptionAssociation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NextBillingAt",
                table: "ZohoSubscriptionHistories",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "FeatureSubscriptionAssociations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextBillingAt",
                table: "ZohoSubscriptionHistories");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "FeatureSubscriptionAssociations");
        }
    }
}
