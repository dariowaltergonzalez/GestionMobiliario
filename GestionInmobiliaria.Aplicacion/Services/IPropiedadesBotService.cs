namespace GestionInmobiliaria.Aplicacion.Services;

/// <summary>
/// Filtros extraídos de un mensaje en lenguaje natural — la IA solo interpreta la intención y arma
/// esto, nunca toca la base de datos ni redacta el resultado final: eso lo hace el llamador con datos
/// reales, para no arriesgarse a que "invente" una propiedad o un dato que no existe.
/// </summary>
public record ConsultaPropiedades(
    bool EsBusquedaPropiedades,
    string? Barrio,
    string? Ciudad,
    string? Operacion,
    string? Tipo,
    int? DormitoriosMinimo,
    bool? Cochera,
    bool? Pileta,
    bool? Mascotas
);

/// <summary>
/// Interfaz swappeable para interpretar mensajes de WhatsApp — hoy la implementa
/// <c>GeminiPropiedadesBotService</c>, mismo criterio que <see cref="IReciboIaService"/> y
/// <see cref="IWhatsAppService"/>.
/// </summary>
public interface IPropiedadesBotService
{
    Task<ConsultaPropiedades> InterpretarAsync(string mensaje);
}
