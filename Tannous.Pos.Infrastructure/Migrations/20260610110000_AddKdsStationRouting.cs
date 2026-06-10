using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// KDS station routing: KdsStations table and MenuItem.KdsStationId FK.
/// Deleting a station sets MenuItem.KdsStationId to null (SetNull).
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260610110000_AddKdsStationRouting")]
public partial class AddKdsStationRouting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "KdsStations",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                Name         = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                NameAr       = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Color        = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                IsActive     = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                BranchId     = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy    = table.Column<string>(type: "text", nullable: true),
                UpdatedBy    = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KdsStations", x => x.Id);
                table.ForeignKey(
                    name: "FK_KdsStations_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_KdsStations_BranchId_IsActive",
            table: "KdsStations",
            columns: new[] { "BranchId", "IsActive" });

        migrationBuilder.AddColumn<Guid>(
            name: "KdsStationId",
            table: "MenuItems",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_MenuItems_KdsStationId",
            table: "MenuItems",
            column: "KdsStationId");

        migrationBuilder.AddForeignKey(
            name: "FK_MenuItems_KdsStations_KdsStationId",
            table: "MenuItems",
            column: "KdsStationId",
            principalTable: "KdsStations",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MenuItems_KdsStations_KdsStationId",
            table: "MenuItems");

        migrationBuilder.DropIndex(
            name: "IX_MenuItems_KdsStationId",
            table: "MenuItems");

        migrationBuilder.DropColumn(
            name: "KdsStationId",
            table: "MenuItems");

        migrationBuilder.DropTable(
            name: "KdsStations");
    }
}
