using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDetalleCobroGasto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChequeBanco",
                table: "Gastos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChequeFechaVencimiento",
                table: "Gastos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChequeNumero",
                table: "Gastos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCobro",
                table: "Gastos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenciaCobro",
                table: "Gastos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChequeBanco",
                table: "Gastos");

            migrationBuilder.DropColumn(
                name: "ChequeFechaVencimiento",
                table: "Gastos");

            migrationBuilder.DropColumn(
                name: "ChequeNumero",
                table: "Gastos");

            migrationBuilder.DropColumn(
                name: "FechaCobro",
                table: "Gastos");

            migrationBuilder.DropColumn(
                name: "ReferenciaCobro",
                table: "Gastos");
        }
    }
}
