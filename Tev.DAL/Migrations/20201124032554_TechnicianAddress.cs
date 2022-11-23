using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tev.DAL.Migrations
{
    public partial class TechnicianAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Technicians",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    PaymentCreatedTime = table.Column<DateTime>(nullable: false),
                    EventType = table.Column<string>(nullable: true),
                    PaymentId = table.Column<string>(nullable: true),
                    PaymentNumber = table.Column<string>(nullable: true),
                    PayedAmount = table.Column<double>(nullable: false),
                    PaymentDate = table.Column<DateTime>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    CurrencyCode = table.Column<string>(nullable: true),
                    CustomerId = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    PaymentStatus = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayementInvoiceAssociations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    InvoiceDate = table.Column<DateTime>(nullable: false),
                    InvoiceId = table.Column<string>(nullable: true),
                    TransactionType = table.Column<string>(nullable: true),
                    InvoiceNumber = table.Column<string>(nullable: true),
                    InvoiceAmount = table.Column<double>(nullable: false),
                    AmountApplied = table.Column<double>(nullable: false),
                    BalanceAmount = table.Column<double>(nullable: false),
                    PaymentHistoryId = table.Column<int>(nullable: true),
                    PaymentHistoryFK = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayementInvoiceAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayementInvoiceAssociations_PaymentHistories_PaymentHistoryId",
                        column: x => x.PaymentHistoryId,
                        principalTable: "PaymentHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceSubscriptionAssociations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<long>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    ModifiedDate = table.Column<long>(nullable: true),
                    ModifiedBy = table.Column<string>(nullable: true),
                    InvoiceId = table.Column<string>(nullable: true),
                    SubscriptionId = table.Column<string>(nullable: true),
                    PayementInvoiceAssociationId = table.Column<int>(nullable: true),
                    PayementInvoiceAssociationFK = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceSubscriptionAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceSubscriptionAssociations_PayementInvoiceAssociations_PayementInvoiceAssociationId",
                        column: x => x.PayementInvoiceAssociationId,
                        principalTable: "PayementInvoiceAssociations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSubscriptionAssociations_PayementInvoiceAssociationId",
                table: "InvoiceSubscriptionAssociations",
                column: "PayementInvoiceAssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_PayementInvoiceAssociations_PaymentHistoryId",
                table: "PayementInvoiceAssociations",
                column: "PaymentHistoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceSubscriptionAssociations");

            migrationBuilder.DropTable(
                name: "PayementInvoiceAssociations");

            migrationBuilder.DropTable(
                name: "PaymentHistories");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Technicians");
        }
    }
}
