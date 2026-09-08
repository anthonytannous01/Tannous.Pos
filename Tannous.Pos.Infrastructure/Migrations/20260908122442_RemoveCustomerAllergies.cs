using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerAllergies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Allergies",
                table: "Customers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Allergies",
                table: "Customers",
                type: "text",
                nullable: true);
        }
    }
}
