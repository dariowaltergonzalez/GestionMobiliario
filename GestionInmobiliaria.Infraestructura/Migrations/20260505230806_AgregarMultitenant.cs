using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMultitenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Propietarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Propiedades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Inquilinos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "EventosAgenda",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ConfiguracionEmpresa",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Agentes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "Activo", "FechaCreacion", "Nombre", "Slug" },
                values: new object[] { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Demo", "demo" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            // Asignar todos los datos existentes al Tenant #1 (Demo)
            migrationBuilder.Sql("UPDATE Propietarios SET TenantId = 1");
            migrationBuilder.Sql("UPDATE Propiedades SET TenantId = 1");
            migrationBuilder.Sql("UPDATE Inquilinos SET TenantId = 1");
            migrationBuilder.Sql("UPDATE Agentes SET TenantId = 1");
            migrationBuilder.Sql("UPDATE Leads SET TenantId = 1");
            migrationBuilder.Sql("UPDATE EventosAgenda SET TenantId = 1");
            migrationBuilder.Sql("UPDATE ConfiguracionEmpresa SET TenantId = 1");
            migrationBuilder.Sql("UPDATE AspNetUsers SET TenantId = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Propietarios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Propiedades");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Inquilinos");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EventosAgenda");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ConfiguracionEmpresa");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Agentes");
        }
    }
}
