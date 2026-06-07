namespace GestionInmobiliaria.Dominio.Entidades;

public class FotoPropiedad
{
    public int Id { get; set; }
    public int PropiedadId { get; set; }
    public Propiedad Propiedad { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }
    public int Orden { get; set; }
    public int TenantId { get; set; }
    public DateTime FechaCreacion { get; set; }
}
