using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AddReservas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropiedadId = table.Column<int>(type: "int", nullable: false),
                    LeadId = table.Column<int>(type: "int", nullable: true),
                    AgenteId = table.Column<int>(type: "int", nullable: true),
                    CompradorNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompradorApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompradorDni = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompradorTelefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompradorEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VendedorNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendedorApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendedorDni = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VendedorTelefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VendedorEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MontoSenia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Moneda = table.Column<int>(type: "int", nullable: false),
                    MedioDeposito = table.Column<int>(type: "int", nullable: false),
                    FechaReserva = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_Agentes_AgenteId",
                        column: x => x.AgenteId,
                        principalTable: "Agentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reservas_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reservas_Propiedades_PropiedadId",
                        column: x => x.PropiedadId,
                        principalTable: "Propiedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_AgenteId",
                table: "Reservas",
                column: "AgenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_LeadId",
                table: "Reservas",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_PropiedadId",
                table: "Reservas",
                column: "PropiedadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservas");
        }
    }
}
