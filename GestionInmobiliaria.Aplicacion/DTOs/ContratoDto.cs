namespace GestionInmobiliaria.Aplicacion.DTOs;

public class ContratoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;

    public int PropiedadId { get; set; }
    public string PropiedadDireccion { get; set; } = string.Empty;
    public string? PropiedadCodigo { get; set; }

    public int? ReservaId { get; set; }
    public int? AgenteId { get; set; }
    public string? AgenteNombre { get; set; }

    public int? PropietarioRefId { get; set; }
    public int? InquilinoRefId { get; set; }

    public string LocadorNombre { get; set; } = string.Empty;
    public string LocadorApellido { get; set; } = string.Empty;
    public string? LocadorDni { get; set; }
    public string? LocadorEmail { get; set; }
    public string? LocadorTelefono { get; set; }
    public string? LocadorDomicilio { get; set; }
    public string? LocadorBanco { get; set; }
    public string? LocadorCbu { get; set; }
    public string? LocadorCuit { get; set; }

    public string LocatarioNombre { get; set; } = string.Empty;
    public string LocatarioApellido { get; set; } = string.Empty;
    public string? LocatarioDni { get; set; }
    public string? LocatarioEmail { get; set; }
    public string? LocatarioTelefono { get; set; }

    public string? GaranteNombre { get; set; }
    public string? GaranteApellido { get; set; }
    public string? GaranteDni { get; set; }
    public string? GaranteTelefono { get; set; }

    public decimal MontoBase { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string TipoAjuste { get; set; } = string.Empty;
    public int? PeriodicidadAjusteMeses { get; set; }
    public int? DiaVencimientoPago { get; set; }

    public decimal? ComisionLocadorPorcentaje { get; set; }
    public decimal? ComisionLocadorMonto { get; set; }
    public decimal? ComisionLocatarioPorcentaje { get; set; }
    public decimal? ComisionLocatarioMonto { get; set; }

    public bool AdministracionCobros { get; set; }

    public decimal? PorcentajeAjuste { get; set; }
    public decimal MontoActual { get; set; }
    public DateTime? FechaUltimoAjuste { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime? FechaEscrituracion { get; set; }

    public string? MotivoRescision { get; set; }
    public DateTime? FechaRescision { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public string? Observaciones { get; set; }
    public string? ArchivoUrl { get; set; }

    public List<PagoDto> Pagos { get; set; } = new();
    public List<AjusteContratoDto> Ajustes { get; set; } = new();

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class PagoDetalleDto
{
    public int Id { get; set; }
    public string Medio { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public string? ChequeBanco { get; set; }
    public string? ChequeNumero { get; set; }
    public DateTime? ChequeFechaVencimiento { get; set; }
}

public class PagoDetalleRequest
{
    public int Medio { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public string? ChequeBanco { get; set; }
    public string? ChequeNumero { get; set; }
    public string? ChequeFechaVencimiento { get; set; }
}

public class PagoDto
{
    public int Id { get; set; }
    public int ContratoId { get; set; }
    public int NumeroCuota { get; set; }
    public DateTime Periodo { get; set; }
    public decimal MontoEsperado { get; set; }
    public decimal? MontoPagado { get; set; }
    public DateTime? FechaPago { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public List<PagoDetalleDto> Detalles { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class CreateContratoRequest
{
    public int Tipo { get; set; }
    public int Estado { get; set; } = 1;
    public int PropiedadId { get; set; }
    public int? ReservaId { get; set; }
    public int? AgenteId { get; set; }
    public int? PropietarioRefId { get; set; }
    public int? InquilinoRefId { get; set; }

    public string LocadorNombre { get; set; } = string.Empty;
    public string LocadorApellido { get; set; } = string.Empty;
    public string? LocadorDni { get; set; }
    public string? LocadorEmail { get; set; }
    public string? LocadorTelefono { get; set; }
    public string? LocadorDomicilio { get; set; }
    public string? LocadorBanco { get; set; }
    public string? LocadorCbu { get; set; }
    public string? LocadorCuit { get; set; }

    public string LocatarioNombre { get; set; } = string.Empty;
    public string LocatarioApellido { get; set; } = string.Empty;
    public string? LocatarioDni { get; set; }
    public string? LocatarioEmail { get; set; }
    public string? LocatarioTelefono { get; set; }

    public string? GaranteNombre { get; set; }
    public string? GaranteApellido { get; set; }
    public string? GaranteDni { get; set; }
    public string? GaranteTelefono { get; set; }

    public decimal MontoBase { get; set; }
    public int Moneda { get; set; } = 1;
    public int TipoAjuste { get; set; } = 1;
    public int? PeriodicidadAjusteMeses { get; set; }
    public int? DiaVencimientoPago { get; set; }

    public decimal? ComisionLocadorPorcentaje { get; set; }
    public decimal? ComisionLocadorMonto { get; set; }
    public decimal? ComisionLocatarioPorcentaje { get; set; }
    public decimal? ComisionLocatarioMonto { get; set; }

    public bool AdministracionCobros { get; set; } = false;

    public decimal? PorcentajeAjuste { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime? FechaEscrituracion { get; set; }

    public string? Observaciones { get; set; }
}

public class UpdateContratoRequest : CreateContratoRequest { }

public class TransicionEstadoRequest
{
    public int Estado { get; set; }
    public string? Motivo { get; set; }
    public DateTime? Fecha { get; set; }
}

public class UpdatePagoRequest
{
    public int Estado { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? Observaciones { get; set; }
    public List<PagoDetalleRequest> Detalles { get; set; } = new();
}

public class PagoListDto : PagoDto
{
    public string ContratoCodigo { get; set; } = string.Empty;
    public string PropiedadDireccion { get; set; } = string.Empty;
    public string LocatarioNombre { get; set; } = string.Empty;
    public string LocatarioApellido { get; set; } = string.Empty;
    public string LocadorNombre { get; set; } = string.Empty;
    public string LocadorApellido { get; set; } = string.Empty;
    public string? LocadorEmail { get; set; }
}


public class DocumentoContratoDto
{
    public int Id { get; set; }
    public int ContratoId { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string TipoMime { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class AjusteContratoDto
{
    public int Id { get; set; }
    public int ContratoId { get; set; }
    public DateTime FechaAplicacion { get; set; }
    public decimal MontoPrevio { get; set; }
    public decimal MontoNuevo { get; set; }
    public decimal? Porcentaje { get; set; }
    public string TipoAjuste { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

public class PagoMetricasDto
{
    public int PendientesCount { get; set; }
    public int AtrasadosCount { get; set; }
    public int PagadosMesCount { get; set; }
    public decimal MontoCobradoMes { get; set; }
    public decimal MontoTotalPendiente { get; set; }
}
