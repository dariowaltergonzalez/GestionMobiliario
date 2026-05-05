namespace GestionInmobiliaria.Dominio.Entidades;

public class Agente : IAuditable
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string? Zona { get; set; }
    public string? TelefonoInterno { get; set; }
    public decimal ComisionPorcentaje { get; set; } = 0;
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }

    public ICollection<Propiedad> Propiedades { get; set; } = new List<Propiedad>();
    public ICollection<Inquilino> Inquilinos { get; set; } = new List<Inquilino>();
    public ICollection<Lead> Leads { get; set; } = new List<Lead>();
    public ICollection<EventoAgenda> Eventos { get; set; } = new List<EventoAgenda>();
}
