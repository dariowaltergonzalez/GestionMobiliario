using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IPropietarioRepository
{
    Task<PagedResult<Propietario>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, bool? activo = null);
    Task<IEnumerable<Propietario>> GetActivosAsync();
    Task<Propietario?> GetByIdAsync(int id);
    Task<Propietario> CreateAsync(Propietario propietario);
    Task<Propietario> UpdateAsync(Propietario propietario);
    Task<bool> DeleteAsync(int id);
}
