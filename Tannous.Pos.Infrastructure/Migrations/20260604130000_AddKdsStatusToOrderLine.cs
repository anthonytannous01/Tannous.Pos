using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Kitchen Display System: adds KDS lifecycle fields to OrderLines.
///
/// Changes:
///   OrderLines — KdsStatus (int, default 0 = Pending), KdsAcknowledgedAt (timestamp nullable),
///                KdsDoneAt (timestamp nullable)
///
/// All additive. Existing rows get KdsStatus = 0 (Pending) which is correct:
/// historical lines are treated as unprocessed for display purposes.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260604130000_AddKdsStatusToOrderLine")]
public partial class AddKdsStatusToOrderLine : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "KdsStatus",
            table: "OrderLines",
            type: "integer",
            nullable: false,
            defaultValue: 0); // KdsStatus.Pending

        migrationBuilder.AddColumn<DateTime>(
            name: "KdsAcknowledgedAt",
            table: "OrderLines",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "KdsDoneAt",
            table: "OrderLines",
            type: "timestamp with time zone",
            nullable: true);

        // Index for fast KDS polling — only active (Pending/InProgress) lines matter
        migrationBuilder.CreateIndex(
            name: "IX_OrderLines_KdsStatus",
            table: "OrderLines",
            column: "KdsStatus");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OrderLines_KdsStatus",
            table: "OrderLines");

        migrationBuilder.DropColumn(name: "KdsDoneAt",         table: "OrderLines");
        migrationBuilder.DropColumn(name: "KdsAcknowledgedAt", table: "OrderLines");
        migrationBuilder.DropColumn(name: "KdsStatus",         table: "OrderLines");
    }
}
