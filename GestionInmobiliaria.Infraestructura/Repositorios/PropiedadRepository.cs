using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class PropiedadRepository : IPropiedadRepository
{
    private readonly ApplicationDbContext _context;

    public PropiedadRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<Propiedad>> GetAllAsync(string? buscar = null, TipoPropiedad? tipo = null, EstadoPropiedad? estado = null, int? propietarioId = null)
    {
        var query = _context.Propiedades.Include(p => p.Propietario).Where(p => p.Activo).AsQueryable();

        if (tipo.HasValue)
            query = query.Where(p => p.Tipo == tipo.Value);

        if (estado.HasValue)
            query = query.Where(p => p.Estado == estado.Value);

        if (propietarioId.HasValue)
            query = query.Where(p => p.PropietarioId == propietarioId.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(p => p.Direccion.Contains(buscar) ||
                                     (p.Barrio != null && p.Barrio.Contains(buscar)) ||
                                     (p.Ciudad != null && p.Ciudad.Contains(buscar)));

        return await query.OrderBy(p => p.Direccion).ToListAsync();
    }

    public async Task<Propiedad?> GetByIdAsync(int id) =>
        await _context.Propiedades.Include(p => p.Propietario).FirstOrDefaultAsync(p => p.Id == id && p.Activo);

    public async Task<Propiedad> CreateAsync(Propiedad propiedad)
    {
        _context.Propiedades.Add(propiedad);
        await _context.SaveChangesAsync();
        return propiedad;
    }

    public async Task<Propiedad> UpdateAsync(Propiedad propiedad)
    {
        _context.Propiedades.Update(propiedad);
        await _context.SaveChangesAsync();
        return propiedad;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var propiedad = await _context.Propiedades.FindAsync(id);
        if (propiedad is null) return false;
        propiedad.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
