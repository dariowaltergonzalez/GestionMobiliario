using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class PreparacionAjusteContratos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAnulacion",
                table: "Contratos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRescision",
                table: "Contratos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimoAjuste",
                table: "Contratos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoActual",
                table: "Contratos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "Contratos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRescision",
                table: "Contratos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeAjuste",
                table: "Contratos",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AjustesContrato",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    FechaAplicacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoPrevio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoNuevo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Porcentaje = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    TipoAjuste = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AjustesContrato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AjustesContrato_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AjustesContrato_ContratoId",
                table: "AjustesContrato",
                column: "ContratoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AjustesContrato");

            migrationBuilder.DropColumn(
                name: "FechaAnulacion",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "FechaRescision",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "FechaUltimoAjuste",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "MontoActual",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "MotivoRescision",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "PorcentajeAjuste",
                table: "Contratos");
        }
    }
}
