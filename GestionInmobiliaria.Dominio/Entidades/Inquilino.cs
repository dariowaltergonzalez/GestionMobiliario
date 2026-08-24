namespace GestionInmobiliaria.Dominio.Entidades;

public class Inquilino : IAuditable, INotificable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Cuit { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string? Direccion { get; set; }
    public string? Ocupacion { get; set; }
    public string? NombreGarante { get; set; }
    public string? TelefonoGarante { get; set; }
    public string? DniGarante { get; set; }
    public string? Notas { get; set; }
    public string? Notificaciones { get; set; }

    // Token del portal de autoservicio (sin login) — formato "{TenantId}.{secreto}", ver
    // docs/logica-negocio.md sección PORTAL DE AUTOSERVICIO. Null hasta que se genera la primera vez.
    public string? TokenPortal { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }

    public int? AgenteId { get; set; }
    public Agente? Agente { get; set; }
}
