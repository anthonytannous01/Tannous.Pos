using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Links ingredients to a preferred supplier for demand-driven PO suggestions.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260610150000_AddIngredientPreferredSupplier")]
public partial class AddIngredientPreferredSupplier : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PreferredSupplierId",
            table: "Ingredients",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Ingredients_PreferredSupplierId",
            table: "Ingredients",
            column: "PreferredSupplierId");

        migrationBuilder.AddForeignKey(
            name: "FK_Ingredients_Suppliers_PreferredSupplierId",
            table: "Ingredients",
            column: "PreferredSupplierId",
            principalTable: "Suppliers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Ingredients_Suppliers_PreferredSupplierId",
            table: "Ingredients");

        migrationBuilder.DropIndex(
            name: "IX_Ingredients_PreferredSupplierId",
            table: "Ingredients");

        migrationBuilder.DropColumn(
            name: "PreferredSupplierId",
            table: "Ingredients");
    }
}
