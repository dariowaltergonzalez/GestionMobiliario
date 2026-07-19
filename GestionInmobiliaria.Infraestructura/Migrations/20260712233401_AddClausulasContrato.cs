using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionInmobiliaria.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class AddClausulasContrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClausulasContrato",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClausulasContrato", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ClausulasContrato",
                columns: new[] { "Id", "Activo", "FechaActualizacion", "FechaCreacion", "Numero", "Orden", "TenantId", "Texto", "Titulo" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PRIMERA", 1, 1, "Entre {locador}, en adelante \"EL/LA LOCADOR/A\", con domicilio en {locadorDomicilio}, y {locatario}, en adelante \"EL/LA LOCATARIO/A\", con domicilio en {propiedadDireccion}, convienen celebrar el presente contrato de locación, que se regirá por el Código Civil y Comercial de la Nación (CCyCN) y la Ley N° 27551 y sus modificatorias.", "PARTES" },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SEGUNDA", 2, 1, "EL/LA LOCADOR/A cede en locación a EL/LA LOCATARIO/A, que acepta, el inmueble sito en {propiedadDireccion}. El inmueble tendrá por destino la vivienda familiar de EL/LA LOCATARIO/A, no pudiendo modificarlo salvo consentimiento expreso de EL/LA LOCADOR/A (art. 1196, CCyCN).", "OBJETO" },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TERCERA", 3, 1, "Las partes convienen que la presente locación se extenderá por {duracionMeses} MESES, desde el día {fechaInicio} hasta el día {fechaFin}, inclusive (art. 1198, CCyCN).", "PLAZO" },
                    { 4, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CUARTA", 4, 1, "Por la locación, las partes convienen un canon locativo de {montoAlquiler} por mes para el período inicial del contrato.", "PRECIO" },
                    { 5, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "QUINTA", 5, 1, "El canon mensual definido en la cláusula anterior se actualizará {ajusteTexto}, {periodicidad}. EL/LA LOCADOR/A informará el nuevo valor al LOCATARIO/A por vía electrónica, al menos diez (10) días antes que venza el pago del mes (art. 14, Ley N° 27737).", "AJUSTE" },
                    { 6, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SEXTA", 6, 1, "EL/LA LOCATARIO/A se obliga a abonar el alquiler convenido por mes entero y adelantado{diaVencimiento}. {pagoMedio}", "PERÍODO Y LUGAR DE PAGO" },
                    { 7, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SÉPTIMA", 7, 1, "La mora en el pago del alquiler se producirá de forma automática. Por ésta se abonará la tasa activa por plazo fijo del Banco de la Nación Argentina, durante el tiempo que demore en efectivizar el pago de los alquileres adeudados.", "DEMORA" },
                    { 8, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OCTAVA", 8, 1, "EL/LA LOCATARIO/A tiene a su cargo el pago en tiempo y forma de: (i) servicios de energía eléctrica, agua y gas; (ii) cargas y contribuciones asociadas al destino de vivienda del inmueble; (iii) las expensas que deriven de gastos habituales ordinarios. EL/LA LOCADOR/A tiene a su cargo las cargas y contribuciones que graven el inmueble (impuesto inmobiliario) y las expensas comunes extraordinarias (art. 1209, CCyCN).", "EXPENSAS, SERVICIOS E IMPUESTOS" },
                    { 9, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "NOVENA", 9, 1, "EL/LA LOCATARIO/A, dentro de los TREINTA (30) días de suscripto el presente, transferirá a su nombre los servicios públicos, TV por cable e internet. EL/LA LOCADOR/A, dentro de los TREINTA (30) días de terminado el contrato, asegurará el cambio de titularidad del total de servicios.", "TITULARIDAD DE SERVICIOS" },
                    { 10, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DÉCIMA", 10, 1, "EL/LA LOCATARIO/A se compromete a respetar los reglamentos de Copropiedad y Administración y el Interno del edificio, siendo responsable ante el consorcio de propietarios de las transgresiones estipuladas en los mismos.", "REGLAMENTOS Y CONSORCIO" },
                    { 11, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DÉCIMA PRIMERA", 11, 1, "EL/LA LOCATARIO/A no podrá hacer modificaciones de ninguna naturaleza en la propiedad, sin consentimiento previo del/la LOCADOR/A expresado por vía electrónica. En caso de que las modificaciones impliquen mejoras del inmueble, EL/LA LOCADOR/A deberá reembolsar al LOCATARIO/A lo invertido.", "MEJORAS Y MODIFICACIONES" },
                    { 12, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DÉCIMA SEGUNDA", 12, 1, "El presente contrato de locación es intransferible. Queda prohibido al/la LOCATARIO/A ceder o subarrendar total o parcialmente el inmueble sin consentimiento del/la LOCADOR/A. Asimismo, queda prohibido usarlo contrariando las leyes o darle otro destino que el de vivienda familiar.", "PROHIBICIÓN" },
                    { 13, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DÉCIMA TERCERA", 13, 1, "EL/LA LOCATARIO/A tiene la obligación de mantener el inmueble y restituirlo en el estado que lo recibió, excepto por deterioros ocasionados por el mero transcurso del tiempo y por el uso regular (art. 1210 CCyCN). EL/LA LOCADOR/A debe entregarlo en las condiciones previstas, conservarlo para que sirva al uso convenido y efectuar las reparaciones que exija el deterioro originado por causa no imputable al LOCATARIO/A (art. 1201, CCyCN).", "RESPONSABILIDADES" },
                    { 14, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DÉCIMA CUARTA", 14, 1, "En caso de negativa o silencio del/la LOCADOR/A ante un reclamo debidamente notificado para efectuar una reparación urgente, EL/LA LOCATARIO/A puede realizarla por sí, con cargo al/la LOCADOR/A, una vez transcurridas al menos veinticuatro (24) horas corridas. Si las reparaciones no fueran urgentes, EL/LA LOCATARIO/A debe intimar al/la LOCADOR/A con un plazo mínimo de diez (10) días (art. 1201, CCyCN).", "REPARACIONES" },
                    { 15, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DÉCIMA QUINTA", 15, 1, "EL/LA LOCATARIO/A abona en este acto la cantidad de {montoAlquiler} en concepto del alquiler correspondiente al mes de {mesInicio}. Por este primer canon, EL/LA LOCADOR/A remitirá la correspondiente factura electrónica conforme la cláusula sexta del presente.", "PRIMER MES" },
                    { 16, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DÉCIMA SEXTA", 16, 1, "En garantía de las obligaciones contraídas, EL/LA LOCATARIO/A da en depósito al/la LOCADOR/A la suma de {montoAlquiler}, equivalente al valor del primer mes de alquiler del contrato. Al momento de restitución del inmueble, EL/LA LOCADOR/A deberá devolver el depósito actualizado al valor del último mes del contrato (art. 1196, CCyCN).", "DEPÓSITO EN GARANTÍA" },
                    { 17, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DÉCIMA SÉPTIMA", 17, 1, "La finalización del presente contrato, por cualquier modalidad de extinción, se formalizará a través del Acta de Entrega de Llaves, que EL/LA LOCADOR/A confeccionará y cuyo texto enviará al/la LOCATARIO/A 48 horas antes de la entrega. El acta informará la fecha y hora de entrega, el estado del inmueble, el estado de las obligaciones contractuales y la devolución total o parcial del depósito en garantía.", "FINALIZACIÓN" },
                    { 18, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DÉCIMA OCTAVA", 18, 1, "{garanteTexto}", "FIANZA" },
                    { 19, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DÉCIMA NOVENA", 19, 1, "EL/LA LOCATARIO/A puede rescindir el presente contrato sin expresión de causa de forma anticipada una vez transcurridos los primeros seis (6) meses, notificando su decisión con un (1) mes de anticipación. Si la rescisión es en el primer año, corresponde una indemnización de un mes y medio de alquiler; después del primer año, de un mes. Si la notificación se efectúa con tres (3) meses o más de anticipación, no corresponde indemnización (art. 1221 CCyCN).", "RESOLUCIÓN ANTICIPADA" },
                    { 20, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VIGÉSIMA", 20, 1, "Dentro de los últimos tres (3) meses del contrato, cualquiera de las partes puede convocar a la otra a conversar sobre la renovación de la locación mediante notificación fehaciente. El silencio o negativa del/la LOCADOR/A a renovar habilitará al/la LOCATARIO/A a rescindir sin preaviso ni indemnización (art. 1221 bis CCyCN).", "RENOVACIÓN" },
                    { 21, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VIGÉSIMA PRIMERA", 21, 1, "La falta de pago de dos (2) meses de alquiler consecutivos da derecho al/la LOCADOR/A a considerar irrevocablemente rescindido el contrato y tramitar la acción de desalojo. Previo a ello, EL/LA LOCADOR/A deberá intimar fehacientemente al/la LOCATARIO/A, otorgando un plazo no inferior a diez (10) días (art. 1222 CCyCN).", "FALTA DE PAGO" },
                    { 22, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VIGÉSIMA SEGUNDA", 22, 1, "Las partes establecen los siguientes domicilios: a) LOCADOR/A: {locadorDomicilio}; {locadorEmail}. b) LOCATARIO/A: en el inmueble locado ({propiedadDireccion}); {locatarioEmail}. Ambas convienen que las comunicaciones entre sí se efectuarán por vía electrónica, las que se tendrán por válidas y plenamente eficaces (art. 75, CCyCN).", "DOMICILIOS" },
                    { 23, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VIGÉSIMA TERCERA", 23, 1, "Las partes se comprometen a manejarse en todo momento de buena fe y a sostener diálogo permanente, pacífico y tolerante. Ante desavenencias, se comprometen a recurrir a mediación comunitaria gratuita en la Defensoría del Pueblo.", "DIÁLOGO" },
                    { 24, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VIGÉSIMA CUARTA", 24, 1, "Las partes se someten a la jurisdicción de los Tribunales Ordinarios de la ciudad de {ciudad}, con renuncia expresa a cualquier otro fuero o jurisdicción.", "JURISDICCIÓN" },
                    { 25, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VIGÉSIMA QUINTA", 25, 1, "En cumplimiento de la normativa vigente, EL/LA LOCADOR/A registrará el presente contrato ante la AFIP dentro de los próximos treinta (30) días de suscripto.", "REGISTRACIÓN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClausulasContrato");
        }
    }
}
