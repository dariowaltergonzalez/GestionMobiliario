namespace GestionInmobiliaria.Dominio.Entidades;

public enum TipoContactoPreferido
{
    Llamada = 1,
    Visita = 2,
    Online = 3
}

public enum EstadoSolicitudTasacion
{
    Pendiente = 1,
    Asignada = 2,
    EnProceso = 3,
    Completada = 4,
    Cancelada = 5
}

public class SolicitudTasacion : IAuditable
{
    public int Id { get; set; }

    // Datos del solicitante
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Telefono { get; set; } = string.Empty;

    // Datos de la propiedad
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

    // Preferencia de contacto
    public TipoContactoPreferido TipoContactoPreferido { get; set; }

    // Seguimiento interno (solo agentes)
    public EstadoSolicitudTasacion Estado { get; set; } = EstadoSolicitudTasacion.Pendiente;
    public string? NotasInternas { get; set; }
    public decimal? ValorEstimado { get; set; }

    // Operación y moneda del ValorEstimado — explícitos a propósito (decisión del usuario, 2026-10-31):
    // antes no existían y la UI asumía ARS siempre, lo cual era incorrecto para una tasación de venta
    // (que en Argentina se tasa en USD). El agente los define a mano al cargar el valor estimado, no
    // se infieren de ningún otro campo.
    public TipoOperacion Operacion { get; set; } = TipoOperacion.Venta;
    public Moneda Moneda { get; set; } = Moneda.ARS;

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }

    public int? AgenteId { get; set; }
    public Agente? Agente { get; set; }

    public ICollection<FotoSolicitud> Fotos { get; set; } = new List<FotoSolicitud>();
}
