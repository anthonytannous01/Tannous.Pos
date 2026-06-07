using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Delivery Call Center: DeliveryInfo table — separate aggregate linked 1:1 to Order.
/// Keeps delivery concerns out of the core Order entity.
/// Fully additive — no existing data touched.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260605160000_AddDeliveryInfo")]
public partial class AddDeliveryInfo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DeliveryInfos",
            columns: table => new
            {
                Id               = table.Column<Guid>(type: "uuid", nullable: false),
                OrderId          = table.Column<Guid>(type: "uuid", nullable: false),
                DeliveryAddress  = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ApartmentDetails = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CustomerPhone    = table.Column<string>(type: "character varying(50)",  maxLength: 50,  nullable: true),
                Channel          = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                Status           = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                DeliveryFee      = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                EstimatedMinutes = table.Column<int>(type: "integer", nullable: true),
                Notes            = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                DriverName       = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                DriverPhone      = table.Column<string>(type: "character varying(50)",  maxLength: 50,  nullable: true),
                AssignedAt       = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                PickedUpAt       = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeliveredAt      = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                BranchId         = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt        = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt        = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy        = table.Column<string>(type: "text", nullable: true),
                UpdatedBy        = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeliveryInfos", x => x.Id);
                table.ForeignKey("FK_DeliveryInfos_Orders_OrderId",
                    x => x.OrderId, "Orders", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_DeliveryInfos_Branches_BranchId",
                    x => x.BranchId, "Branches", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_DeliveryInfos_OrderId",  "DeliveryInfos", "OrderId",  unique: true);
        migrationBuilder.CreateIndex("IX_DeliveryInfos_Status",   "DeliveryInfos", "Status");
        migrationBuilder.CreateIndex("IX_DeliveryInfos_BranchId", "DeliveryInfos", "BranchId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "DeliveryInfos");
}
