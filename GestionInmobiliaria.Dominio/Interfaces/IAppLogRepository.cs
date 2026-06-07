using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IAppLogRepository
{
    Task<PagedResult<AppLog>> GetPagedAsync(PaginationParams paginacion, NivelLog? nivel = null);
    Task RegistrarAsync(NivelLog nivel, string origen, string mensaje, string? detalle = null);
}
