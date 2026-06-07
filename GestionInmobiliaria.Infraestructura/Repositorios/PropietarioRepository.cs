using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Helpers;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class PropietarioRepository : IPropietarioRepository
{
    private readonly ApplicationDbContext _context;

    public PropietarioRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<Propietario>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, bool? activo = null)
    {
        var query = _context.Propietarios.Include(p => p.Propiedades).AsQueryable();

        if (activo.HasValue)
            query = query.Where(p => p.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(p => p.Nombre.Contains(buscar) || p.Apellido.Contains(buscar) ||
                                     (p.Dni != null && p.Dni.Contains(buscar)) ||
                                     (p.Email != null && p.Email.Contains(buscar)));

        query = query.OrderBy(p => p.Apellido).ThenBy(p => p.Nombre);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<IEnumerable<Propietario>> GetActivosAsync() =>
        await _context.Propietarios
            .Where(p => p.Activo)
            .OrderBy(p => p.Apellido).ThenBy(p => p.Nombre)
            .ToListAsync();

    public async Task<Propietario?> GetByIdAsync(int id) =>
        await _context.Propietarios.Include(p => p.Propiedades).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Propietario> CreateAsync(Propietario propietario)
    {
        Normalizar(propietario);
        propietario.FechaCreacion = DateTime.UtcNow;
        propietario.FechaActualizacion = DateTime.UtcNow;
        _context.Propietarios.Add(propietario);
        await _context.SaveChangesAsync();
        return propietario;
    }

    public async Task<Propietario> UpdateAsync(Propietario propietario)
    {
        Normalizar(propietario);
        propietario.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return propietario;
    }

    private static void Normalizar(Propietario p)
    {
        p.Nombre = TextNormalizer.TitleCase(p.Nombre)!;
        p.Apellido = TextNormalizer.TitleCase(p.Apellido)!;
        p.Direccion = TextNormalizer.TitleCase(p.Direccion);
        p.Email = TextNormalizer.Lower(p.Email);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var propietario = await _context.Propietarios.FindAsync(id);
        if (propietario is null) return false;
        propietario.Activo = false;
        propietario.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
