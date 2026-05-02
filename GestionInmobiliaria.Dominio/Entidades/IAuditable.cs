namespace GestionInmobiliaria.Dominio.Entidades;

public interface IAuditable
{
    DateTime FechaCreacion { get; set; }
    DateTime FechaActualizacion { get; set; }
}
