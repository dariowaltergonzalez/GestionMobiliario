using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IAgenteRepository
{
    Task<PagedResult<Agente>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, bool? activo = null);
    Task<IEnumerable<Agente>> GetActivosAsync();
    Task<Agente?> GetByIdAsync(int id);
    Task<Agente?> GetByUserIdAsync(string userId);
    Task<Agente> CreateAsync(Agente agente);
    Task<Agente> UpdateAsync(Agente agente);
    Task<bool> DeleteAsync(int id);
}
