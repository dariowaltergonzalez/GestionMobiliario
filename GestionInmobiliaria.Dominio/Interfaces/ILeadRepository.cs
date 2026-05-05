using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface ILeadRepository
{
    Task<PagedResult<Lead>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, EstadoLead? estado = null, int? agenteId = null);
    Task<IEnumerable<Lead>> GetActivosAsync();
    Task<Lead?> GetByIdAsync(int id);
    Task<Lead> CreateAsync(Lead lead);
    Task<Lead> UpdateAsync(Lead lead);
    Task<bool> DeleteAsync(int id);
}
