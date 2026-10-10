namespace GestionInmobiliaria.Dominio.Entidades;

public enum EstadoComisionAgente
{
    Pendiente = 1,
    Pagado = 2,
}

/// <summary>
/// Lo que le corresponde cobrar al agente por haber cerrado un contrato (venta o alquiler) — distinto
/// de la comisión de administración de cobros (<see cref="Liquidacion"/>), que es recurrente por cada
/// cuota cobrada. Se genera una sola vez, al pasar el contrato a Vigente, sobre el total que cobra la
/// inmobiliaria en esa operación (ComisionLocador + ComisionLocatario del contrato). El % del agente
/// queda congelado al generarse (snapshot de <see cref="Agente.ComisionPorcentaje"/> en ese momento),
/// para que un cambio posterior en su ficha no altere comisiones ya generadas.
/// Ver docs/logica-negocio.md, sección COMISIONES DE AGENTES.
/// </summary>
public class ComisionAgente : IAuditable
{
    public int Id { get; set; }

    public int ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;

    public int AgenteId { get; set; }
    public Agente Agente { get; set; } = null!;

    public decimal MontoComisionInmobiliaria { get; set; }
    public decimal PorcentajeAgente { get; set; }
    public decimal MontoAgente { get; set; }

    public EstadoComisionAgente Estado { get; set; } = EstadoComisionAgente.Pendiente;

    // Datos del pago — se completan al registrar el pago, se limpian si se deshace.
    public DateTime? FechaPago { get; set; }
    public MedioPago? Medio { get; set; }
    public string? Observaciones { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }
}
