using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class EventoAgendaRepository : IEventoAgendaRepository
{
    private readonly ApplicationDbContext _context;

    public EventoAgendaRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<EventoAgenda>> GetPagedAsync(PaginationParams paginacion, int? agenteId = null, EstadoEvento? estado = null, TipoEvento? tipo = null)
    {
        var query = _context.EventosAgenda
            .Include(e => e.Agente).ThenInclude(a => a.User)
            .Include(e => e.Propiedad)
            .Include(e => e.Lead)
            .Include(e => e.Inquilino)
            .Where(e => e.Activo)
            .AsQueryable();

        if (agenteId.HasValue)
            query = query.Where(e => e.AgenteId == agenteId.Value);

        if (estado.HasValue)
            query = query.Where(e => e.Estado == estado.Value);

        if (tipo.HasValue)
            query = query.Where(e => e.Tipo == tipo.Value);

        query = query.OrderBy(e => e.FechaHora);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<IEnumerable<EventoAgenda>> GetByAgenteAsync(int agenteId) =>
        await _context.EventosAgenda
            .Include(e => e.Propiedad)
            .Include(e => e.Lead)
            .Include(e => e.Inquilino)
            .Where(e => e.Activo && e.AgenteId == agenteId && e.Estado == EstadoEvento.Pendiente)
            .OrderBy(e => e.FechaHora)
            .ToListAsync();

    public async Task<EventoAgenda?> GetByIdAsync(int id) =>
        await _context.EventosAgenda
            .Include(e => e.Agente).ThenInclude(a => a.User)
            .Include(e => e.Propiedad)
            .Include(e => e.Lead)
            .Include(e => e.Inquilino)
            .FirstOrDefaultAsync(e => e.Id == id && e.Activo);

    public async Task<EventoAgenda> CreateAsync(EventoAgenda evento)
    {
        evento.FechaCreacion = DateTime.UtcNow;
        evento.FechaActualizacion = DateTime.UtcNow;
        _context.EventosAgenda.Add(evento);
        await _context.SaveChangesAsync();
        return evento;
    }

    public async Task<EventoAgenda> UpdateAsync(EventoAgenda evento)
    {
        evento.FechaActualizacion = DateTime.UtcNow;
        _context.EventosAgenda.Update(evento);
        await _context.SaveChangesAsync();
        return evento;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var evento = await _context.EventosAgenda.FindAsync(id);
        if (evento is null) return false;
        evento.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
