namespace GestionInmobiliaria.Dominio.Entidades;

public enum TipoPropiedad
{
    Departamento = 1,
    Casa = 2,
    Local = 3,
    Oficina = 4,
    Terreno = 5,
    Galpon = 6,
    PH = 7,
    Otro = 8
}

public enum TipoOperacion
{
    Alquiler = 1,
    Venta = 2,
    AlquilerOVenta = 3
}

public enum EstadoPropiedad
{
    Disponible = 1,
    Alquilada = 2,
    EnMantenimiento = 3,
    NoDisponible = 4,
    Vendida = 5,
    Reservada = 6,
    BoletoFirmado = 7
}

public enum EstadoConservacion
{
    Excelente = 1,
    Bueno = 2,
    Regular = 3,
    ARefaccionar = 4
}

public class Propiedad : IAuditable
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
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
    public bool Cochera { get; set; } = false;
    public int? Antiguedad { get; set; }
    public bool TieneCalefaccion { get; set; } = false;
    public bool AceptaMascotas { get; set; } = false;
    public bool TienePiscina { get; set; } = false;
    public string? NroCatastro { get; set; }
    public string? Descripcion { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }

    public int PropietarioId { get; set; }
    public Propietario Propietario { get; set; } = null!;

    public int? AgenteId { get; set; }
    public Agente? Agente { get; set; }

    public string? VideoUrl { get; set; }

    public ICollection<FotoPropiedad> Fotos { get; set; } = new List<FotoPropiedad>();
}
