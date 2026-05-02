namespace GestionInmobiliaria.Dominio.Entidades;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Expiracion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaRevocacion { get; set; }
    public bool EstaRevocado => FechaRevocacion.HasValue;
    public bool EstaExpirado => DateTime.UtcNow >= Expiracion;
    public bool EsValido => !EstaRevocado && !EstaExpirado;
}
