namespace GestionInmobiliaria.Dominio.Entidades;

public enum EstadoReserva
{
    Vigente = 1,
    Vencida = 2,
    Cancelada = 3,
    Convertida = 4
}

public enum Moneda
{
    ARS = 1,
    USD = 2
}

public enum MedioDeposito
{
    Efectivo = 1,
    Transferencia = 2,
    CuentaInmobiliaria = 3,
    Escribania = 4
}

public class Reserva : IAuditable
{
    public int Id { get; set; }

    public int PropiedadId { get; set; }
    public Propiedad Propiedad { get; set; } = null!;

    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public int? AgenteId { get; set; }
    public Agente? Agente { get; set; }

    // Datos del comprador (snapshot editable)
    public string CompradorNombre { get; set; } = string.Empty;
    public string CompradorApellido { get; set; } = string.Empty;
    public string? CompradorDni { get; set; }
    public string? CompradorTelefono { get; set; }
    public string? CompradorEmail { get; set; }

    // Datos del vendedor (snapshot editable, pre-poblado del propietario)
    public string VendedorNombre { get; set; } = string.Empty;
    public string VendedorApellido { get; set; } = string.Empty;
    public string? VendedorDni { get; set; }
    public string? VendedorTelefono { get; set; }
    public string? VendedorEmail { get; set; }

    // Condiciones económicas
    public decimal MontoSenia { get; set; }
    public decimal? PrecioTotal { get; set; }
    public Moneda Moneda { get; set; } = Moneda.USD;
    public MedioDeposito MedioDeposito { get; set; } = MedioDeposito.Efectivo;

    // Vigencia
    public DateTime FechaReserva { get; set; }
    public DateTime FechaVencimiento { get; set; }

    public EstadoReserva Estado { get; set; } = EstadoReserva.Vigente;
    public string? Observaciones { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }
}
