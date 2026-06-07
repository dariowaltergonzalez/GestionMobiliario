using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Aplicacion.DTOs;

public class AppLogDto
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public NivelLog Nivel { get; set; }
    public string NivelNombre => Nivel.ToString();
    public string Origen { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? Detalle { get; set; }
}
