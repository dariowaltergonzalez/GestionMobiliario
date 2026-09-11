using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IPropiedadRepository
{
    Task<PagedResult<Propiedad>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, TipoPropiedad? tipo = null, EstadoPropiedad? estado = null, int? propietarioId = null, TipoOperacion? operacion = null);
    Task<IEnumerable<Propiedad>> GetDisponiblesAsync();
    Task<IEnumerable<Propiedad>> GetPublicasAsync();
    /// <summary>
    /// Búsqueda usada por el bot de WhatsApp — corre fuera de un request HTTP (sin tenant activo vía
    /// ITenantService), por eso recibe el <paramref name="tenantId"/> explícito, mismo patrón que
    /// RecordatorioVencimientoService/AjusteAutomaticoService.
    /// </summary>
    Task<IEnumerable<Propiedad>> BuscarBotAsync(
        int tenantId, string? barrio, string? ciudad, TipoOperacion? operacion, TipoPropiedad? tipo,
        int? dormitoriosMinimo, bool? cochera, bool? pileta, bool? mascotas, int limite = 5);
    Task<Propiedad?> GetPublicaByIdAsync(int id);
    Task<Propiedad?> GetByIdAsync(int id);
    Task<Propiedad> CreateAsync(Propiedad propiedad);
    Task<Propiedad> UpdateAsync(Propiedad propiedad);
    Task<bool> DeleteAsync(int id);
    Task<FotoPropiedad> AddFotoAsync(FotoPropiedad foto);
    Task<FotoPropiedad?> GetFotoAsync(int propiedadId, int fotoId);
    Task SetFotoPrincipalAsync(int propiedadId, int fotoId);
    Task<bool> DeleteFotoAsync(int propiedadId, int fotoId);
    Task SetVideoUrlAsync(int propiedadId, string? url);
}
