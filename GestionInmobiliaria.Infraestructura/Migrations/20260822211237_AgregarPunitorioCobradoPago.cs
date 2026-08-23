using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPunitorioCobradoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetallePunitorioCobrado",
                table: "Pagos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiasAtrasoPunitorioCobrado",
                table: "Pagos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimientoPunitorioCobrado",
                table: "Pagos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoPunitorioCobrado",
                table: "Pagos",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetallePunitorioCobrado",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "DiasAtrasoPunitorioCobrado",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "FechaVencimientoPunitorioCobrado",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MontoPunitorioCobrado",
                table: "Pagos");
        }
    }
}
