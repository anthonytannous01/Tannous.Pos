using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>Adds durable sync replay receipts (deviceId + operationId) for high-risk push operations.</summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260514100000_AddSyncOperationReceipts")]
public partial class AddSyncOperationReceipts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SyncOperationReceipts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                OperationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                OperationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Success = table.Column<bool>(type: "boolean", nullable: false),
                Conflict = table.Column<bool>(type: "boolean", nullable: false),
                ServerId = table.Column<string>(type: "text", nullable: true),
                ResultMessage = table.Column<string>(type: "text", nullable: true),
                ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncOperationReceipts", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SyncOperationReceipts_DeviceId_OperationId",
            table: "SyncOperationReceipts",
            columns: new[] { "DeviceId", "OperationId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SyncOperationReceipts");
    }
}
