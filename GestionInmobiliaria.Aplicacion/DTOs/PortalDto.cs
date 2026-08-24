namespace GestionInmobiliaria.Aplicacion.DTOs;

public class PortalInquilinoDto
{
    public string NombreEmpresa { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string InquilinoNombre { get; set; } = string.Empty;
    public string InquilinoApellido { get; set; } = string.Empty;
    public PortalContratoDto? Contrato { get; set; }
    public List<PortalPagoDto> Pagos { get; set; } = [];
    public List<PortalGastoDto> Gastos { get; set; } = [];
}

public class PortalContratoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string PropiedadDireccion { get; set; } = string.Empty;
    public decimal MontoActual { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}

public class PortalPagoDto
{
    public int NumeroCuota { get; set; }
    public DateTime Periodo { get; set; }
    public decimal MontoEsperado { get; set; }
    public decimal? MontoPagado { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaPago { get; set; }
    public decimal MontoPunitorio { get; set; }
    public int DiasAtraso { get; set; }
}

public class PortalGastoDto
{
    public string Categoria { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class PortalPropietarioDto
{
    public string NombreEmpresa { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PropietarioNombre { get; set; } = string.Empty;
    public string PropietarioApellido { get; set; } = string.Empty;
    public List<PortalLiquidacionDto> Liquidaciones { get; set; } = [];
}

public class PortalLiquidacionDto
{
    public string PropiedadDireccion { get; set; } = string.Empty;
    public string ContratoCodigo { get; set; } = string.Empty;
    public DateTime Periodo { get; set; }
    public decimal MontoCobrado { get; set; }
    public decimal MontoComision { get; set; }
    public decimal MontoGastos { get; set; }
    public decimal MontoALiquidar { get; set; }
    public decimal MontoAbonado { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaLiquidacion { get; set; }
    public List<PortalAbonoDto> Abonos { get; set; } = [];
    public List<PortalGastoDto> Gastos { get; set; } = [];
}

public class PortalAbonoDto
{
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public string Medio { get; set; } = string.Empty;
    public string? CbuCvuDestino { get; set; }
    public string? EntidadDestino { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? ComprobanteUrl { get; set; }
}
