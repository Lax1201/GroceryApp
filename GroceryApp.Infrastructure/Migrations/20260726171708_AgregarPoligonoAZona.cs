using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroceryApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPoligonoAZona : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PoligonoWkt",
                table: "Zonas",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoligonoWkt",
                table: "Zonas");
        }
    }
}
