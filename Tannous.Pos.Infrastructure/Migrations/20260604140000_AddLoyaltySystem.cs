using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Loyalty Phase 1: LoyaltyAccounts, LoyaltyTransactions tables,
/// and loyalty configuration fields on BusinessSettings.
///
/// All additive. No existing data is affected.
/// LoyaltyAccount has a unique index on CustomerId (one account per customer).
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260604140000_AddLoyaltySystem")]
public partial class AddLoyaltySystem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── BusinessSettings: loyalty config ─────────────────────────────────
        migrationBuilder.AddColumn<bool>(
            name: "LoyaltyEnabled",
            table: "BusinessSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "LoyaltyPointsPerDollar",
            table: "BusinessSettings",
            type: "integer",
            nullable: false,
            defaultValue: 10);

        migrationBuilder.AddColumn<decimal>(
            name: "LoyaltyPointValueUsd",
            table: "BusinessSettings",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 0.01m);

        migrationBuilder.AddColumn<int>(
            name: "LoyaltyMinRedeemPoints",
            table: "BusinessSettings",
            type: "integer",
            nullable: false,
            defaultValue: 100);

        // ── LoyaltyAccounts ──────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "LoyaltyAccounts",
            columns: table => new
            {
                Id                     = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId             = table.Column<Guid>(type: "uuid", nullable: false),
                PointBalance           = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LifetimePointsEarned   = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LifetimePointsRedeemed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                IsActive               = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt              = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt              = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy              = table.Column<string>(type: "text", nullable: true),
                UpdatedBy              = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoyaltyAccounts", x => x.Id);
                table.ForeignKey(
                    name: "FK_LoyaltyAccounts_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LoyaltyAccounts_CustomerId",
            table: "LoyaltyAccounts",
            column: "CustomerId",
            unique: true);

        // ── LoyaltyTransactions ──────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "LoyaltyTransactions",
            columns: table => new
            {
                Id               = table.Column<Guid>(type: "uuid", nullable: false),
                LoyaltyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Points           = table.Column<int>(type: "integer", nullable: false),
                TransactionType  = table.Column<int>(type: "integer", nullable: false),
                OrderId          = table.Column<Guid>(type: "uuid", nullable: true),
                Notes            = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                CreatedAt        = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt        = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy        = table.Column<string>(type: "text", nullable: true),
                UpdatedBy        = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoyaltyTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_LoyaltyTransactions_LoyaltyAccounts_LoyaltyAccountId",
                    column: x => x.LoyaltyAccountId,
                    principalTable: "LoyaltyAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LoyaltyTransactions_LoyaltyAccountId",
            table: "LoyaltyTransactions",
            column: "LoyaltyAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_LoyaltyTransactions_OrderId",
            table: "LoyaltyTransactions",
            column: "OrderId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LoyaltyTransactions");
        migrationBuilder.DropTable(name: "LoyaltyAccounts");

        migrationBuilder.DropColumn(name: "LoyaltyMinRedeemPoints",   table: "BusinessSettings");
        migrationBuilder.DropColumn(name: "LoyaltyPointValueUsd",     table: "BusinessSettings");
        migrationBuilder.DropColumn(name: "LoyaltyPointsPerDollar",   table: "BusinessSettings");
        migrationBuilder.DropColumn(name: "LoyaltyEnabled",           table: "BusinessSettings");
    }
}
