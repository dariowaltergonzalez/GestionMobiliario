using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class FixOperacionDefaultData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Operacion = 0 es inválido (enum arranca en 1=Alquiler). Corrije datos existentes.
            migrationBuilder.Sql("UPDATE Propiedades SET Operacion = 1 WHERE Operacion = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
