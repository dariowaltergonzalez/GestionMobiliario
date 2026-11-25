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
    private readonly IAgenteRepository _agentes;

    public LeadRepository(ApplicationDbContext context, IAgenteRepository agentes)
    {
        _context = context;
        _agentes = agentes;
    }

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

        if (!lead.AgenteId.HasValue)
            lead.AgenteId = await ResolverAgenteAutomaticoAsync();

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();
        return lead;
    }

    // Reparto automático cuando se crea un Lead sin agente ya determinado (ej. "Solicitar visita"
    // sobre una propiedad sin agente fijo, o el Lead que se genera solo desde una Tasación) — ver
    // docs/logica-negocio.md, sección LEADS. Si la propiedad ya tenía agente fijo, o el alta fue
    // manual desde el dashboard, esto ni se llama (AgenteId ya viene seteado desde antes).
    private async Task<int?> ResolverAgenteAutomaticoAsync()
    {
        var config = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();
        if (config is null || config.EstrategiaAsignacionLeads == EstrategiaAsignacionLeads.Manual)
            return null;

        var activos = (await _agentes.GetActivosAsync()).OrderBy(a => a.Id).ToList();
        if (activos.Count == 0) return null;

        if (config.EstrategiaAsignacionLeads == EstrategiaAsignacionLeads.PorCargaDeTrabajo)
        {
            var cargaPorAgente = await _context.Leads
                .Where(l => l.Activo && l.AgenteId != null
                            && l.Estado != EstadoLead.Convertido && l.Estado != EstadoLead.Descartado)
                .GroupBy(l => l.AgenteId!.Value)
                .Select(g => new { AgenteId = g.Key, Cantidad = g.Count() })
                .ToListAsync();
            var cargaPorId = cargaPorAgente.ToDictionary(x => x.AgenteId, x => x.Cantidad);

            return activos.OrderBy(a => cargaPorId.GetValueOrDefault(a.Id, 0)).ThenBy(a => a.Id).First().Id;
        }

        // RotacionSimple: le toca al siguiente agente activo después del último asignado (se guarda
        // el cursor en ConfiguracionEmpresa para que la rotación siga de donde quedó entre requests).
        var indiceAnterior = config.UltimoAgenteAsignadoLeadId.HasValue
            ? activos.FindIndex(a => a.Id == config.UltimoAgenteAsignadoLeadId.Value)
            : -1;
        var elegido = activos[(indiceAnterior + 1) % activos.Count];
        config.UltimoAgenteAsignadoLeadId = elegido.Id;
        await _context.SaveChangesAsync();
        return elegido.Id;
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
