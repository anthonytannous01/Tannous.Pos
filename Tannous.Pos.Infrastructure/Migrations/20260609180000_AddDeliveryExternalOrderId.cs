using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Delivery channel integration: external platform order identifiers on DeliveryInfo
/// (ExternalOrderId for dedup, ExternalOrderReference for staff display) + composite index
/// on (Channel, ExternalOrderId) for fast idempotency lookups.
///
/// Additive only. No existing data is affected.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260609180000_AddDeliveryExternalOrderId")]
public partial class AddDeliveryExternalOrderId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ExternalOrderId",
            table: "DeliveryInfos",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExternalOrderReference",
            table: "DeliveryInfos",
            type: "character varying(160)",
            maxLength: 160,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_DeliveryInfos_Channel_ExternalOrderId",
            table: "DeliveryInfos",
            columns: new[] { "Channel", "ExternalOrderId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DeliveryInfos_Channel_ExternalOrderId",
            table: "DeliveryInfos");

        migrationBuilder.DropColumn(name: "ExternalOrderReference", table: "DeliveryInfos");
        migrationBuilder.DropColumn(name: "ExternalOrderId",        table: "DeliveryInfos");
    }
}
