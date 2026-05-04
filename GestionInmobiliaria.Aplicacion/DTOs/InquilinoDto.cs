namespace GestionInmobiliaria.Aplicacion.DTOs;

public class InquilinoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombre} {Apellido}";
    public string? Dni { get; set; }
    public string? Cuit { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string? Direccion { get; set; }
    public string? Ocupacion { get; set; }
    public string? NombreGarante { get; set; }
    public string? TelefonoGarante { get; set; }
    public string? DniGarante { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateInquilinoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Cuit { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string? Direccion { get; set; }
    public string? Ocupacion { get; set; }
    public string? NombreGarante { get; set; }
    public string? TelefonoGarante { get; set; }
    public string? DniGarante { get; set; }
    public string? Notas { get; set; }
}

public class UpdateInquilinoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Cuit { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string? Direccion { get; set; }
    public string? Ocupacion { get; set; }
    public string? NombreGarante { get; set; }
    public string? TelefonoGarante { get; set; }
    public string? DniGarante { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; }
}

public class InquilinoComboDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}
