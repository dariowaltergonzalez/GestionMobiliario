using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposExtendidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CBU",
                table: "Propietarios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "Propietarios",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono2",
                table: "Propietarios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AceptaMascotas",
                table: "Propiedades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Antiguedad",
                table: "Propiedades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Cochera",
                table: "Propiedades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EstadoConservacion",
                table: "Propiedades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Expensas",
                table: "Propiedades",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "Propiedades",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NroCatastro",
                table: "Propiedades",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TieneCalefaccion",
                table: "Propiedades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DniGarante",
                table: "Inquilinos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreGarante",
                table: "Inquilinos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notas",
                table: "Inquilinos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ocupacion",
                table: "Inquilinos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono2",
                table: "Inquilinos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoGarante",
                table: "Inquilinos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CBU",
                table: "Propietarios");

            migrationBuilder.DropColumn(
                name: "Notas",
                table: "Propietarios");

            migrationBuilder.DropColumn(
                name: "Telefono2",
                table: "Propietarios");

            migrationBuilder.DropColumn(
                name: "AceptaMascotas",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "Antiguedad",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "Cochera",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "EstadoConservacion",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "Expensas",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "Notas",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "NroCatastro",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "TieneCalefaccion",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "DniGarante",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "NombreGarante",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "Notas",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "Ocupacion",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "Telefono2",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "TelefonoGarante",
                table: "Inquilinos");
        }
    }
}
