using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddZohoSubscriptionHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZohoSubscriptionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    SubscriptionId = table.Column<string>(nullable: true),
                    ProductName = table.Column<string>(nullable: true),
                    DeviceId = table.Column<string>(nullable: true),
                    DeviceName = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true),
                    CreatedTime = table.Column<DateTime>(nullable: false),
                    EventType = table.Column<string>(nullable: true),
                    NextBillingAt = table.Column<DateTime>(nullable: false),
                    PlanCode = table.Column<string>(nullable: true),
                    PlanName = table.Column<string>(nullable: true),
                    PlanPrice = table.Column<double>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    SubTotal = table.Column<double>(nullable: false),
                    Amount = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZohoSubscriptionHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureSubscriptionAssociations",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    SubscriptionId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Price = table.Column<string>(nullable: true),
                    ZohoSubscriptionHistoryFK = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureSubscriptionAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureSubscriptionAssociations_ZohoSubscriptionHistories_ZohoSubscriptionHistoryFK",
                        column: x => x.ZohoSubscriptionHistoryFK,
                        principalTable: "ZohoSubscriptionHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSubscriptionAssociations_ZohoSubscriptionHistoryFK",
                table: "FeatureSubscriptionAssociations",
                column: "ZohoSubscriptionHistoryFK");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureSubscriptionAssociations");

            migrationBuilder.DropTable(
                name: "ZohoSubscriptionHistories");
        }
    }
}
