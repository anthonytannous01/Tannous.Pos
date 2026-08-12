using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// HAND-EDITED after generation. The model snapshot had drifted months behind the live
    /// database (Branches, KDS, loyalty, Arabic fields, payment currency columns were applied
    /// to the DB outside the migrations workflow), so the scaffolded Up() tried to re-create
    /// dozens of objects that already exist and failed on the first duplicate column.
    /// This migration's operations were reduced to ONLY the genuinely new dual-currency
    /// drawer columns; its Designer snapshot intentionally captures the FULL current model,
    /// which re-baselines EF against the real database. Future migrations will be clean.
    /// </remarks>
    public partial class DualCurrencyDrawer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Shifts: per-currency drawer reconciliation ─────────────────────────
            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalanceLbp",
                table: "Shifts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedCashLbp",
                table: "Shifts",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualCashLbp",
                table: "Shifts",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CashDifferenceLbp",
                table: "Shifts",
                type: "numeric(18,2)",
                nullable: true);

            // ── Orders: change currency chosen by cashier per sale ─────────────────
            migrationBuilder.AddColumn<string>(
                name: "ChangeCurrency",
                table: "Orders",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "ChangeAmountInCurrency",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            // ── CashDrawerEvents: currency of drops/counts ─────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "CashDrawerEvents",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "USD");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OpeningBalanceLbp", table: "Shifts");
            migrationBuilder.DropColumn(name: "ExpectedCashLbp", table: "Shifts");
            migrationBuilder.DropColumn(name: "ActualCashLbp", table: "Shifts");
            migrationBuilder.DropColumn(name: "CashDifferenceLbp", table: "Shifts");
            migrationBuilder.DropColumn(name: "ChangeCurrency", table: "Orders");
            migrationBuilder.DropColumn(name: "ChangeAmountInCurrency", table: "Orders");
            migrationBuilder.DropColumn(name: "Currency", table: "CashDrawerEvents");
        }
    }
}
