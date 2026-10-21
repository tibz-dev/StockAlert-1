using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockAlert.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStocktakeReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StocktakeSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Zone = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BlindCount = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceSnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StocktakeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StocktakeSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalProductId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UnitValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedQuantity = table.Column<int>(type: "int", nullable: false),
                    FirstCount = table.Column<int>(type: "int", nullable: true),
                    FirstCountedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FirstCountedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecondCount = table.Column<int>(type: "int", nullable: true),
                    SecondCountedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SecondCountedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalCount = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResolutionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StocktakeItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StocktakeItems_StocktakeSessions_StocktakeSessionId",
                        column: x => x.StocktakeSessionId,
                        principalTable: "StocktakeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeItems_ProductId",
                table: "StocktakeItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeItems_StocktakeSessionId_Barcode",
                table: "StocktakeItems",
                columns: new[] { "StocktakeSessionId", "Barcode" });

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_CreatedAt",
                table: "StocktakeSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSessions_ReferenceNumber",
                table: "StocktakeSessions",
                column: "ReferenceNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StocktakeItems");

            migrationBuilder.DropTable(
                name: "StocktakeSessions");
        }
    }
}
