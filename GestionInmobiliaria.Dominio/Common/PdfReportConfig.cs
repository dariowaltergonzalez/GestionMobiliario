namespace GestionInmobiliaria.Dominio.Common;

public class PdfReportConfig
{
    public string Titulo { get; set; } = "Reporte";
    public string NombreEmpresa { get; set; } = string.Empty;
    public string? Slogan { get; set; }
    public string? InfoEmpresa { get; set; }
    public byte[]? LogoBytes { get; set; }
    public string? FiltrosAplicados { get; set; }
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;
    public bool Landscape { get; set; } = false;
}
