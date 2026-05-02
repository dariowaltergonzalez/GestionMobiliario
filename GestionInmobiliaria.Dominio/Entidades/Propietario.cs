namespace GestionInmobiliaria.Dominio.Entidades;

public class Propietario : IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Dni { get; set; }
    public string? Cuit { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string? Direccion { get; set; }
    public string? CBU { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }

    public ICollection<Propiedad> Propiedades { get; set; } = new List<Propiedad>();
}
