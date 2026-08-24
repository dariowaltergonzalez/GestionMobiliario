using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTokenPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenPortal",
                table: "Propietarios",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenPortal",
                table: "Inquilinos",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Propietarios_TokenPortal",
                table: "Propietarios",
                column: "TokenPortal",
                unique: true,
                filter: "[TokenPortal] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Inquilinos_TokenPortal",
                table: "Inquilinos",
                column: "TokenPortal",
                unique: true,
                filter: "[TokenPortal] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Propietarios_TokenPortal",
                table: "Propietarios");

            migrationBuilder.DropIndex(
                name: "IX_Inquilinos_TokenPortal",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "TokenPortal",
                table: "Propietarios");

            migrationBuilder.DropColumn(
                name: "TokenPortal",
                table: "Inquilinos");
        }
    }
}
