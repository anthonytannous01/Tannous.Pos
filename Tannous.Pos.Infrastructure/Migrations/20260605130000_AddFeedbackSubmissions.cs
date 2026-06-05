using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Adds FeedbackSubmissions table — customer post-order feedback (rating, category, comment).
/// Nullable FK to Orders and Branches; fully additive, no existing data touched.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260605130000_AddFeedbackSubmissions")]
public partial class AddFeedbackSubmissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FeedbackSubmissions",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                Rating       = table.Column<int>(type: "integer", nullable: false),
                Comment      = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Category     = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                OrderId      = table.Column<Guid>(type: "uuid", nullable: true),
                OrderNumber  = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                BranchId     = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy    = table.Column<string>(type: "text", nullable: true),
                UpdatedBy    = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeedbackSubmissions", x => x.Id);
                table.ForeignKey("FK_FeedbackSubmissions_Orders_OrderId",
                    x => x.OrderId, "Orders", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_FeedbackSubmissions_Branches_BranchId",
                    x => x.BranchId, "Branches", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_FeedbackSubmissions_CreatedAt", "FeedbackSubmissions", "CreatedAt");
        migrationBuilder.CreateIndex("IX_FeedbackSubmissions_Rating",    "FeedbackSubmissions", "Rating");
        migrationBuilder.CreateIndex("IX_FeedbackSubmissions_BranchId",  "FeedbackSubmissions", "BranchId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "FeedbackSubmissions");
}
