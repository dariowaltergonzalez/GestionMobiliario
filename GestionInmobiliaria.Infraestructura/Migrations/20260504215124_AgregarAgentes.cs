using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAgentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgenteId",
                table: "Propiedades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgenteId",
                table: "Inquilinos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Agentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Zona = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TelefonoInterno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ComisionPorcentaje = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agentes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Propiedades_AgenteId",
                table: "Propiedades",
                column: "AgenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Inquilinos_AgenteId",
                table: "Inquilinos",
                column: "AgenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Agentes_UserId",
                table: "Agentes",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Inquilinos_Agentes_AgenteId",
                table: "Inquilinos",
                column: "AgenteId",
                principalTable: "Agentes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Propiedades_Agentes_AgenteId",
                table: "Propiedades",
                column: "AgenteId",
                principalTable: "Agentes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inquilinos_Agentes_AgenteId",
                table: "Inquilinos");

            migrationBuilder.DropForeignKey(
                name: "FK_Propiedades_Agentes_AgenteId",
                table: "Propiedades");

            migrationBuilder.DropTable(
                name: "Agentes");

            migrationBuilder.DropIndex(
                name: "IX_Propiedades_AgenteId",
                table: "Propiedades");

            migrationBuilder.DropIndex(
                name: "IX_Inquilinos_AgenteId",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "AgenteId",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "AgenteId",
                table: "Inquilinos");
        }
    }
}
