using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IPropiedadRepository
{
    Task<IEnumerable<Propiedad>> GetAllAsync(string? buscar = null, TipoPropiedad? tipo = null, EstadoPropiedad? estado = null, int? propietarioId = null);
    Task<Propiedad?> GetByIdAsync(int id);
    Task<Propiedad> CreateAsync(Propiedad propiedad);
    Task<Propiedad> UpdateAsync(Propiedad propiedad);
    Task<bool> DeleteAsync(int id);
}
