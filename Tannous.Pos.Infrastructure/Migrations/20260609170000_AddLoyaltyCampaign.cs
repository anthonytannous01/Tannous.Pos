using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Loyalty CRM: LoyaltyCampaigns table for WhatsApp campaign dispatch to behavioural segments.
///
/// Additive only. No existing data is affected. TargetSegment and Status are stored as int enums.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260609170000_AddLoyaltyCampaign")]
public partial class AddLoyaltyCampaign : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LoyaltyCampaigns",
            columns: table => new
            {
                Id              = table.Column<Guid>(type: "uuid", nullable: false),
                Name            = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Message         = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                TargetSegment   = table.Column<int>(type: "integer", nullable: false),
                RecipientCount  = table.Column<int>(type: "integer", nullable: false),
                SentCount       = table.Column<int>(type: "integer", nullable: false),
                Status          = table.Column<int>(type: "integer", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                SentAt          = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ErrorMessage    = table.Column<string>(type: "text", nullable: true),
                CreatedAt       = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt       = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy       = table.Column<string>(type: "text", nullable: true),
                UpdatedBy       = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoyaltyCampaigns", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LoyaltyCampaigns_CreatedAt",
            table: "LoyaltyCampaigns",
            column: "CreatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LoyaltyCampaigns");
    }
}
