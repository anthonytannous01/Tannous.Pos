using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Multi-Branch: creates Branches table, adds nullable BranchId FK to all operational
/// aggregates, inserts a "Main Branch" default, and backfills existing rows.
/// BusinessSettings gains DefaultBranchId.
/// All existing data is attributed to the default branch — fully safe, no data loss.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260605120000_AddMultiBranch")]
public partial class AddMultiBranch : Migration
{
    // Fixed GUID for the seed "Main Branch" — deterministic so the migration is idempotent.
    private static readonly Guid DefaultBranchId = new("00000000-0000-0000-0000-000000000001");

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. Create Branches table ─────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Branches",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                Name         = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Address      = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                Phone        = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                IsActive     = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                IsDefault    = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy    = table.Column<string>(type: "text", nullable: true),
                UpdatedBy    = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Branches", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Branches_IsDefault",
            table: "Branches",
            column: "IsDefault");

        // ── 2. Seed default "Main Branch" (raw SQL avoids model-snapshot dependency) ──
        migrationBuilder.Sql($"""
            INSERT INTO "Branches" ("Id", "Name", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt")
            VALUES ('{DefaultBranchId}', 'Main Branch', true, true, 0, NOW() AT TIME ZONE 'UTC')
            ON CONFLICT DO NOTHING;
            """);

        // ── 3. Add BranchId column to each aggregate ─────────────────────────
        foreach (var table in new[] { "Orders", "Shifts", "InventoryItems",
                                       "WastageRecords", "PurchaseOrders",
                                       "GoodsReceipts", "InventoryMovements" })
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: table,
                type: "uuid",
                nullable: true);
        }

        // ── 4. Backfill existing rows → default branch ────────────────────────
        foreach (var table in new[] { "Orders", "Shifts", "InventoryItems",
                                       "WastageRecords", "PurchaseOrders",
                                       "GoodsReceipts", "InventoryMovements" })
        {
            migrationBuilder.Sql(
                $"UPDATE \"{table}\" SET \"BranchId\" = '{DefaultBranchId}' WHERE \"BranchId\" IS NULL;");
        }

        // ── 5. Add FK constraints ─────────────────────────────────────────────
        foreach (var (table, fkName) in new[]
        {
            ("Orders",            "FK_Orders_Branches_BranchId"),
            ("Shifts",            "FK_Shifts_Branches_BranchId"),
            ("InventoryItems",    "FK_InventoryItems_Branches_BranchId"),
            ("WastageRecords",    "FK_WastageRecords_Branches_BranchId"),
            ("PurchaseOrders",    "FK_PurchaseOrders_Branches_BranchId"),
            ("GoodsReceipts",     "FK_GoodsReceipts_Branches_BranchId"),
            ("InventoryMovements","FK_InventoryMovements_Branches_BranchId"),
        })
        {
            migrationBuilder.AddForeignKey(
                name: fkName,
                table: table,
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        // ── 6. Indexes on BranchId ────────────────────────────────────────────
        migrationBuilder.CreateIndex("IX_Orders_BranchId",            "Orders",            "BranchId");
        migrationBuilder.CreateIndex("IX_Shifts_BranchId",            "Shifts",            "BranchId");
        migrationBuilder.CreateIndex("IX_InventoryItems_BranchId",    "InventoryItems",    "BranchId");
        migrationBuilder.CreateIndex("IX_WastageRecords_BranchId",    "WastageRecords",    "BranchId");
        migrationBuilder.CreateIndex("IX_PurchaseOrders_BranchId",    "PurchaseOrders",    "BranchId");
        migrationBuilder.CreateIndex("IX_GoodsReceipts_BranchId",     "GoodsReceipts",     "BranchId");
        migrationBuilder.CreateIndex("IX_InventoryMovements_BranchId","InventoryMovements","BranchId");

        // ── 7. DefaultBranchId on BusinessSettings ────────────────────────────
        migrationBuilder.AddColumn<Guid>(
            name: "DefaultBranchId",
            table: "BusinessSettings",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            $"UPDATE \"BusinessSettings\" SET \"DefaultBranchId\" = '{DefaultBranchId}';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DefaultBranchId", table: "BusinessSettings");

        foreach (var (table, fkName) in new[]
        {
            ("Orders",            "FK_Orders_Branches_BranchId"),
            ("Shifts",            "FK_Shifts_Branches_BranchId"),
            ("InventoryItems",    "FK_InventoryItems_Branches_BranchId"),
            ("WastageRecords",    "FK_WastageRecords_Branches_BranchId"),
            ("PurchaseOrders",    "FK_PurchaseOrders_Branches_BranchId"),
            ("GoodsReceipts",     "FK_GoodsReceipts_Branches_BranchId"),
            ("InventoryMovements","FK_InventoryMovements_Branches_BranchId"),
        })
        {
            migrationBuilder.DropForeignKey(name: fkName, table: table);
        }

        foreach (var (table, ix) in new[]
        {
            ("Orders",            "IX_Orders_BranchId"),
            ("Shifts",            "IX_Shifts_BranchId"),
            ("InventoryItems",    "IX_InventoryItems_BranchId"),
            ("WastageRecords",    "IX_WastageRecords_BranchId"),
            ("PurchaseOrders",    "IX_PurchaseOrders_BranchId"),
            ("GoodsReceipts",     "IX_GoodsReceipts_Branches_BranchId"),
            ("InventoryMovements","IX_InventoryMovements_BranchId"),
        })
        {
            migrationBuilder.DropIndex(name: ix, table: table);
        }

        foreach (var table in new[] { "Orders", "Shifts", "InventoryItems",
                                       "WastageRecords", "PurchaseOrders",
                                       "GoodsReceipts", "InventoryMovements" })
        {
            migrationBuilder.DropColumn(name: "BranchId", table: table);
        }

        migrationBuilder.DropTable(name: "Branches");
    }
}
