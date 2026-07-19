namespace GestionInmobiliaria.Dominio.Entidades;

public class ClausulaContrato : IAuditable
{
    public int Id { get; set; }
    public int Orden { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }
}
