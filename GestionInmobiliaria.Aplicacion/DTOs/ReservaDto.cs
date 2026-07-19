namespace GestionInmobiliaria.Aplicacion.DTOs;

public class ReservaDto
{
    public int Id { get; set; }

    public int PropiedadId { get; set; }
    public string PropiedadDireccion { get; set; } = string.Empty;

    public int? LeadId { get; set; }
    public int? AgenteId { get; set; }
    public string? AgenteNombre { get; set; }

    public string CompradorNombre { get; set; } = string.Empty;
    public string CompradorApellido { get; set; } = string.Empty;
    public string? CompradorDni { get; set; }
    public string? CompradorTelefono { get; set; }
    public string? CompradorEmail { get; set; }

    public string VendedorNombre { get; set; } = string.Empty;
    public string VendedorApellido { get; set; } = string.Empty;
    public string? VendedorDni { get; set; }
    public string? VendedorTelefono { get; set; }
    public string? VendedorEmail { get; set; }

    public decimal MontoSenia { get; set; }
    public decimal? PrecioTotal { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string MedioDeposito { get; set; } = string.Empty;

    public decimal? ComisionVendedorPorcentaje { get; set; }
    public decimal? ComisionVendedorMonto { get; set; }
    public decimal? ComisionCompradorPorcentaje { get; set; }
    public decimal? ComisionCompradorMonto { get; set; }

    public DateTime FechaReserva { get; set; }
    public DateTime FechaVencimiento { get; set; }

    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class CreateReservaRequest
{
    public int PropiedadId { get; set; }
    public int? LeadId { get; set; }
    public int? AgenteId { get; set; }

    public string CompradorNombre { get; set; } = string.Empty;
    public string CompradorApellido { get; set; } = string.Empty;
    public string? CompradorDni { get; set; }
    public string? CompradorTelefono { get; set; }
    public string? CompradorEmail { get; set; }

    public string VendedorNombre { get; set; } = string.Empty;
    public string VendedorApellido { get; set; } = string.Empty;
    public string? VendedorDni { get; set; }
    public string? VendedorTelefono { get; set; }
    public string? VendedorEmail { get; set; }

    public decimal MontoSenia { get; set; }
    public decimal? PrecioTotal { get; set; }
    public int Moneda { get; set; } = 2; // USD por defecto
    public int MedioDeposito { get; set; } = 1;

    public decimal? ComisionVendedorPorcentaje { get; set; }
    public decimal? ComisionVendedorMonto { get; set; }
    public decimal? ComisionCompradorPorcentaje { get; set; }
    public decimal? ComisionCompradorMonto { get; set; }

    public DateTime FechaReserva { get; set; }
    public DateTime FechaVencimiento { get; set; }

    public string? Observaciones { get; set; }
}

public class UpdateReservaRequest : CreateReservaRequest
{
    public int Estado { get; set; } = 1;
}
