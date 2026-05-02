using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IPropietarioRepository
{
    Task<IEnumerable<Propietario>> GetAllAsync(string? buscar = null, bool? activo = null);
    Task<Propietario?> GetByIdAsync(int id);
    Task<Propietario> CreateAsync(Propietario propietario);
    Task<Propietario> UpdateAsync(Propietario propietario);
    Task<bool> DeleteAsync(int id);
}
