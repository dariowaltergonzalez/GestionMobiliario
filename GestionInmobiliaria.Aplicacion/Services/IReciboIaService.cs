namespace GestionInmobiliaria.Aplicacion.Services;

/// <summary>
/// Datos que un modelo de IA con visión pudo extraer de la foto de un comprobante de transferencia
/// (Mercado Pago, home banking, etc). Todos los campos son opcionales — la IA puede no encontrar
/// alguno, y el usuario los completa/corrige a mano antes de guardar. Nunca se autoguarda solo.
/// </summary>
public record DatosComprobante(
    decimal? Monto,
    DateTime? Fecha,
    string? CbuCvuDestino,
    string? EntidadDestino,
    string? NumeroOperacion);

/// <summary>
/// Único punto de contacto con el proveedor de IA con visión usado para leer comprobantes. Se arrancó
/// con Gemini (tier gratis para desarrollar, ver <see cref="GestionInmobiliaria.Infraestructura.Services"/>
/// namespace `GeminiReciboIaService`) — si el día de mañana se cambia de proveedor (OpenAI, Claude,
/// etc.), alcanza con crear una nueva implementación de esta interfaz y cambiar el registro en
/// Program.cs; nada del resto del sistema (controller, frontend) se entera del cambio. Ver
/// docs/logica-negocio.md, sección LIQUIDACIÓN.
/// </summary>
public interface IReciboIaService
{
    Task<DatosComprobante> ExtraerDatosAsync(Stream imagen, string contentType);
}
