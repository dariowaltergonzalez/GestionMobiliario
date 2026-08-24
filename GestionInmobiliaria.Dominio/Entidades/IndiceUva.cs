namespace GestionInmobiliaria.Dominio.Entidades;

/// <summary>
/// Un valor diario de la "Unidad de Valor Adquisitivo" (UVA) que publica el BCRA (idVariable=31 en
/// api.bcra.gob.ar/estadisticas/v4.0/monetarias). Mismo mecanismo de índice acumulado que IndiceIcl —
/// se usa dividiendo dos valores del índice: nuevoMonto = montoActual * (Valor(fechaAjuste) /
/// Valor(fechaInicioPeriodo)). Ver docs/logica-negocio.md, sección AJUSTE AUTOMÁTICO. Tabla separada
/// a propósito (mismo criterio que IndiceIcl/TasaMoratoria: no se generaliza). Es un dato GLOBAL (no
/// depende del Tenant) — a propósito no tiene TenantId ni query filter.
/// </summary>
public class IndiceUva : IAuditable
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }
    public decimal Valor { get; set; }

    public string Origen { get; set; } = "BCRA";
    public DateTime FechaConsulta { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
