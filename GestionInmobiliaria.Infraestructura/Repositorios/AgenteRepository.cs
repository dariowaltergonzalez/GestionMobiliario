using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class AgenteRepository : IAgenteRepository
{
    private readonly ApplicationDbContext _context;

    public AgenteRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<Agente>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, bool? activo = null)
    {
        var query = _context.Agentes
            .Include(a => a.User)
            .Include(a => a.Propiedades)
            .Include(a => a.Inquilinos)
            .AsQueryable();

        if (activo.HasValue)
            query = query.Where(a => a.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(a => a.User.Nombre.Contains(buscar) ||
                                     a.User.Apellido.Contains(buscar) ||
                                     (a.User.Email != null && a.User.Email.Contains(buscar)) ||
                                     (a.Zona != null && a.Zona.Contains(buscar)));

        query = query.OrderBy(a => a.User.Apellido).ThenBy(a => a.User.Nombre);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<IEnumerable<Agente>> GetActivosAsync() =>
        await _context.Agentes
            .Include(a => a.User)
            .Where(a => a.Activo)
            .OrderBy(a => a.User.Apellido).ThenBy(a => a.User.Nombre)
            .ToListAsync();

    public async Task<Agente?> GetByIdAsync(int id) =>
        await _context.Agentes
            .Include(a => a.User)
            .Include(a => a.Propiedades)
            .Include(a => a.Inquilinos)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Agente?> GetByUserIdAsync(string userId) =>
        await _context.Agentes
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId);

    public async Task<Agente> CreateAsync(Agente agente)
    {
        agente.FechaCreacion = DateTime.UtcNow;
        agente.FechaActualizacion = DateTime.UtcNow;
        _context.Agentes.Add(agente);
        await _context.SaveChangesAsync();
        return agente;
    }

    public async Task<Agente> UpdateAsync(Agente agente)
    {
        agente.FechaActualizacion = DateTime.UtcNow;
        _context.Agentes.Update(agente);
        await _context.SaveChangesAsync();
        return agente;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var agente = await _context.Agentes.FindAsync(id);
        if (agente is null) return false;
        agente.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
