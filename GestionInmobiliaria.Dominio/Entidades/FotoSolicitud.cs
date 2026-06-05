namespace GestionInmobiliaria.Dominio.Entidades;

public class FotoSolicitud
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? NombreArchivo { get; set; }
    public DateTime FechaSubida { get; set; }
    public int TenantId { get; set; }

    public int SolicitudTasacionId { get; set; }
    public SolicitudTasacion SolicitudTasacion { get; set; } = null!;
}
