using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class AddInvoiceHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.CreateTable(
                name: "InvoiceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    EventType = table.Column<string>(nullable: true),
                    CreatedTime = table.Column<DateTime>(nullable: false),
                    InvoiceNumber = table.Column<string>(nullable: true),
                    Balance = table.Column<double>(nullable: false),
                    CurrencyCode = table.Column<string>(nullable: true),
                    InvoiceDate = table.Column<DateTime>(nullable: false),
                    Email = table.Column<string>(nullable: true),
                    CustomerId = table.Column<string>(nullable: true),
                    CustomerName = table.Column<string>(nullable: true),
                    InvoiceId = table.Column<string>(nullable: true),
                    Total = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceHistoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    ItemCode = table.Column<string>(nullable: true),
                    Quantity = table.Column<int>(nullable: false),
                    ItemId = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Price = table.Column<double>(nullable: false),
                    InvoiceHistoryFK = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceHistoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceHistoryItems_InvoiceHistories_InvoiceHistoryFK",
                        column: x => x.InvoiceHistoryFK,
                        principalTable: "InvoiceHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceHistoryPayments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    PaymentId = table.Column<string>(nullable: true),
                    Amount = table.Column<double>(nullable: false),
                    AmountRefunded = table.Column<double>(nullable: false),
                    BankCharges = table.Column<double>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    InvoiceHistoryFK = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceHistoryPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceHistoryPayments_InvoiceHistories_InvoiceHistoryFK",
                        column: x => x.InvoiceHistoryFK,
                        principalTable: "InvoiceHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceHistorySubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    SubscriptionId = table.Column<string>(nullable: true),
                    InvoiceHistoryFK = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceHistorySubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceHistorySubscriptions_InvoiceHistories_InvoiceHistoryFK",
                        column: x => x.InvoiceHistoryFK,
                        principalTable: "InvoiceHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHistoryItems_InvoiceHistoryFK",
                table: "InvoiceHistoryItems",
                column: "InvoiceHistoryFK");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHistoryPayments_InvoiceHistoryFK",
                table: "InvoiceHistoryPayments",
                column: "InvoiceHistoryFK");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHistorySubscriptions_InvoiceHistoryFK",
                table: "InvoiceHistorySubscriptions",
                column: "InvoiceHistoryFK");



        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceHistoryItems");

            migrationBuilder.DropTable(
                name: "InvoiceHistoryPayments");

            migrationBuilder.DropTable(
                name: "InvoiceHistorySubscriptions");

            migrationBuilder.DropTable(
                name: "InvoiceHistories");

        }
    }
}
