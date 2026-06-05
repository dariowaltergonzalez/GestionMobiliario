using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface ISolicitudTasacionRepository
{
    Task<PagedResult<SolicitudTasacion>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, EstadoSolicitudTasacion? estado = null, int? agenteId = null);
    Task<SolicitudTasacion?> GetByIdAsync(int id);
    Task<SolicitudTasacion> CreateAsync(SolicitudTasacion solicitud);
    Task<SolicitudTasacion> UpdateAsync(SolicitudTasacion solicitud);
    Task<bool> DeleteAsync(int id);
    Task<FotoSolicitud> AddFotoAsync(FotoSolicitud foto);
    Task<FotoSolicitud?> GetFotoAsync(int fotoId);
    Task<bool> DeleteFotoAsync(int fotoId);
}
