namespace GestionInmobiliaria.Dominio.Entidades;

public enum OrigenLead
{
    Web = 1,
    WhatsApp = 2,
    Referido = 3,
    RedesSociales = 4,
    Llamada = 5,
    Otro = 6
}

public enum EstadoLead
{
    Nuevo = 1,
    Contactado = 2,
    Interesado = 3,
    Convertido = 4,
    Descartado = 5
}

public class Lead : IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public OrigenLead Origen { get; set; } = OrigenLead.Web;
    public EstadoLead Estado { get; set; } = EstadoLead.Nuevo;
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }

    public int? AgenteId { get; set; }
    public Agente? Agente { get; set; }

    public int? PropiedadId { get; set; }
    public Propiedad? Propiedad { get; set; }

    public ICollection<EventoAgenda> Eventos { get; set; } = new List<EventoAgenda>();
}
