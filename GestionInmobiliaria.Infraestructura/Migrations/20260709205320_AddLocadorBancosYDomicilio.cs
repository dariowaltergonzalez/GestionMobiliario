using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AddLocadorBancosYDomicilio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Banco",
                table: "Propietarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocadorBanco",
                table: "Contratos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocadorCbu",
                table: "Contratos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocadorCuit",
                table: "Contratos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocadorDomicilio",
                table: "Contratos",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Banco",
                table: "Propietarios");

            migrationBuilder.DropColumn(
                name: "LocadorBanco",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "LocadorCbu",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "LocadorCuit",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "LocadorDomicilio",
                table: "Contratos");
        }
    }
}
