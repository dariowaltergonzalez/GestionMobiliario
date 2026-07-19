using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AddContratosYPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contratos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    PropiedadId = table.Column<int>(type: "int", nullable: false),
                    ReservaId = table.Column<int>(type: "int", nullable: true),
                    AgenteId = table.Column<int>(type: "int", nullable: true),
                    PropietarioRefId = table.Column<int>(type: "int", nullable: true),
                    InquilinoRefId = table.Column<int>(type: "int", nullable: true),
                    LocadorNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocadorApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocadorDni = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LocadorEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LocadorTelefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LocatarioNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocatarioApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocatarioDni = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LocatarioEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LocatarioTelefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GaranteNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GaranteApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GaranteDni = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GaranteTelefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MontoBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Moneda = table.Column<int>(type: "int", nullable: false),
                    TipoAjuste = table.Column<int>(type: "int", nullable: false),
                    PeriodicidadAjusteMeses = table.Column<int>(type: "int", nullable: true),
                    DiaVencimientoPago = table.Column<int>(type: "int", nullable: true),
                    ComisionLocadorPorcentaje = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ComisionLocadorMonto = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ComisionLocatarioPorcentaje = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ComisionLocatarioMonto = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AdministracionCobros = table.Column<bool>(type: "bit", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaEscrituracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ArchivoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contratos_Agentes_AgenteId",
                        column: x => x.AgenteId,
                        principalTable: "Agentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Contratos_Propiedades_PropiedadId",
                        column: x => x.PropiedadId,
                        principalTable: "Propiedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contratos_Reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    NumeroCuota = table.Column<int>(type: "int", nullable: false),
                    Periodo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoEsperado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoPagado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_AgenteId",
                table: "Contratos",
                column: "AgenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_Codigo",
                table: "Contratos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_InquilinoRefId",
                table: "Contratos",
                column: "InquilinoRefId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_PropiedadId",
                table: "Contratos",
                column: "PropiedadId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_PropietarioRefId",
                table: "Contratos",
                column: "PropietarioRefId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_ReservaId",
                table: "Contratos",
                column: "ReservaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ContratoId_NumeroCuota",
                table: "Pagos",
                columns: new[] { "ContratoId", "NumeroCuota" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Contratos");
        }
    }
}
