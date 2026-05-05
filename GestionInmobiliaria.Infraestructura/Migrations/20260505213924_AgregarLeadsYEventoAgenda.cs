using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarLeadsYEventoAgenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Origen = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AgenteId = table.Column<int>(type: "int", nullable: true),
                    PropiedadId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leads_Agentes_AgenteId",
                        column: x => x.AgenteId,
                        principalTable: "Agentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Leads_Propiedades_PropiedadId",
                        column: x => x.PropiedadId,
                        principalTable: "Propiedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EventosAgenda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AgenteId = table.Column<int>(type: "int", nullable: false),
                    PropiedadId = table.Column<int>(type: "int", nullable: true),
                    LeadId = table.Column<int>(type: "int", nullable: true),
                    InquilinoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosAgenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosAgenda_Agentes_AgenteId",
                        column: x => x.AgenteId,
                        principalTable: "Agentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosAgenda_Inquilinos_InquilinoId",
                        column: x => x.InquilinoId,
                        principalTable: "Inquilinos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventosAgenda_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventosAgenda_Propiedades_PropiedadId",
                        column: x => x.PropiedadId,
                        principalTable: "Propiedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosAgenda_AgenteId",
                table: "EventosAgenda",
                column: "AgenteId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAgenda_InquilinoId",
                table: "EventosAgenda",
                column: "InquilinoId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAgenda_LeadId",
                table: "EventosAgenda",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAgenda_PropiedadId",
                table: "EventosAgenda",
                column: "PropiedadId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_AgenteId",
                table: "Leads",
                column: "AgenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_PropiedadId",
                table: "Leads",
                column: "PropiedadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosAgenda");

            migrationBuilder.DropTable(
                name: "Leads");
        }
    }
}
