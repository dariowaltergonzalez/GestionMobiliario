using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Aplicacion.DTOs;

public class PropiedadDto
{
    public int Id { get; set; }
    public TipoPropiedad Tipo { get; set; }
    public string TipoNombre => Tipo.ToString();
    public TipoOperacion Operacion { get; set; }
    public string OperacionNombre => Operacion.ToString();
    public string Direccion { get; set; } = string.Empty;
    public string? Barrio { get; set; }
    public string? Ciudad { get; set; }
    public string? Provincia { get; set; }
    public string DireccionCompleta => string.Join(", ", new[] { Direccion, Barrio, Ciudad, Provincia }.Where(s => !string.IsNullOrWhiteSpace(s)));
    public int? Ambientes { get; set; }
    public int? Dormitorios { get; set; }
    public int? Banios { get; set; }
    public decimal? SuperficieTotal { get; set; }
    public decimal? SuperficieCubierta { get; set; }
    public string? Piso { get; set; }
    public string? NumeroDepartamento { get; set; }
    public decimal? PrecioAlquiler { get; set; }
    public decimal? PrecioVenta { get; set; }
    public decimal? Expensas { get; set; }
    public EstadoPropiedad Estado { get; set; }
    public string EstadoNombre => Estado.ToString();
    public EstadoConservacion? EstadoConservacion { get; set; }
    public string? EstadoConservacionNombre => EstadoConservacion?.ToString();
    public bool Cochera { get; set; }
    public int? Antiguedad { get; set; }
    public bool TieneCalefaccion { get; set; }
    public bool AceptaMascotas { get; set; }
    public string? NroCatastro { get; set; }
    public string? Descripcion { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int PropietarioId { get; set; }
    public string PropietarioNombre { get; set; } = string.Empty;
}

public class CreatePropiedadRequest
{
    public TipoPropiedad Tipo { get; set; }
    public TipoOperacion Operacion { get; set; } = TipoOperacion.Alquiler;
    public string Direccion { get; set; } = string.Empty;
    public string? Barrio { get; set; }
    public string? Ciudad { get; set; }
    public string? Provincia { get; set; }
    public int? Ambientes { get; set; }
    public int? Dormitorios { get; set; }
    public int? Banios { get; set; }
    public decimal? SuperficieTotal { get; set; }
    public decimal? SuperficieCubierta { get; set; }
    public string? Piso { get; set; }
    public string? NumeroDepartamento { get; set; }
    public decimal? PrecioAlquiler { get; set; }
    public decimal? PrecioVenta { get; set; }
    public decimal? Expensas { get; set; }
    public EstadoPropiedad Estado { get; set; } = EstadoPropiedad.Disponible;
    public EstadoConservacion? EstadoConservacion { get; set; }
    public bool Cochera { get; set; }
    public int? Antiguedad { get; set; }
    public bool TieneCalefaccion { get; set; }
    public bool AceptaMascotas { get; set; }
    public string? NroCatastro { get; set; }
    public string? Descripcion { get; set; }
    public string? Notas { get; set; }
    public int PropietarioId { get; set; }
}

public class UpdatePropiedadRequest
{
    public TipoPropiedad Tipo { get; set; }
    public TipoOperacion Operacion { get; set; } = TipoOperacion.Alquiler;
    public string Direccion { get; set; } = string.Empty;
    public string? Barrio { get; set; }
    public string? Ciudad { get; set; }
    public string? Provincia { get; set; }
    public int? Ambientes { get; set; }
    public int? Dormitorios { get; set; }
    public int? Banios { get; set; }
    public decimal? SuperficieTotal { get; set; }
    public decimal? SuperficieCubierta { get; set; }
    public string? Piso { get; set; }
    public string? NumeroDepartamento { get; set; }
    public decimal? PrecioAlquiler { get; set; }
    public decimal? PrecioVenta { get; set; }
    public decimal? Expensas { get; set; }
    public EstadoPropiedad Estado { get; set; }
    public EstadoConservacion? EstadoConservacion { get; set; }
    public bool Cochera { get; set; }
    public int? Antiguedad { get; set; }
    public bool TieneCalefaccion { get; set; }
    public bool AceptaMascotas { get; set; }
    public string? NroCatastro { get; set; }
    public string? Descripcion { get; set; }
    public string? Notas { get; set; }
    public int PropietarioId { get; set; }
}

public class PropiedadComboDto
{
    public int Id { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string TipoNombre { get; set; } = string.Empty;
    public decimal? PrecioAlquiler { get; set; }
    public decimal? PrecioVenta { get; set; }
    public string PropietarioNombre { get; set; } = string.Empty;
}
