using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// QuickBooks / Xero accounting sync: OAuth connections and per-day sync audit records.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260610130000_AddAccountingSyncEntities")]
public partial class AddAccountingSyncEntities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccountingConnections",
            columns: table => new
            {
                Id                   = table.Column<Guid>(type: "uuid", nullable: false),
                Provider             = table.Column<int>(type: "integer", nullable: false),
                BranchId             = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive             = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                AccessToken          = table.Column<string>(type: "text", nullable: false),
                RefreshToken         = table.Column<string>(type: "text", nullable: false),
                AccessTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompanyId            = table.Column<string>(type: "text", nullable: false),
                CompanyName          = table.Column<string>(type: "text", nullable: true),
                LastSyncAt           = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastSyncError        = table.Column<string>(type: "text", nullable: true),
                CreatedAt            = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt            = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy            = table.Column<string>(type: "text", nullable: true),
                UpdatedBy            = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountingConnections", x => x.Id);
                table.ForeignKey(
                    name: "FK_AccountingConnections_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "AccountingSyncRecords",
            columns: table => new
            {
                Id                = table.Column<Guid>(type: "uuid", nullable: false),
                Provider          = table.Column<int>(type: "integer", nullable: false),
                BranchId          = table.Column<Guid>(type: "uuid", nullable: true),
                SyncDate          = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsSuccess         = table.Column<bool>(type: "boolean", nullable: false),
                ExternalReference = table.Column<string>(type: "text", nullable: true),
                ErrorMessage      = table.Column<string>(type: "text", nullable: true),
                SyncedAt          = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt         = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt         = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy         = table.Column<string>(type: "text", nullable: true),
                UpdatedBy         = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountingSyncRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_AccountingSyncRecords_Branches_BranchId",
                    column: x => x.BranchId,
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AccountingConnections_Provider_BranchId",
            table: "AccountingConnections",
            columns: new[] { "Provider", "BranchId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AccountingSyncRecords_Provider_BranchId_SyncDate",
            table: "AccountingSyncRecords",
            columns: new[] { "Provider", "BranchId", "SyncDate" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AccountingSyncRecords");
        migrationBuilder.DropTable(name: "AccountingConnections");
    }
}
