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
    public decimal MontoAbonado { get; set; }
    public decimal MontoRestante { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaLiquidacion { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<LiquidacionAbonoDto> Abonos { get; set; } = [];
}

public class LiquidacionAbonoDto
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public string Medio { get; set; } = string.Empty;
    public string? CbuCvuDestino { get; set; }
    public string? EntidadDestino { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? Observaciones { get; set; }
}

public class AbonoLiquidacionRequest
{
    public decimal Monto { get; set; }
    public DateTime? Fecha { get; set; }
    public int Medio { get; set; }
    public string? CbuCvuDestino { get; set; }
    public string? EntidadDestino { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? Observaciones { get; set; }
}

public class LiquidacionMetricasDto
{
    public int PendientesCount { get; set; }
    public decimal MontoPendienteTotal { get; set; }
    public int LiquidadasMesCount { get; set; }
    public decimal MontoLiquidadoMes { get; set; }
}
