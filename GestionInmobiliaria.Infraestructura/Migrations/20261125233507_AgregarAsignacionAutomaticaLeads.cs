using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAsignacionAutomaticaLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default = 1 (EstrategiaAsignacionLeads.Manual) — 0 no es un valor válido del enum.
            migrationBuilder.AddColumn<int>(
                name: "EstrategiaAsignacionLeads",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "UltimoAgenteAsignadoLeadId",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstrategiaAsignacionLeads",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "UltimoAgenteAsignadoLeadId",
                table: "ConfiguracionEmpresa");
        }
    }
}
