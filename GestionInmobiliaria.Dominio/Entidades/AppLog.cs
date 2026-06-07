namespace GestionInmobiliaria.Dominio.Entidades;

public enum NivelLog
{
    Info = 1,
    Advertencia = 2,
    Error = 3
}

public class AppLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public NivelLog Nivel { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public int TenantId { get; set; }
}
