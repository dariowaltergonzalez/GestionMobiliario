namespace GestionInmobiliaria.Dominio.Entidades;

public enum CategoriaGasto
{
    Reparacion = 1,
    Impuesto = 2,
    Expensas = 3,
    Seguro = 4,
    Otro = 5,
}

public enum ResponsableGasto
{
    Propietario = 1,
    Inquilino = 2,
}

public enum EstadoGasto
{
    Pendiente = 1,
    Resuelto = 2,
}

/// <summary>
/// Un gasto de una propiedad (reparación, impuesto, expensas...), que puede ser a cargo del
/// propietario (se descuenta de la próxima Liquidacion) o del inquilino (se registra y se marca
/// Resuelto cuando la inmobiliaria lo cobra por fuera — no toca Pago.MontoEsperado, ver
/// docs/logica-negocio.md sección GASTOS).
/// </summary>
public class Gasto : IAuditable
{
    public int Id { get; set; }

    public int PropiedadId { get; set; }
    public Propiedad Propiedad { get; set; } = null!;

    // Contexto opcional: qué contrato estaba vigente cuando se cargó el gasto. La propiedad es lo
    // que no cambia, el contrato sí (puede haber varios inquilinos a lo largo del tiempo).
    public int? ContratoId { get; set; }
    public Contrato? Contrato { get; set; }

    public CategoriaGasto Categoria { get; set; }
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }

    public ResponsableGasto Responsable { get; set; }
    public EstadoGasto Estado { get; set; } = EstadoGasto.Pendiente;
    public DateTime? FechaResolucion { get; set; }

    // Solo tienen sentido cuando Responsable=Inquilino: cómo y cuándo se cobró la deuda al marcarlo
    // Resuelto (el descuento a Propietario vía Liquidacion no pasa por acá, ver LiquidacionId).
    // Mismos campos que PagoDetalle, para que el detalle del cobro tenga el mismo nivel que Pagos.
    public MedioPago? MedioCobro { get; set; }
    public DateTime? FechaCobro { get; set; }
    public string? ReferenciaCobro { get; set; }
    public string? ChequeBanco { get; set; }
    public string? ChequeNumero { get; set; }
    public DateTime? ChequeFechaVencimiento { get; set; }
    public string? ObservacionesResolucion { get; set; }

    // Si Responsable=Propietario y ya se descontó de una Liquidacion, queda la referencia.
    public int? LiquidacionId { get; set; }
    public Liquidacion? Liquidacion { get; set; }

    public bool VisibleParaInquilino { get; set; } = true;

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }
}
