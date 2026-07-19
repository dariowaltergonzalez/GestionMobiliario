using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class ClausulaContratoRepository : IClausulaContratoRepository
{
    private readonly ApplicationDbContext _context;

    public ClausulaContratoRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<IEnumerable<ClausulaContrato>> GetAllAsync() =>
        await _context.ClausulasContrato.OrderBy(c => c.Orden).ToListAsync();

    public async Task<IEnumerable<ClausulaContrato>> GetActivasAsync() =>
        await _context.ClausulasContrato.Where(c => c.Activo).OrderBy(c => c.Orden).ToListAsync();

    public async Task<ClausulaContrato?> GetByIdAsync(int id) =>
        await _context.ClausulasContrato.FindAsync(id);

    public async Task<ClausulaContrato> CreateAsync(ClausulaContrato clausula)
    {
        var maxOrden = await _context.ClausulasContrato.MaxAsync(c => (int?)c.Orden) ?? 0;
        clausula.Orden = maxOrden + 1;
        clausula.FechaCreacion = DateTime.UtcNow;
        clausula.FechaActualizacion = DateTime.UtcNow;
        _context.ClausulasContrato.Add(clausula);
        await _context.SaveChangesAsync();
        return clausula;
    }

    public async Task<ClausulaContrato> UpdateAsync(ClausulaContrato clausula)
    {
        clausula.FechaActualizacion = DateTime.UtcNow;
        _context.ClausulasContrato.Update(clausula);
        await _context.SaveChangesAsync();
        return clausula;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var clausula = await _context.ClausulasContrato.FindAsync(id);
        if (clausula is null) return false;
        clausula.Activo = false;
        clausula.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task InicializarDefaultsAsync()
    {
        var yaExisten = await _context.ClausulasContrato.AnyAsync();
        if (yaExisten) return;

        var now = DateTime.UtcNow;
        var defaults = new[]
        {
            (1,  "PRIMERA",          "PARTES",                       "Entre {locador}, en adelante \"EL/LA LOCADOR/A\", con domicilio en {locadorDomicilio}, y {locatario}, en adelante \"EL/LA LOCATARIO/A\", con domicilio en {propiedadDireccion}, convienen celebrar el presente contrato de locación, que se regirá por el Código Civil y Comercial de la Nación (CCyCN) y la Ley N° 27551 y sus modificatorias."),
            (2,  "SEGUNDA",          "OBJETO",                       "EL/LA LOCADOR/A cede en locación a EL/LA LOCATARIO/A, que acepta, el inmueble sito en {propiedadDireccion}. El inmueble tendrá por destino la vivienda familiar de EL/LA LOCATARIO/A, no pudiendo modificarlo salvo consentimiento expreso de EL/LA LOCADOR/A (art. 1196, CCyCN)."),
            (3,  "TERCERA",          "PLAZO",                        "Las partes convienen que la presente locación se extenderá por {duracionMeses} MESES, desde el día {fechaInicio} hasta el día {fechaFin}, inclusive (art. 1198, CCyCN)."),
            (4,  "CUARTA",           "PRECIO",                       "Por la locación, las partes convienen un canon locativo de {montoAlquiler} por mes para el período inicial del contrato."),
            (5,  "QUINTA",           "AJUSTE",                       "El canon mensual definido en la cláusula anterior se actualizará {ajusteTexto}, {periodicidad}. EL/LA LOCADOR/A informará el nuevo valor al LOCATARIO/A por vía electrónica, al menos diez (10) días antes que venza el pago del mes (art. 14, Ley N° 27737)."),
            (6,  "SEXTA",            "PERÍODO Y LUGAR DE PAGO",      "EL/LA LOCATARIO/A se obliga a abonar el alquiler convenido por mes entero y adelantado{diaVencimiento}. {pagoMedio}"),
            (7,  "SÉPTIMA",          "DEMORA",                       "La mora en el pago del alquiler se producirá de forma automática. Por ésta se abonará la tasa activa por plazo fijo del Banco de la Nación Argentina, durante el tiempo que demore en efectivizar el pago de los alquileres adeudados."),
            (8,  "OCTAVA",           "EXPENSAS, SERVICIOS E IMPUESTOS", "EL/LA LOCATARIO/A tiene a su cargo el pago en tiempo y forma de: (i) servicios de energía eléctrica, agua y gas; (ii) cargas y contribuciones asociadas al destino de vivienda del inmueble; (iii) las expensas que deriven de gastos habituales ordinarios. EL/LA LOCADOR/A tiene a su cargo las cargas y contribuciones que graven el inmueble (impuesto inmobiliario) y las expensas comunes extraordinarias (art. 1209, CCyCN)."),
            (9,  "NOVENA",           "TITULARIDAD DE SERVICIOS",     "EL/LA LOCATARIO/A, dentro de los TREINTA (30) días de suscripto el presente, transferirá a su nombre los servicios públicos, TV por cable e internet. EL/LA LOCADOR/A, dentro de los TREINTA (30) días de terminado el contrato, asegurará el cambio de titularidad del total de servicios."),
            (10, "DÉCIMA",           "REGLAMENTOS Y CONSORCIO",      "EL/LA LOCATARIO/A se compromete a respetar los reglamentos de Copropiedad y Administración y el Interno del edificio, siendo responsable ante el consorcio de propietarios de las transgresiones estipuladas en los mismos."),
            (11, "DÉCIMA PRIMERA",   "MEJORAS Y MODIFICACIONES",     "EL/LA LOCATARIO/A no podrá hacer modificaciones de ninguna naturaleza en la propiedad, sin consentimiento previo del/la LOCADOR/A expresado por vía electrónica. En caso de que las modificaciones impliquen mejoras del inmueble, EL/LA LOCADOR/A deberá reembolsar al LOCATARIO/A lo invertido."),
            (12, "DÉCIMA SEGUNDA",   "PROHIBICIÓN",                  "El presente contrato de locación es intransferible. Queda prohibido al/la LOCATARIO/A ceder o subarrendar total o parcialmente el inmueble sin consentimiento del/la LOCADOR/A. Asimismo, queda prohibido usarlo contrariando las leyes o darle otro destino que el de vivienda familiar."),
            (13, "DÉCIMA TERCERA",   "RESPONSABILIDADES",            "EL/LA LOCATARIO/A tiene la obligación de mantener el inmueble y restituirlo en el estado que lo recibió, excepto por deterioros ocasionados por el mero transcurso del tiempo y por el uso regular (art. 1210 CCyCN). EL/LA LOCADOR/A debe entregarlo en las condiciones previstas, conservarlo para que sirva al uso convenido y efectuar las reparaciones que exija el deterioro originado por causa no imputable al LOCATARIO/A (art. 1201, CCyCN)."),
            (14, "DÉCIMA CUARTA",    "REPARACIONES",                 "En caso de negativa o silencio del/la LOCADOR/A ante un reclamo debidamente notificado para efectuar una reparación urgente, EL/LA LOCATARIO/A puede realizarla por sí, con cargo al/la LOCADOR/A, una vez transcurridas al menos veinticuatro (24) horas corridas. Si las reparaciones no fueran urgentes, EL/LA LOCATARIO/A debe intimar al/la LOCADOR/A con un plazo mínimo de diez (10) días (art. 1201, CCyCN)."),
            (15, "DÉCIMA QUINTA",    "PRIMER MES",                   "EL/LA LOCATARIO/A abona en este acto la cantidad de {montoAlquiler} en concepto del alquiler correspondiente al mes de {mesInicio}. Por este primer canon, EL/LA LOCADOR/A remitirá la correspondiente factura electrónica conforme la cláusula sexta del presente."),
            (16, "DÉCIMA SEXTA",     "DEPÓSITO EN GARANTÍA",         "En garantía de las obligaciones contraídas, EL/LA LOCATARIO/A da en depósito al/la LOCADOR/A la suma de {montoAlquiler}, equivalente al valor del primer mes de alquiler del contrato. Al momento de restitución del inmueble, EL/LA LOCADOR/A deberá devolver el depósito actualizado al valor del último mes del contrato (art. 1196, CCyCN)."),
            (17, "DÉCIMA SÉPTIMA",   "FINALIZACIÓN",                 "La finalización del presente contrato, por cualquier modalidad de extinción, se formalizará a través del Acta de Entrega de Llaves, que EL/LA LOCADOR/A confeccionará y cuyo texto enviará al/la LOCATARIO/A 48 horas antes de la entrega. El acta informará la fecha y hora de entrega, el estado del inmueble, el estado de las obligaciones contractuales y la devolución total o parcial del depósito en garantía."),
            (18, "DÉCIMA OCTAVA",    "FIANZA",                       "{garanteTexto}"),
            (19, "DÉCIMA NOVENA",    "RESOLUCIÓN ANTICIPADA",        "EL/LA LOCATARIO/A puede rescindir el presente contrato sin expresión de causa de forma anticipada una vez transcurridos los primeros seis (6) meses, notificando su decisión con un (1) mes de anticipación. Si la rescisión es en el primer año, corresponde una indemnización de un mes y medio de alquiler; después del primer año, de un mes. Si la notificación se efectúa con tres (3) meses o más de anticipación, no corresponde indemnización (art. 1221 CCyCN)."),
            (20, "VIGÉSIMA",         "RENOVACIÓN",                   "Dentro de los últimos tres (3) meses del contrato, cualquiera de las partes puede convocar a la otra a conversar sobre la renovación de la locación mediante notificación fehaciente. El silencio o negativa del/la LOCADOR/A a renovar habilitará al/la LOCATARIO/A a rescindir sin preaviso ni indemnización (art. 1221 bis CCyCN)."),
            (21, "VIGÉSIMA PRIMERA", "FALTA DE PAGO",                "La falta de pago de dos (2) meses de alquiler consecutivos da derecho al/la LOCADOR/A a considerar irrevocablemente rescindido el contrato y tramitar la acción de desalojo. Previo a ello, EL/LA LOCADOR/A deberá intimar fehacientemente al/la LOCATARIO/A, otorgando un plazo no inferior a diez (10) días (art. 1222 CCyCN)."),
            (22, "VIGÉSIMA SEGUNDA", "DOMICILIOS",                   "Las partes establecen los siguientes domicilios: a) LOCADOR/A: {locadorDomicilio}; {locadorEmail}. b) LOCATARIO/A: en el inmueble locado ({propiedadDireccion}); {locatarioEmail}. Ambas convienen que las comunicaciones entre sí se efectuarán por vía electrónica, las que se tendrán por válidas y plenamente eficaces (art. 75, CCyCN)."),
            (23, "VIGÉSIMA TERCERA", "DIÁLOGO",                      "Las partes se comprometen a manejarse en todo momento de buena fe y a sostener diálogo permanente, pacífico y tolerante. Ante desavenencias, se comprometen a recurrir a mediación comunitaria gratuita en la Defensoría del Pueblo."),
            (24, "VIGÉSIMA CUARTA",  "JURISDICCIÓN",                 "Las partes se someten a la jurisdicción de los Tribunales Ordinarios de la ciudad de {ciudad}, con renuncia expresa a cualquier otro fuero o jurisdicción."),
            (25, "VIGÉSIMA QUINTA",  "REGISTRACIÓN",                 "En cumplimiento de la normativa vigente, EL/LA LOCADOR/A registrará el presente contrato ante la AFIP dentro de los próximos treinta (30) días de suscripto."),
        };

        foreach (var (orden, numero, titulo, texto) in defaults)
        {
            _context.ClausulasContrato.Add(new ClausulaContrato
            {
                Orden = orden, Numero = numero, Titulo = titulo, Texto = texto,
                Activo = true, FechaCreacion = now, FechaActualizacion = now
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task MoverAsync(int id, bool subir)
    {
        var clausula = await _context.ClausulasContrato.FindAsync(id);
        if (clausula is null) return;

        if (subir)
        {
            var anterior = await _context.ClausulasContrato
                .Where(c => c.Orden < clausula.Orden)
                .OrderByDescending(c => c.Orden)
                .FirstOrDefaultAsync();
            if (anterior is not null)
            {
                (clausula.Orden, anterior.Orden) = (anterior.Orden, clausula.Orden);
                clausula.FechaActualizacion = anterior.FechaActualizacion = DateTime.UtcNow;
            }
        }
        else
        {
            var siguiente = await _context.ClausulasContrato
                .Where(c => c.Orden > clausula.Orden)
                .OrderBy(c => c.Orden)
                .FirstOrDefaultAsync();
            if (siguiente is not null)
            {
                (clausula.Orden, siguiente.Orden) = (siguiente.Orden, clausula.Orden);
                clausula.FechaActualizacion = siguiente.FechaActualizacion = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }
}
