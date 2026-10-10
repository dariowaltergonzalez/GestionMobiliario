namespace GestionInmobiliaria.Aplicacion.DTOs;

public class ComisionAgenteDto
{
    public int Id { get; set; }
    public int ContratoId { get; set; }
    public string ContratoCodigo { get; set; } = string.Empty;
    public string PropiedadDireccion { get; set; } = string.Empty;
    public int AgenteId { get; set; }
    public string AgenteNombre { get; set; } = string.Empty;
    public decimal MontoComisionInmobiliaria { get; set; }
    public decimal PorcentajeAgente { get; set; }
    public decimal MontoAgente { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaPago { get; set; }
    public string? Medio { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class ComisionAgenteMetricasDto
{
    public int PendientesCount { get; set; }
    public decimal MontoTotalPendiente { get; set; }
    public int PagadasMesCount { get; set; }
    public decimal MontoPagadoMes { get; set; }
}

public class RegistrarPagoComisionRequest
{
    public DateTime? Fecha { get; set; }
    public int Medio { get; set; }
    public string? Observaciones { get; set; }
}
