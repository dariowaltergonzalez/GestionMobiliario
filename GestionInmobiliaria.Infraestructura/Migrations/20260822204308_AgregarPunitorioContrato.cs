using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPunitorioContrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PunitorioPorcentaje",
                table: "Contratos",
                type: "decimal(7,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PunitorioPorcentaje",
                table: "Contratos");
        }
    }
}
