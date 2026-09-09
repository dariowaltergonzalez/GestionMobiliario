using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTelefonoWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelefonoWhatsApp",
                table: "Propietarios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoWhatsApp",
                table: "Inquilinos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelefonoWhatsApp",
                table: "Propietarios");

            migrationBuilder.DropColumn(
                name: "TelefonoWhatsApp",
                table: "Inquilinos");
        }
    }
}
