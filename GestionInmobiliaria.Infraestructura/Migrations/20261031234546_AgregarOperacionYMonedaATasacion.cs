using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarOperacionYMonedaATasacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default = 1 (Moneda.ARS) / 2 (TipoOperacion.Venta) para que las filas existentes (y
            // cualquier fila nueva creada fuera de la UI) tengan un valor de enum válido — 0 no existe
            // en ninguno de los dos enums. El agente lo corrige a mano en la UI si no corresponde.
            migrationBuilder.AddColumn<int>(
                name: "Moneda",
                table: "SolicitudesTasacion",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Operacion",
                table: "SolicitudesTasacion",
                type: "int",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Moneda",
                table: "SolicitudesTasacion");

            migrationBuilder.DropColumn(
                name: "Operacion",
                table: "SolicitudesTasacion");
        }
    }
}
