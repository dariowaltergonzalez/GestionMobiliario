namespace GestionInmobiliaria.Dominio.Entidades;

public enum TipoEvento
{
    Visita = 1,
    Llamada = 2,
    Reunion = 3,
    Otro = 4
}

public enum EstadoEvento
{
    Pendiente = 1,
    Realizada = 2,
    Cancelada = 3
}

public class EventoAgenda : IAuditable
{
    public int Id { get; set; }
    public TipoEvento Tipo { get; set; } = TipoEvento.Visita;
    public EstadoEvento Estado { get; set; } = EstadoEvento.Pendiente;
    public DateTime FechaHora { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }

    public int AgenteId { get; set; }
    public Agente Agente { get; set; } = null!;

    public int? PropiedadId { get; set; }
    public Propiedad? Propiedad { get; set; }

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public int? InquilinoId { get; set; }
    public Inquilino? Inquilino { get; set; }
}
