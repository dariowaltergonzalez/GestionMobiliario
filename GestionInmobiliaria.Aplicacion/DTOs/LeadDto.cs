using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Aplicacion.DTOs;

public class LeadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombre} {Apellido}";
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Notas { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int? AgenteId { get; set; }
    public string? AgenteNombre { get; set; }
    public int? PropiedadId { get; set; }
    public string? PropiedadDireccion { get; set; }
}

public class CreateLeadRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public OrigenLead Origen { get; set; } = OrigenLead.Web;
    public string? Notas { get; set; }
    public int? AgenteId { get; set; }
    public int? PropiedadId { get; set; }
}

public class UpdateLeadRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public OrigenLead Origen { get; set; }
    public EstadoLead Estado { get; set; }
    public string? Notas { get; set; }
    public int? AgenteId { get; set; }
    public int? PropiedadId { get; set; }
}

public class LeadComboDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class SolicitarVisitaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public int PropiedadId { get; set; }
    public DateTime FechaHoraPreferida { get; set; }
    public string? Notas { get; set; }
}
