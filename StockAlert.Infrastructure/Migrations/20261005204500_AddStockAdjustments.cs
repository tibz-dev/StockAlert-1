using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StockAlert.Infrastructure.Persistence;

#nullable disable

namespace StockAlert.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261005204500_AddStockAdjustments")]
public partial class AddStockAdjustments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StockAdjustments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PreviousQuantity = table.Column<int>(type: "int", nullable: false),
                QuantityChange = table.Column<int>(type: "int", nullable: false),
                NewQuantity = table.Column<int>(type: "int", nullable: false),
                Reason = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: false),
                PerformedBy = table.Column<string>(
                    type: "nvarchar(256)",
                    maxLength: 256,
                    nullable: false),
                CreatedAt = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                table.ForeignKey(
                    name: "FK_StockAdjustments_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StockAdjustments_ProductId",
            table: "StockAdjustments",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_StockAdjustments_CreatedAt",
            table: "StockAdjustments",
            column: "CreatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "StockAdjustments");
    }
}
