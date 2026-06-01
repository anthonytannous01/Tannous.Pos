using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>Adds settlement fields (tendered / change due / net captured) to Orders.</summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260516140000_AddOrderSettlementFields")]
public partial class AddOrderSettlementFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "AmountTendered",
            table: "Orders",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "ChangeDue",
            table: "Orders",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "NetCapturedAmount",
            table: "Orders",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AmountTendered", table: "Orders");
        migrationBuilder.DropColumn(name: "ChangeDue", table: "Orders");
        migrationBuilder.DropColumn(name: "NetCapturedAmount", table: "Orders");
    }
}
