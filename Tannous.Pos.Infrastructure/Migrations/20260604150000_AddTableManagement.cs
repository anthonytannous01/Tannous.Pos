using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Table Management: FloorPlans + Tables entities, nullable TableId on Orders.
/// All additive. Existing orders get TableId = NULL (correct — they predate table assignment).
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260604150000_AddTableManagement")]
public partial class AddTableManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── FloorPlans ───────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "FloorPlans",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                Name         = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description  = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                IsActive     = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy    = table.Column<string>(type: "text", nullable: true),
                UpdatedBy    = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_FloorPlans", x => x.Id));

        // ── Tables ───────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Tables",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                TableNumber  = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Label        = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Capacity     = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                Status       = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                IsActive     = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                FloorPlanId  = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy    = table.Column<string>(type: "text", nullable: true),
                UpdatedBy    = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tables", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tables_FloorPlans_FloorPlanId",
                    column: x => x.FloorPlanId,
                    principalTable: "FloorPlans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Tables_FloorPlanId", "Tables", "FloorPlanId");

        // ── Orders: add nullable TableId ─────────────────────────────────────
        migrationBuilder.AddColumn<Guid>(
            name: "TableId",
            table: "Orders",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Orders_Tables_TableId",
            table: "Orders",
            column: "TableId",
            principalTable: "Tables",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.CreateIndex("IX_Orders_TableId", "Orders", "TableId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_Orders_TableId", "Orders");
        migrationBuilder.DropForeignKey("FK_Orders_Tables_TableId", "Orders");
        migrationBuilder.DropColumn(name: "TableId", table: "Orders");
        migrationBuilder.DropTable(name: "Tables");
        migrationBuilder.DropTable(name: "FloorPlans");
    }
}
