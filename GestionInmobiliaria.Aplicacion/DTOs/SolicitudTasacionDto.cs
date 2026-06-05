using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Aplicacion.DTOs;

public class SolicitudTasacionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string TipoPropiedad { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? Barrio { get; set; }
    public string? Ciudad { get; set; }
    public decimal? SuperficieTotal { get; set; }
    public decimal? SuperficieCubierta { get; set; }
    public int? Ambientes { get; set; }
    public int? Banios { get; set; }
    public int? Antiguedad { get; set; }
    public string EstadoConservacion { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string TipoContactoPreferido { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? NotasInternas { get; set; }
    public decimal? ValorEstimado { get; set; }
    public int? AgenteId { get; set; }
    public string? NombreAgente { get; set; }
    public List<FotoSolicitudDto> Fotos { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class FotoSolicitudDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? NombreArchivo { get; set; }
    public DateTime FechaSubida { get; set; }
}

public class CreateSolicitudTasacionRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public TipoPropiedad TipoPropiedad { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string? Barrio { get; set; }
    public string? Ciudad { get; set; }
    public decimal? SuperficieTotal { get; set; }
    public decimal? SuperficieCubierta { get; set; }
    public int? Ambientes { get; set; }
    public int? Banios { get; set; }
    public int? Antiguedad { get; set; }
    public EstadoConservacion EstadoConservacion { get; set; }
    public string? Descripcion { get; set; }
    public TipoContactoPreferido TipoContactoPreferido { get; set; }
}

public class UpdateSolicitudTasacionRequest
{
    public EstadoSolicitudTasacion Estado { get; set; }
    public int? AgenteId { get; set; }
    public string? NotasInternas { get; set; }
    public decimal? ValorEstimado { get; set; }
}
