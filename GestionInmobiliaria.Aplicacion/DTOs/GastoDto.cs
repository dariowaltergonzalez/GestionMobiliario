namespace GestionInmobiliaria.Aplicacion.DTOs;

public class GastoDto
{
    public int Id { get; set; }
    public int PropiedadId { get; set; }
    public string PropiedadDireccion { get; set; } = string.Empty;
    public int? ContratoId { get; set; }
    public string? ContratoCodigo { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public string Responsable { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaResolucion { get; set; }
    public string? MedioCobro { get; set; }
    public DateTime? FechaCobro { get; set; }
    public string? ReferenciaCobro { get; set; }
    public string? ChequeBanco { get; set; }
    public string? ChequeNumero { get; set; }
    public DateTime? ChequeFechaVencimiento { get; set; }
    public string? ObservacionesResolucion { get; set; }
    public int? LiquidacionId { get; set; }
    public bool VisibleParaInquilino { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class ResolverGastoRequest
{
    public int Medio { get; set; }
    public DateTime? Fecha { get; set; }
    public string? ReferenciaCobro { get; set; }
    public string? ChequeBanco { get; set; }
    public string? ChequeNumero { get; set; }
    public DateTime? ChequeFechaVencimiento { get; set; }
    public string? Observaciones { get; set; }
}

public class CreateGastoRequest
{
    public int PropiedadId { get; set; }
    public int? ContratoId { get; set; }
    public int Categoria { get; set; }
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public int Responsable { get; set; }
    public bool VisibleParaInquilino { get; set; } = true;
}

public class UpdateGastoRequest
{
    public int PropiedadId { get; set; }
    public int? ContratoId { get; set; }
    public int Categoria { get; set; }
    public string? Descripcion { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public int Responsable { get; set; }
    public bool VisibleParaInquilino { get; set; }
}

public class CategoriaGastoDto
{
    public int Valor { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
