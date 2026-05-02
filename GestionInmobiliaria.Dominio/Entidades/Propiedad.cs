namespace GestionInmobiliaria.Dominio.Entidades;

public enum TipoPropiedad
{
    Departamento = 1,
    Casa = 2,
    Local = 3,
    Oficina = 4,
    Terreno = 5,
    Otro = 6
}

public enum EstadoPropiedad
{
    Disponible = 1,
    Alquilada = 2,
    EnMantenimiento = 3,
    NoDisponible = 4
}

public class Propiedad : IAuditable
{
    public int Id { get; set; }
    public TipoPropiedad Tipo { get; set; }
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
    public decimal PrecioAlquiler { get; set; }
    public EstadoPropiedad Estado { get; set; } = EstadoPropiedad.Disponible;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }

    public int PropietarioId { get; set; }
    public Propietario Propietario { get; set; } = null!;
}
