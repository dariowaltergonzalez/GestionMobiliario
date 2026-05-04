using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class InquilinoRepository : IInquilinoRepository
{
    private readonly ApplicationDbContext _context;

    public InquilinoRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<Inquilino>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, bool? activo = null)
    {
        var query = _context.Inquilinos.AsQueryable();

        if (activo.HasValue)
            query = query.Where(i => i.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(i => i.Nombre.Contains(buscar) || i.Apellido.Contains(buscar) ||
                                     (i.Dni != null && i.Dni.Contains(buscar)) ||
                                     (i.Email != null && i.Email.Contains(buscar)));

        query = query.OrderBy(i => i.Apellido).ThenBy(i => i.Nombre);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<IEnumerable<Inquilino>> GetActivosAsync() =>
        await _context.Inquilinos
            .Where(i => i.Activo)
            .OrderBy(i => i.Apellido).ThenBy(i => i.Nombre)
            .ToListAsync();

    public async Task<Inquilino?> GetByIdAsync(int id) =>
        await _context.Inquilinos.FirstOrDefaultAsync(i => i.Id == id);

    public async Task<Inquilino> CreateAsync(Inquilino inquilino)
    {
        inquilino.FechaCreacion = DateTime.UtcNow;
        inquilino.FechaActualizacion = DateTime.UtcNow;
        _context.Inquilinos.Add(inquilino);
        await _context.SaveChangesAsync();
        return inquilino;
    }

    public async Task<Inquilino> UpdateAsync(Inquilino inquilino)
    {
        inquilino.FechaActualizacion = DateTime.UtcNow;
        _context.Inquilinos.Update(inquilino);
        await _context.SaveChangesAsync();
        return inquilino;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var inquilino = await _context.Inquilinos.FindAsync(id);
        if (inquilino is null) return false;
        inquilino.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
