namespace GestionInmobiliaria.Dominio.Entidades;

/// <summary>
/// Un valor mensual del IPC Nacional Nivel General que publica el INDEC (serie
/// "148.3_INIVELNAL_DICI_M_26" en apis.datos.gob.ar/series). A diferencia de ICL/UVA/TIM (BCRA), el
/// IPC NO es un índice acumulado propio del sistema financiero, es un índice de precios — igual se
/// usa dividiendo dos valores para sacar la variación acumulada del período:
/// nuevoMonto = montoActual * (Valor(fechaAjuste) / Valor(fechaInicioPeriodo)). Ver
/// docs/logica-negocio.md, sección AJUSTE AUTOMÁTICO. Tabla separada a propósito (mismo criterio que
/// IndiceIcl/IndiceUva: no se generaliza). Es un dato GLOBAL (no depende del Tenant) — a propósito no
/// tiene TenantId ni query filter.
/// </summary>
public class IndiceIpc : IAuditable
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }
    public decimal Valor { get; set; }

    public string Origen { get; set; } = "INDEC";
    public DateTime FechaConsulta { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
