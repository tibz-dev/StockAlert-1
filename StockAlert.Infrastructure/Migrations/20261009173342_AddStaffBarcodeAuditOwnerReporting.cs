using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockAlert.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffBarcodeAuditOwnerReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SalespersonId",
                table: "Sales",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalespersonName",
                table: "Sales",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercentage",
                table: "Quotes",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "SalespersonId",
                table: "Quotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalespersonName",
                table: "Quotes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Products",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EntityName",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OwnerReportSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DailyEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WeeklyEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyEnabled = table.Column<bool>(type: "bit", nullable: false),
                    YearlyEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SendHourLocal = table.Column<int>(type: "int", nullable: false),
                    WeeklyDay = table.Column<int>(type: "int", nullable: false),
                    MonthlyDay = table.Column<int>(type: "int", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastDailySentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastWeeklySentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMonthlySentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastYearlySentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerReportSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SalespersonId",
                table: "Sales",
                column: "SalespersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_SalespersonId",
                table: "Quotes",
                column: "SalespersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_Action",
                table: "AuditLogs",
                columns: new[] { "EntityName", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_Email",
                table: "StaffMembers",
                column: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_StaffMembers_SalespersonId",
                table: "Quotes",
                column: "SalespersonId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_StaffMembers_SalespersonId",
                table: "Sales",
                column: "SalespersonId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_StaffMembers_SalespersonId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_StaffMembers_SalespersonId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "OwnerReportSettings");

            migrationBuilder.DropTable(
                name: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SalespersonId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_SalespersonId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityName_Action",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SalespersonId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SalespersonName",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DepositPercentage",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "SalespersonId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "SalespersonName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "EntityName",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
