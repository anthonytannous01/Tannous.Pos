using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>Append-only operational audit trail for money, inventory, replay, and lifecycle forensics.</summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260517120000_AddOperationalAuditRecords")]
public partial class AddOperationalAuditRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OperationalAuditRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                OperationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CorrelationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Summary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                MetadataJson = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OperationalAuditRecords", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OperationalAuditRecords_OrderId_CreatedAtUtc",
            table: "OperationalAuditRecords",
            columns: new[] { "OrderId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_OperationalAuditRecords_DeviceId_OperationId_Action",
            table: "OperationalAuditRecords",
            columns: new[] { "DeviceId", "OperationId", "Action" });

        migrationBuilder.CreateIndex(
            name: "IX_OperationalAuditRecords_EntityType_EntityId_CreatedAtUtc",
            table: "OperationalAuditRecords",
            columns: new[] { "EntityType", "EntityId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OperationalAuditRecords");
    }
}
