using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AddPagoDetalles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PagoDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PagoId = table.Column<int>(type: "int", nullable: false),
                    Medio = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChequeBanco = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChequeNumero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ChequeFechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagoDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagoDetalles_Pagos_PagoId",
                        column: x => x.PagoId,
                        principalTable: "Pagos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagoDetalles_PagoId",
                table: "PagoDetalles",
                column: "PagoId");

            // Migrar datos existentes: cada Pago con MedioPago != NULL pasa a un PagoDetalle
            migrationBuilder.Sql(@"
                INSERT INTO PagoDetalles (PagoId, Medio, Monto, Referencia, Activo, FechaCreacion, FechaActualizacion, TenantId)
                SELECT
                    p.Id,
                    p.MedioPago,
                    ISNULL(p.MontoPagado, p.MontoEsperado),
                    p.MedioPagoReferencia,
                    1,
                    GETUTCDATE(),
                    GETUTCDATE(),
                    p.TenantId
                FROM Pagos p
                WHERE p.MedioPago IS NOT NULL AND p.Activo = 1
            ");

            migrationBuilder.DropColumn(
                name: "MedioPago",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MedioPagoReferencia",
                table: "Pagos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MedioPago",
                table: "Pagos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedioPagoReferencia",
                table: "Pagos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.DropTable(
                name: "PagoDetalles");
        }
    }
}
