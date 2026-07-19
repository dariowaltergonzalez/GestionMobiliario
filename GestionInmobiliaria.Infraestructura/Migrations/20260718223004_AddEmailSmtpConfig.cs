using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSmtpConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailHabilitado",
                table: "ConfiguracionEmpresa",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailNombreRemitente",
                table: "ConfiguracionEmpresa",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSmtpHost",
                table: "ConfiguracionEmpresa",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSmtpPassword",
                table: "ConfiguracionEmpresa",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailSmtpPuerto",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EmailSmtpUsuario",
                table: "ConfiguracionEmpresa",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailHabilitado",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "EmailNombreRemitente",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "EmailSmtpHost",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "EmailSmtpPassword",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "EmailSmtpPuerto",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "EmailSmtpUsuario",
                table: "ConfiguracionEmpresa");
        }
    }
}
