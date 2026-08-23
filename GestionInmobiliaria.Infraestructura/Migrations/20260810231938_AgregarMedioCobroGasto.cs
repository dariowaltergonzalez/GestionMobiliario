using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMedioCobroGasto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MedioCobro",
                table: "Gastos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacionesResolucion",
                table: "Gastos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MedioCobro",
                table: "Gastos");

            migrationBuilder.DropColumn(
                name: "ObservacionesResolucion",
                table: "Gastos");
        }
    }
}
