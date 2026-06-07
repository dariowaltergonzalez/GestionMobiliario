using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IAuditLogRepository
{
    Task<PagedResult<AuditLog>> GetPagedAsync(PaginationParams paginacion, string? accion = null, string? entidad = null);
    Task<IEnumerable<string>> GetEntidadesAsync();
}
