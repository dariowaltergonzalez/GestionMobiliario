namespace GestionInmobiliaria.Dominio.Entidades;

public interface INotificable
{
    bool Activo { get; }
    string? Email { get; }
    string? Notificaciones { get; }
}
