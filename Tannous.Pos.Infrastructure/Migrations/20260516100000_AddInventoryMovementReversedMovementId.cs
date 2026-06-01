using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>Adds optional link from reversal movements to original finalize sale deductions.</summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260516100000_AddInventoryMovementReversedMovementId")]
public partial class AddInventoryMovementReversedMovementId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ReversedMovementId",
            table: "InventoryMovements",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_InventoryMovements_ReversedMovementId",
            table: "InventoryMovements",
            column: "ReversedMovementId");

        migrationBuilder.AddForeignKey(
            name: "FK_InventoryMovements_InventoryMovements_ReversedMovementId",
            table: "InventoryMovements",
            column: "ReversedMovementId",
            principalTable: "InventoryMovements",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_InventoryMovements_InventoryMovements_ReversedMovementId",
            table: "InventoryMovements");

        migrationBuilder.DropIndex(
            name: "IX_InventoryMovements_ReversedMovementId",
            table: "InventoryMovements");

        migrationBuilder.DropColumn(
            name: "ReversedMovementId",
            table: "InventoryMovements");
    }
}
