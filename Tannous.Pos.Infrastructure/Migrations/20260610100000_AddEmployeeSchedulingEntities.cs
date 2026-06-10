using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Employee scheduling &amp; time tracking: EmployeeSchedules (planned shifts) and
/// TimeEntries (actual clock-in/out). Both reference Users and Branches with Restrict.
///
/// Additive only. No existing data is affected. Status columns are stored as int enums.
/// Distinct from the Shifts table, which remains the cash register session.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260610100000_AddEmployeeSchedulingEntities")]
public partial class AddEmployeeSchedulingEntities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmployeeSchedules",
            columns: table => new
            {
                Id             = table.Column<Guid>(type: "uuid", nullable: false),
                UserId         = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId       = table.Column<Guid>(type: "uuid", nullable: false),
                ScheduledStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ScheduledEnd   = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Position       = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Notes          = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Status         = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                CreatedAt      = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt      = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy      = table.Column<string>(type: "text", nullable: true),
                UpdatedBy      = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmployeeSchedules", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmployeeSchedules_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_EmployeeSchedules_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TimeEntries",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                UserId       = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId     = table.Column<Guid>(type: "uuid", nullable: false),
                ClockIn      = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ClockOut     = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                BreakMinutes = table.Column<int>(type: "integer", nullable: true),
                Notes        = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Status       = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy    = table.Column<string>(type: "text", nullable: true),
                UpdatedBy    = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TimeEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_TimeEntries_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TimeEntries_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeSchedules_ScheduledStart",
            table: "EmployeeSchedules",
            column: "ScheduledStart");

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeSchedules_UserId_ScheduledStart",
            table: "EmployeeSchedules",
            columns: new[] { "UserId", "ScheduledStart" });

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeSchedules_BranchId",
            table: "EmployeeSchedules",
            column: "BranchId");

        migrationBuilder.CreateIndex(
            name: "IX_TimeEntries_UserId_Status",
            table: "TimeEntries",
            columns: new[] { "UserId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_TimeEntries_ClockIn",
            table: "TimeEntries",
            column: "ClockIn");

        migrationBuilder.CreateIndex(
            name: "IX_TimeEntries_BranchId",
            table: "TimeEntries",
            column: "BranchId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmployeeSchedules");
        migrationBuilder.DropTable(name: "TimeEntries");
    }
}
