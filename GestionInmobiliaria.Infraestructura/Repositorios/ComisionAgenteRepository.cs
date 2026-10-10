using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class ComisionAgenteRepository : IComisionAgenteRepository
{
    private readonly ApplicationDbContext _context;

    public ComisionAgenteRepository(ApplicationDbContext context) => _context = context;

    private IQueryable<ComisionAgente> QueryConIncludes() => _context.ComisionesAgentes
        .Include(c => c.Contrato)
            .ThenInclude(c => c.Propiedad)
        .Include(c => c.Agente)
            .ThenInclude(a => a.User)
        .AsQueryable();

    public async Task<PagedResult<ComisionAgente>> GetPagedAsync(
        PaginationParams paginacion, EstadoComisionAgente? estado = null, int? agenteId = null, string? buscar = null)
    {
        var query = QueryConIncludes();

        if (estado.HasValue)
            query = query.Where(c => c.Estado == estado.Value);

        if (agenteId.HasValue)
            query = query.Where(c => c.AgenteId == agenteId.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(c =>
                c.Contrato.Codigo.Contains(buscar) ||
                c.Contrato.Propiedad.Direccion.Contains(buscar) ||
                c.Agente.User.Nombre.Contains(buscar) ||
                c.Agente.User.Apellido.Contains(buscar));

        query = query.OrderByDescending(c => c.FechaCreacion);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<ComisionAgente?> GetByIdAsync(int id) =>
        await QueryConIncludes().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<ComisionAgente?> GetByContratoIdAsync(int contratoId) =>
        await _context.ComisionesAgentes.FirstOrDefaultAsync(c => c.ContratoId == contratoId);

    public async Task<ComisionAgente> CreateAsync(ComisionAgente comision)
    {
        comision.FechaCreacion = DateTime.UtcNow;
        comision.FechaActualizacion = DateTime.UtcNow;
        _context.ComisionesAgentes.Add(comision);
        await _context.SaveChangesAsync();
        return comision;
    }

    public async Task<ComisionAgente?> RegistrarPagoAsync(int id, DateTime fecha, MedioPago medio, string? observaciones)
    {
        var comision = await _context.ComisionesAgentes.FirstOrDefaultAsync(c => c.Id == id);
        if (comision is null) return null;

        comision.Estado = EstadoComisionAgente.Pagado;
        comision.FechaPago = fecha;
        comision.Medio = medio;
        comision.Observaciones = observaciones;
        comision.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<ComisionAgente?> ActualizarPagoAsync(int id, DateTime fecha, MedioPago medio, string? observaciones)
    {
        var comision = await _context.ComisionesAgentes.FirstOrDefaultAsync(c => c.Id == id && c.Estado == EstadoComisionAgente.Pagado);
        if (comision is null) return null;

        comision.FechaPago = fecha;
        comision.Medio = medio;
        comision.Observaciones = observaciones;
        comision.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<ComisionAgente?> DeshacerPagoAsync(int id)
    {
        var comision = await _context.ComisionesAgentes.FirstOrDefaultAsync(c => c.Id == id);
        if (comision is null) return null;

        comision.Estado = EstadoComisionAgente.Pendiente;
        comision.FechaPago = null;
        comision.Medio = null;
        comision.Observaciones = null;
        comision.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }
}
