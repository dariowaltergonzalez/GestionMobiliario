using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class LeadRepository : ILeadRepository
{
    private readonly ApplicationDbContext _context;

    public LeadRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<Lead>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, EstadoLead? estado = null, int? agenteId = null)
    {
        var query = _context.Leads
            .Include(l => l.Agente).ThenInclude(a => a!.User)
            .Include(l => l.Propiedad)
            .Where(l => l.Activo)
            .AsQueryable();

        if (estado.HasValue)
            query = query.Where(l => l.Estado == estado.Value);

        if (agenteId.HasValue)
            query = query.Where(l => l.AgenteId == agenteId.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(l => l.Nombre.Contains(buscar) ||
                                     l.Apellido.Contains(buscar) ||
                                     (l.Email != null && l.Email.Contains(buscar)) ||
                                     (l.Telefono != null && l.Telefono.Contains(buscar)));

        query = query.OrderByDescending(l => l.FechaCreacion);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<IEnumerable<Lead>> GetActivosAsync() =>
        await _context.Leads
            .Where(l => l.Activo && l.Estado != EstadoLead.Descartado && l.Estado != EstadoLead.Convertido)
            .OrderByDescending(l => l.FechaCreacion)
            .ToListAsync();

    public async Task<Lead?> GetByIdAsync(int id) =>
        await _context.Leads
            .Include(l => l.Agente).ThenInclude(a => a!.User)
            .Include(l => l.Propiedad)
            .Include(l => l.Eventos)
            .FirstOrDefaultAsync(l => l.Id == id && l.Activo);

    public async Task<Lead> CreateAsync(Lead lead)
    {
        lead.FechaCreacion = DateTime.UtcNow;
        lead.FechaActualizacion = DateTime.UtcNow;
        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();
        return lead;
    }

    public async Task<Lead> UpdateAsync(Lead lead)
    {
        lead.FechaActualizacion = DateTime.UtcNow;
        _context.Leads.Update(lead);
        await _context.SaveChangesAsync();
        return lead;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead is null) return false;
        lead.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
