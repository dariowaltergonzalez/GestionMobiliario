namespace GestionInmobiliaria.Aplicacion.DTOs;

public class PropietarioDto
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
    public string? Banco { get; set; }
    public string? CBU { get; set; }
    public string? Notas { get; set; }
    public Dictionary<string, bool> Notificaciones { get; set; } = new();
    public Dictionary<string, bool> NotificacionesWhatsApp { get; set; } = new();
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int CantidadPropiedades { get; set; }
}

public class CreatePropietarioRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Cuit { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string? Direccion { get; set; }
    public string? Banco { get; set; }
    public string? CBU { get; set; }
    public string? Notas { get; set; }
    public Dictionary<string, bool>? Notificaciones { get; set; }
    public Dictionary<string, bool>? NotificacionesWhatsApp { get; set; }
}

public class UpdatePropietarioRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Cuit { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string? Direccion { get; set; }
    public string? Banco { get; set; }
    public string? CBU { get; set; }
    public string? Notas { get; set; }
    public Dictionary<string, bool>? Notificaciones { get; set; }
    public Dictionary<string, bool>? NotificacionesWhatsApp { get; set; }
    public bool Activo { get; set; }
}

public class PropietarioComboDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}
