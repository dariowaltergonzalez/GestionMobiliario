using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoPropiedad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Propiedades",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Poblar código en propiedades existentes: PRO-{año}-{id con 4 dígitos}
            migrationBuilder.Sql(@"
                UPDATE Propiedades
                SET Codigo = 'PRO-' + CAST(YEAR(FechaCreacion) AS NVARCHAR(4)) + '-' + RIGHT('0000' + CAST(Id AS NVARCHAR(10)), 4)
                WHERE Codigo = ''
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Propiedades_Codigo",
                table: "Propiedades",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Propiedades_Codigo",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Propiedades");
        }
    }
}
