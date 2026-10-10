using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNotificacionesAgente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notificaciones",
                table: "Agentes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificacionesWhatsApp",
                table: "Agentes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoWhatsApp",
                table: "Agentes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notificaciones",
                table: "Agentes");

            migrationBuilder.DropColumn(
                name: "NotificacionesWhatsApp",
                table: "Agentes");

            migrationBuilder.DropColumn(
                name: "TelefonoWhatsApp",
                table: "Agentes");
        }
    }
}
