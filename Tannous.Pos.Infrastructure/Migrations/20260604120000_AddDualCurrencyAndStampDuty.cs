using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Lebanese market: dual LBP/USD currency support and 2025 Budget Law stamp duty.
///
/// Changes:
///   Orders        — StampDutyAmount (decimal 18,2, default 0)
///   Payments      — TenderedCurrency (varchar 8, default 'USD'), ExchangeRateUsed (decimal 18,4 nullable), AmountInUsd (decimal 18,4)
///   BusinessSettings — ExchangeRateLbpPerUsd (decimal 18,2, default 0), ShowLbpOnReceipt (bool, default false),
///                      StampDutyEnabled (bool, default false), StampDutyAmountUsd (decimal 18,2, default 2.00)
///
/// All additive (no breaking changes). Existing rows receive safe default values.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260604120000_AddDualCurrencyAndStampDuty")]
public partial class AddDualCurrencyAndStampDuty : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Orders ──────────────────────────────────────────────────────────
        migrationBuilder.AddColumn<decimal>(
            name: "StampDutyAmount",
            table: "Orders",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        // ── Payments ─────────────────────────────────────────────────────────
        migrationBuilder.AddColumn<string>(
            name: "TenderedCurrency",
            table: "Payments",
            type: "character varying(8)",
            maxLength: 8,
            nullable: false,
            defaultValue: "USD");

        migrationBuilder.AddColumn<decimal>(
            name: "ExchangeRateUsed",
            table: "Payments",
            type: "numeric(18,4)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "AmountInUsd",
            table: "Payments",
            type: "numeric(18,4)",
            nullable: false,
            defaultValue: 0m);

        // Backfill AmountInUsd = Amount for all existing USD payment rows
        migrationBuilder.Sql(
            """
            UPDATE "Payments"
            SET "AmountInUsd" = "Amount"
            WHERE "TenderedCurrency" = 'USD';
            """);

        // ── BusinessSettings ─────────────────────────────────────────────────
        migrationBuilder.AddColumn<decimal>(
            name: "ExchangeRateLbpPerUsd",
            table: "BusinessSettings",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<bool>(
            name: "ShowLbpOnReceipt",
            table: "BusinessSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "StampDutyEnabled",
            table: "BusinessSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<decimal>(
            name: "StampDutyAmountUsd",
            table: "BusinessSettings",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 2.00m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // BusinessSettings
        migrationBuilder.DropColumn(name: "StampDutyAmountUsd",      table: "BusinessSettings");
        migrationBuilder.DropColumn(name: "StampDutyEnabled",         table: "BusinessSettings");
        migrationBuilder.DropColumn(name: "ShowLbpOnReceipt",         table: "BusinessSettings");
        migrationBuilder.DropColumn(name: "ExchangeRateLbpPerUsd",    table: "BusinessSettings");

        // Payments
        migrationBuilder.DropColumn(name: "AmountInUsd",              table: "Payments");
        migrationBuilder.DropColumn(name: "ExchangeRateUsed",         table: "Payments");
        migrationBuilder.DropColumn(name: "TenderedCurrency",         table: "Payments");

        // Orders
        migrationBuilder.DropColumn(name: "StampDutyAmount",          table: "Orders");
    }
}
