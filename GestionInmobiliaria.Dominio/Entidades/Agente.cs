namespace GestionInmobiliaria.Dominio.Entidades;

/// <summary>
/// Implementa <see cref="INotificable"/> para poder avisarle por email/WhatsApp cuando se le genera o
/// se le paga una comisión (ver <see cref="ComisionAgente"/>) — mismo patrón que Propietario/Inquilino.
/// El email no es propio (usa el de su <see cref="User"/>, con el que se loguea), por eso se expone
/// con implementación explícita de la interfaz en vez de una propiedad pública más.
/// </summary>
public class Agente : IAuditable, INotificable
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string? Zona { get; set; }
    public string? TelefonoInterno { get; set; }
    public decimal ComisionPorcentaje { get; set; } = 0;
    public string? Notas { get; set; }
    public string? TelefonoWhatsApp { get; set; }
    public string? Notificaciones { get; set; }
    public string? NotificacionesWhatsApp { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }

    public ICollection<Propiedad> Propiedades { get; set; } = new List<Propiedad>();
    public ICollection<Inquilino> Inquilinos { get; set; } = new List<Inquilino>();
    public ICollection<Lead> Leads { get; set; } = new List<Lead>();
    public ICollection<EventoAgenda> Eventos { get; set; } = new List<EventoAgenda>();

    string? INotificable.Email => User?.Email;
}
