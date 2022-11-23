using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class PBI2103ZeroRuppeePlanforWSDaddingcertificateId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhysicalDeviceId",
                table: "ZohoSubscriptionHistories",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhysicalDeviceId",
                table: "ZohoSubscriptionHistories");
        }
    }
}
