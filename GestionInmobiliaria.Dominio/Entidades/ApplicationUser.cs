using Microsoft.AspNetCore.Identity;

namespace GestionInmobiliaria.Dominio.Entidades;

public class ApplicationUser : IdentityUser
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
