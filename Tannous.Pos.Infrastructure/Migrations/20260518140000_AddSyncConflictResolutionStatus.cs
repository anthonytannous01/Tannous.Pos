using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>Additive reconciliation workflow fields on SyncConflictRecords (operator diagnostics).</summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260518140000_AddSyncConflictResolutionStatus")]
public partial class AddSyncConflictResolutionStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ResolutionStatus",
            table: "SyncConflictRecords",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Unresolved");

        migrationBuilder.AddColumn<string>(
            name: "ResolvedBy",
            table: "SyncConflictRecords",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "SyncConflictRecords"
            SET "ResolutionStatus" = 'Resolved'
            WHERE "Resolved" = TRUE AND ("ResolutionStatus" IS NULL OR "ResolutionStatus" = 'Unresolved');
            """);

        migrationBuilder.CreateIndex(
            name: "IX_SyncConflictRecords_ResolutionStatus_CreatedAtUtc",
            table: "SyncConflictRecords",
            columns: new[] { "ResolutionStatus", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SyncConflictRecords_ResolutionStatus_CreatedAtUtc",
            table: "SyncConflictRecords");

        migrationBuilder.DropColumn(name: "ResolvedBy", table: "SyncConflictRecords");
        migrationBuilder.DropColumn(name: "ResolutionStatus", table: "SyncConflictRecords");
    }
}
