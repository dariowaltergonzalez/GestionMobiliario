namespace GestionInmobiliaria.Dominio.Entidades;

public interface INotificable
{
    bool Activo { get; }
    string? Email { get; }
    string? Notificaciones { get; }
    string? Telefono { get; }
    string? NotificacionesWhatsApp { get; }
}
