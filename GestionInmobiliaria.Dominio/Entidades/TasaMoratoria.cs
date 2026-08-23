namespace GestionInmobiliaria.Dominio.Entidades;

/// <summary>
/// Un valor diario del índice "Tasa de Intereses Moratorios" (TIM, art. 768(c) CCyCN) que publica
/// el BCRA (idVariable=1197 en api.bcra.gob.ar/estadisticas/v4.0/monetarias). Es un índice ACUMULADO,
/// no una tasa % periódica: el recargo por mora entre dos fechas se calcula como
/// Monto * (Valor(fechaCobro) / Valor(fechaVencimiento) - 1). Ver docs/logica-negocio.md, sección
/// PUNITORIOS, para el detalle completo de por qué se eligió este índice y cómo se usa.
/// Es un dato GLOBAL (no depende del Tenant, es el mismo para cualquier inmobiliaria) — a propósito
/// no tiene TenantId ni query filter.
/// </summary>
public class TasaMoratoria : IAuditable
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }
    public decimal Valor { get; set; }

    public string Origen { get; set; } = "BCRA";
    public DateTime FechaConsulta { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
