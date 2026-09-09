namespace GestionInmobiliaria.Dominio.Entidades;

public interface INotificable
{
    bool Activo { get; }
    string? Email { get; }
    string? Notificaciones { get; }
    /// <summary>
    /// Número dedicado para WhatsApp (formato internacional, ej. "+549..."), distinto del `Telefono`
    /// general de contacto — mucha gente usa un número exclusivo para WhatsApp, y el `Telefono` de
    /// contacto no siempre está en el formato que exige la API.
    /// </summary>
    string? TelefonoWhatsApp { get; }
    string? NotificacionesWhatsApp { get; }
}
