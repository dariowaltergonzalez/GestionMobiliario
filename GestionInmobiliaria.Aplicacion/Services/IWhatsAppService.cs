namespace GestionInmobiliaria.Aplicacion.Services;

/// <summary>
/// A diferencia del email, WhatsApp no admite texto libre para mensajes que inicia el sistema — Meta
/// exige el uso de una plantilla pre-aprobada con variables. <see cref="Sid"/> identifica esa
/// plantilla ante el proveedor (en Twilio, el ContentSid; en Meta Cloud API sería el nombre de la
/// plantilla) y <see cref="Variables"/> son los valores ordenados para sus placeholders ({{1}}, {{2}}...).
/// </summary>
public class WhatsAppPlantilla
{
    public required string Sid { get; init; }
    public required IReadOnlyList<string> Variables { get; init; }
}

/// <summary>
/// Interfaz swappeable para el envío de WhatsApp — hoy la implementa <c>TwilioWhatsAppService</c>
/// (sandbox de Twilio); cambiar de proveedor (ej. a Meta Cloud API directo) es crear otra clase y
/// tocar el registro en Program.cs, sin afectar a los que llaman a esta interfaz. Mismo criterio que
/// <see cref="IReciboIaService"/>.
/// </summary>
public interface IWhatsAppService
{
    Task EnviarAsync(string telefono, WhatsAppPlantilla plantilla);
}
