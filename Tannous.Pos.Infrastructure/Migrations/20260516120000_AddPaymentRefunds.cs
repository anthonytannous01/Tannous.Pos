using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>Internal payment refund consistency records (paid void; no external processor).</summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260516120000_AddPaymentRefunds")]
public partial class AddPaymentRefunds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PaymentRefunds",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                CorrelationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                OriginalPaymentId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentRefunds", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentRefunds_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PaymentRefunds_Payments_OriginalPaymentId",
                    column: x => x.OriginalPaymentId,
                    principalTable: "Payments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentRefunds_OrderId",
            table: "PaymentRefunds",
            column: "OrderId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PaymentRefunds_CorrelationId",
            table: "PaymentRefunds",
            column: "CorrelationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PaymentRefunds");
    }
}
