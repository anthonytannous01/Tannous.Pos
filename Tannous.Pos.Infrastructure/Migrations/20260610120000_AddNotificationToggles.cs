using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Per-feature WhatsApp/SMS notification toggles on BusinessSettings.
/// Both default to false (opt-in).
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260610120000_AddNotificationToggles")]
public partial class AddNotificationToggles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "NotifyOnLoyaltyEarn",
            table: "BusinessSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "NotifyOnReservationConfirm",
            table: "BusinessSettings",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "NotifyOnLoyaltyEarn", table: "BusinessSettings");
        migrationBuilder.DropColumn(name: "NotifyOnReservationConfirm", table: "BusinessSettings");
    }
}
