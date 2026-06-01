using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(PosDbContext))]
[Migration("20260513121500_AddRowVersionConcurrencyToOrdersInventoryItemsShifts")]
public partial class AddRowVersionConcurrencyToOrdersInventoryItemsShifts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "Orders",
            type: "bytea",
            nullable: false,
            defaultValue: new byte[8]);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "InventoryItems",
            type: "bytea",
            nullable: false,
            defaultValue: new byte[8]);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "Shifts",
            type: "bytea",
            nullable: false,
            defaultValue: new byte[8]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "InventoryItems");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "Shifts");
    }
}
