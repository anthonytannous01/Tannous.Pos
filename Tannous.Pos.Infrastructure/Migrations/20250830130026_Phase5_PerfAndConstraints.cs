using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_PerfAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add unique index on Orders.ReceiptNumber
            migrationBuilder.CreateIndex(
                name: "IX_Orders_ReceiptNumber",
                table: "Orders",
                column: "ReceiptNumber",
                unique: true,
                filter: "\"ReceiptNumber\" IS NOT NULL");

            // Add unique index on Devices.DeviceId
            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId",
                table: "Devices",
                column: "DeviceId",
                unique: true);

            // Add index on InventoryMovements (IngredientId, CreatedAt)
            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_IngredientId_CreatedAt",
                table: "InventoryMovements",
                columns: new[] { "IngredientId", "CreatedAt" });

            // Add index on Orders (Status, ClosedAt) for reports
            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_ClosedAt",
                table: "Orders",
                columns: new[] { "Status", "ClosedAt" });

            // Add index on Customers (Phone) for quick lookup - already exists in InitialCreate
            // migrationBuilder.CreateIndex(
            //     name: "IX_Customers_Phone",
            //     table: "Customers",
            //     column: "Phone");

            // Add foreign key constraints with proper cascades - FK_Orders_Customers_CustomerId already exists in InitialCreate
            // migrationBuilder.AddForeignKey(
            //     name: "FK_Orders_Customers_CustomerId",
            //     table: "Orders",
            //     column: "CustomerId",
            //     principalTable: "Customers",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Restrict);

            // migrationBuilder.AddForeignKey(
            //     name: "FK_Orders_Shifts_ShiftId",
            //     table: "Orders",
            //     column: "ShiftId",
            //     principalTable: "Shifts",
            //     principalColumn: "Id",
            //     onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove foreign key constraints
            // migrationBuilder.DropForeignKey(
            //     name: "FK_Orders_Shifts_ShiftId",
            //     table: "Orders");

            // migrationBuilder.DropForeignKey(
            //     name: "FK_Orders_Customers_CustomerId",
            //     table: "Orders");

            // Remove indexes
            // migrationBuilder.DropIndex(
            //     name: "IX_Customers_Phone",
            //     table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_ClosedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_IngredientId_CreatedAt",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_Devices_DeviceId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ReceiptNumber",
                table: "Orders");
        }
    }
}
