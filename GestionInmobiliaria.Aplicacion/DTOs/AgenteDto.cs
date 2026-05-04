namespace GestionInmobiliaria.Aplicacion.DTOs;

public class AgenteDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto => $"{Apellido}, {Nombre}";
    public string Email { get; set; } = string.Empty;
    public string? Zona { get; set; }
    public string? TelefonoInterno { get; set; }
    public decimal ComisionPorcentaje { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int CantidadPropiedades { get; set; }
    public int CantidadInquilinos { get; set; }
}

public class CreateAgenteRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Zona { get; set; }
    public string? TelefonoInterno { get; set; }
    public decimal ComisionPorcentaje { get; set; } = 0;
    public string? Notas { get; set; }
}

public class UpdateAgenteRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Zona { get; set; }
    public string? TelefonoInterno { get; set; }
    public decimal ComisionPorcentaje { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; }
}

public class AgenteComboDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}
