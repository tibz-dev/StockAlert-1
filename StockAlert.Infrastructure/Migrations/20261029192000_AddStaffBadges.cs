using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StockAlert.Infrastructure.Persistence;

#nullable disable
namespace StockAlert.Infrastructure.Migrations;
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261029192000_AddStaffBadges")]
public partial class AddStaffBadges : Migration
{
    protected override void Up(MigrationBuilder builder)
    {
        builder.CreateTable("StaffBadges", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            BadgeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
            IsRevoked = table.Column<bool>(type: "bit", nullable: false),
            CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_StaffBadges", x => x.Id);
            table.ForeignKey("FK_StaffBadges_AspNetUsers_UserId", x => x.UserId,
                "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
        });
        builder.CreateIndex("IX_StaffBadges_BadgeHash", "StaffBadges", "BadgeHash", unique: true);
        builder.CreateIndex("IX_StaffBadges_UserId", "StaffBadges", "UserId");
    }
    protected override void Down(MigrationBuilder builder) =>
        builder.DropTable("StaffBadges");
}
