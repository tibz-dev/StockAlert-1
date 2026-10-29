using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StockAlert.Infrastructure.Persistence;

#nullable disable

namespace StockAlert.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261029183000_AddCheckoutSchema")]
public partial class AddCheckoutSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CheckoutTransactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReceiptNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ClientOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                VatAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CheckoutTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_CheckoutTransactions_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "CheckoutLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CheckoutTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CheckoutLines", x => x.Id);
                table.ForeignKey("FK_CheckoutLines_CheckoutTransactions_CheckoutTransactionId",
                    x => x.CheckoutTransactionId, "CheckoutTransactions", "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_CheckoutLines_Products_ProductId",
                    x => x.ProductId, "Products", "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CheckoutTenders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CheckoutTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Method = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                ExternalReference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CheckoutTenders", x => x.Id);
                table.ForeignKey("FK_CheckoutTenders_CheckoutTransactions_CheckoutTransactionId",
                    x => x.CheckoutTransactionId, "CheckoutTransactions", "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_CheckoutTransactions_ClientOperationId",
            "CheckoutTransactions", "ClientOperationId", unique: true);
        migrationBuilder.CreateIndex("IX_CheckoutTransactions_ReceiptNumber",
            "CheckoutTransactions", "ReceiptNumber", unique: true);
        migrationBuilder.CreateIndex("IX_CheckoutTransactions_CustomerId",
            "CheckoutTransactions", "CustomerId");
        migrationBuilder.CreateIndex("IX_CheckoutLines_CheckoutTransactionId_ProductId",
            "CheckoutLines", new[] { "CheckoutTransactionId", "ProductId" }, unique: true);
        migrationBuilder.CreateIndex("IX_CheckoutLines_ProductId",
            "CheckoutLines", "ProductId");
        migrationBuilder.CreateIndex("IX_CheckoutTenders_CheckoutTransactionId",
            "CheckoutTenders", "CheckoutTransactionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CheckoutLines");
        migrationBuilder.DropTable(name: "CheckoutTenders");
        migrationBuilder.DropTable(name: "CheckoutTransactions");
    }
}
