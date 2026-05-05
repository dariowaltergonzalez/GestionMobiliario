using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IEventoAgendaRepository
{
    Task<PagedResult<EventoAgenda>> GetPagedAsync(PaginationParams paginacion, int? agenteId = null, EstadoEvento? estado = null, TipoEvento? tipo = null);
    Task<IEnumerable<EventoAgenda>> GetByAgenteAsync(int agenteId);
    Task<EventoAgenda?> GetByIdAsync(int id);
    Task<EventoAgenda> CreateAsync(EventoAgenda evento);
    Task<EventoAgenda> UpdateAsync(EventoAgenda evento);
    Task<bool> DeleteAsync(int id);
}
