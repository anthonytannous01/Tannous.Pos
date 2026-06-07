using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tannous.Pos.Infrastructure.Data;

#nullable disable

namespace Tannous.Pos.Infrastructure.Migrations;

/// <summary>
/// Arabic localisation: adds optional NameAr/DescriptionAr to MenuItems,
/// NameAr to Categories, and BusinessNameAr to BusinessSettings.
/// All nullable — existing data is unaffected.
/// </summary>
[DbContext(typeof(PosDbContext))]
[Migration("20260605150000_AddArabicNameFields")]
public partial class AddArabicNameFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NameAr", table: "MenuItems",
            type: "character varying(200)", maxLength: 200, nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DescriptionAr", table: "MenuItems",
            type: "character varying(1000)", maxLength: 1000, nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NameAr", table: "Categories",
            type: "character varying(100)", maxLength: 100, nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BusinessNameAr", table: "BusinessSettings",
            type: "character varying(200)", maxLength: 200, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "NameAr",         table: "MenuItems");
        migrationBuilder.DropColumn(name: "DescriptionAr",  table: "MenuItems");
        migrationBuilder.DropColumn(name: "NameAr",         table: "Categories");
        migrationBuilder.DropColumn(name: "BusinessNameAr", table: "BusinessSettings");
    }
}
