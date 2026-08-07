namespace GestionInmobiliaria.Aplicacion.DTOs;

public class LiquidacionDto
{
    public int Id { get; set; }
    public int PagoId { get; set; }
    public int ContratoId { get; set; }
    public string ContratoCodigo { get; set; } = string.Empty;
    public string PropiedadDireccion { get; set; } = string.Empty;
    public int? PropietarioRefId { get; set; }
    public string PropietarioNombre { get; set; } = string.Empty;
    public string PropietarioApellido { get; set; } = string.Empty;
    public int NumeroCuota { get; set; }
    public DateTime Periodo { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public decimal MontoCobrado { get; set; }
    public decimal? ComisionPorcentaje { get; set; }
    public decimal? ComisionMonto { get; set; }
    public decimal MontoComision { get; set; }
    public decimal MontoALiquidar { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaLiquidacion { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class MarcarLiquidadaRequest
{
    public DateTime? Fecha { get; set; }
    public string? Observaciones { get; set; }
}

public class LiquidacionMetricasDto
{
    public int PendientesCount { get; set; }
    public decimal MontoPendienteTotal { get; set; }
    public int LiquidadasMesCount { get; set; }
    public decimal MontoLiquidadoMes { get; set; }
}
