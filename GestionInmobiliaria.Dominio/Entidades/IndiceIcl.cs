namespace GestionInmobiliaria.Dominio.Entidades;

/// <summary>
/// Un valor diario del "Índice para Contratos de Locación" (ICL) que publica el BCRA
/// (idVariable=40 en api.bcra.gob.ar/estadisticas/v4.0/monetarias). Es un índice ACUMULADO, igual
/// mecanismo que TasaMoratoria (TIM) — se usa dividiendo dos valores del índice:
/// nuevoMonto = montoActual * (Valor(fechaAjuste) / Valor(fechaInicioPeriodo)). Ver
/// docs/logica-negocio.md, sección PENDIENTES GENERALES → "Automatizar el ajuste periódico de
/// cuotas". Tabla separada de TasaMoratoria a propósito (decisión del usuario 2026-08-24: no
/// generalizar, mantener cada índice desacoplado). Es un dato GLOBAL (no depende del Tenant) — a
/// propósito no tiene TenantId ni query filter.
/// </summary>
public class IndiceIcl : IAuditable
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }
    public decimal Valor { get; set; }

    public string Origen { get; set; } = "BCRA";
    public DateTime FechaConsulta { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
