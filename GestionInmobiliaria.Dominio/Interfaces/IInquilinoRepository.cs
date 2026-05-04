using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IInquilinoRepository
{
    Task<PagedResult<Inquilino>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, bool? activo = null);
    Task<IEnumerable<Inquilino>> GetActivosAsync();
    Task<Inquilino?> GetByIdAsync(int id);
    Task<Inquilino> CreateAsync(Inquilino inquilino);
    Task<Inquilino> UpdateAsync(Inquilino inquilino);
    Task<bool> DeleteAsync(int id);
}
