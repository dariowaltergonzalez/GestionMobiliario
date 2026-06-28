using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Aplicacion.DTOs;

public class EventoAgendaDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string? Notas { get; set; }
    public int AgenteId { get; set; }
    public string AgenteNombre { get; set; } = string.Empty;
    public int? PropiedadId { get; set; }
    public string? PropiedadDireccion { get; set; }
    public int? LeadId { get; set; }
    public string? LeadNombre { get; set; }
    public int? InquilinoId { get; set; }
    public string? InquilinoNombre { get; set; }
}

public class CreateEventoAgendaRequest
{
    public TipoEvento Tipo { get; set; } = TipoEvento.Visita;
    public DateTime FechaHora { get; set; }
    public string? Notas { get; set; }
    public int AgenteId { get; set; }
    public int? PropiedadId { get; set; }
    public int? LeadId { get; set; }
    public int? InquilinoId { get; set; }
}

public class UpdateEventoAgendaRequest
{
    public TipoEvento Tipo { get; set; }
    public EstadoEvento Estado { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Notas { get; set; }
    public int AgenteId { get; set; }
    public int? PropiedadId { get; set; }
    public int? LeadId { get; set; }
    public int? InquilinoId { get; set; }
}
