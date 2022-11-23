using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class ChangesInZohoSubscriptionHistoriesV3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "FeatureSubscriptionAssociations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubscriptionId",
                table: "FeatureSubscriptionAssociations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
