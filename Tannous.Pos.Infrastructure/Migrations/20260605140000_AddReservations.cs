using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Table Reservation system: Reservations table with FK to Tables and Branches.
/// Fully additive — no existing data touched.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260605140000_AddReservations")]
public partial class AddReservations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Reservations",
            columns: table => new
            {
                Id                  = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerName        = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CustomerPhone       = table.Column<string>(type: "character varying(50)",  maxLength: 50,  nullable: true),
                PartySize           = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                ReservationDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Notes               = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Status              = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                TableId             = table.Column<Guid>(type: "uuid", nullable: true),
                BranchId            = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt           = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt           = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy           = table.Column<string>(type: "text", nullable: true),
                UpdatedBy           = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Reservations", x => x.Id);
                table.ForeignKey("FK_Reservations_Tables_TableId",
                    x => x.TableId, "Tables", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_Reservations_Branches_BranchId",
                    x => x.BranchId, "Branches", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_Reservations_ReservationDateTime", "Reservations", "ReservationDateTime");
        migrationBuilder.CreateIndex("IX_Reservations_Status",              "Reservations", "Status");
        migrationBuilder.CreateIndex("IX_Reservations_BranchId",            "Reservations", "BranchId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "Reservations");
}
