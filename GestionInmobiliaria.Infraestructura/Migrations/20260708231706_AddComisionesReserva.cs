using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AddComisionesReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ComisionCompradorMonto",
                table: "Reservas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionCompradorPorcentaje",
                table: "Reservas",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionVendedorMonto",
                table: "Reservas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionVendedorPorcentaje",
                table: "Reservas",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComisionCompradorMonto",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "ComisionCompradorPorcentaje",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "ComisionVendedorMonto",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "ComisionVendedorPorcentaje",
                table: "Reservas");
        }
    }
}
