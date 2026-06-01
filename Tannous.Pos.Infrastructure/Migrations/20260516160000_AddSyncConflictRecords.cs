using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>Internal sync reconciliation conflict records for operational diagnostics.</summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260516160000_AddSyncConflictRecords")]
public partial class AddSyncConflictRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SyncConflictRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                OperationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                OperationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                ConflictType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                CorrelationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Resolved = table.Column<bool>(type: "boolean", nullable: false),
                ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ResolutionNotes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncConflictRecords", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SyncConflictRecords_DeviceId_OperationId_ConflictType",
            table: "SyncConflictRecords",
            columns: new[] { "DeviceId", "OperationId", "ConflictType" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SyncConflictRecords");
    }
}
