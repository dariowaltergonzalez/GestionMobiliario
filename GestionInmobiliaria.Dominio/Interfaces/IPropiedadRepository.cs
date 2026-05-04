using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IPropiedadRepository
{
    Task<PagedResult<Propiedad>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, TipoPropiedad? tipo = null, EstadoPropiedad? estado = null, int? propietarioId = null);
    Task<IEnumerable<Propiedad>> GetDisponiblesAsync();
    Task<Propiedad?> GetByIdAsync(int id);
    Task<Propiedad> CreateAsync(Propiedad propiedad);
    Task<Propiedad> UpdateAsync(Propiedad propiedad);
    Task<bool> DeleteAsync(int id);
}
