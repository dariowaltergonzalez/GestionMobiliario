namespace GestionInmobiliaria.Aplicacion.DTOs;

public class ConfiguracionEmpresaDto
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string? Cuit { get; set; }
    public string? CondicionFiscal { get; set; }
    public string? LogoUrl { get; set; }
    public string? Slogan { get; set; }
    public string? Telefono { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? SitioWeb { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? Provincia { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Pais { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public string? Twitter { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class ConfiguracionPublicaDto
{
    public string NombreComercial { get; set; } = string.Empty;
    public string? Slogan { get; set; }
    public string? Telefono { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? SitioWeb { get; set; }
    public string? LogoUrl { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public string? Twitter { get; set; }
    public string? TenantSlug { get; set; }
}

public class UpdateConfiguracionEmpresaRequest
{
    public string NombreComercial { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string? Cuit { get; set; }
    public string? CondicionFiscal { get; set; }
    public string? LogoUrl { get; set; }
    public string? Slogan { get; set; }
    public string? Telefono { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? SitioWeb { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? Provincia { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Pais { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public string? Twitter { get; set; }
}
