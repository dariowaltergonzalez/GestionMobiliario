using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Helpers;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class PropiedadRepository : IPropiedadRepository
{
    private readonly ApplicationDbContext _context;

    public PropiedadRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<Propiedad>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, TipoPropiedad? tipo = null, EstadoPropiedad? estado = null, int? propietarioId = null)
    {
        var query = _context.Propiedades
            .Include(p => p.Propietario)
            .Include(p => p.Fotos)
            .Where(p => p.Activo).AsQueryable();

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

        query = query.OrderBy(p => p.Direccion);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<IEnumerable<Propiedad>> GetDisponiblesAsync() =>
        await _context.Propiedades
            .Include(p => p.Propietario)
            .Where(p => p.Activo && p.Estado == EstadoPropiedad.Disponible)
            .OrderBy(p => p.Direccion)
            .ToListAsync();

    public async Task<IEnumerable<Propiedad>> GetPublicasAsync() =>
        await _context.Propiedades
            .Include(p => p.Fotos)
            .Where(p => p.Activo && p.Estado == EstadoPropiedad.Disponible)
            .OrderBy(p => p.Direccion)
            .ToListAsync();

    public async Task<Propiedad?> GetByIdAsync(int id) =>
        await _context.Propiedades
            .Include(p => p.Propietario)
            .Include(p => p.Fotos.OrderBy(f => f.Orden))
            .FirstOrDefaultAsync(p => p.Id == id && p.Activo);

    public async Task<Propiedad> CreateAsync(Propiedad propiedad)
    {
        Normalizar(propiedad);
        propiedad.FechaCreacion = DateTime.UtcNow;
        propiedad.FechaActualizacion = DateTime.UtcNow;
        _context.Propiedades.Add(propiedad);
        await _context.SaveChangesAsync();
        return propiedad;
    }

    public async Task<Propiedad> UpdateAsync(Propiedad propiedad)
    {
        var existing = await _context.Propiedades.FindAsync(propiedad.Id)
            ?? throw new KeyNotFoundException($"Propiedad {propiedad.Id} no encontrada.");

        Normalizar(propiedad);

        existing.Tipo = propiedad.Tipo;
        existing.Operacion = propiedad.Operacion;
        existing.Direccion = propiedad.Direccion;
        existing.Barrio = propiedad.Barrio;
        existing.Ciudad = propiedad.Ciudad;
        existing.Provincia = propiedad.Provincia;
        existing.Ambientes = propiedad.Ambientes;
        existing.Dormitorios = propiedad.Dormitorios;
        existing.Banios = propiedad.Banios;
        existing.SuperficieTotal = propiedad.SuperficieTotal;
        existing.SuperficieCubierta = propiedad.SuperficieCubierta;
        existing.Piso = propiedad.Piso;
        existing.NumeroDepartamento = propiedad.NumeroDepartamento;
        existing.PrecioAlquiler = propiedad.PrecioAlquiler;
        existing.PrecioVenta = propiedad.PrecioVenta;
        existing.Expensas = propiedad.Expensas;
        existing.Estado = propiedad.Estado;
        existing.EstadoConservacion = propiedad.EstadoConservacion;
        existing.Cochera = propiedad.Cochera;
        existing.Antiguedad = propiedad.Antiguedad;
        existing.TieneCalefaccion = propiedad.TieneCalefaccion;
        existing.AceptaMascotas = propiedad.AceptaMascotas;
        existing.NroCatastro = propiedad.NroCatastro;
        existing.Descripcion = propiedad.Descripcion;
        existing.Notas = propiedad.Notas;
        existing.PropietarioId = propiedad.PropietarioId;
        existing.AgenteId = propiedad.AgenteId;
        existing.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    private static void Normalizar(Propiedad p)
    {
        p.Direccion = TextNormalizer.TitleCase(p.Direccion)!;
        p.Barrio = TextNormalizer.TitleCase(p.Barrio);
        p.Ciudad = TextNormalizer.TitleCase(p.Ciudad);
        p.Provincia = TextNormalizer.TitleCase(p.Provincia);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var propiedad = await _context.Propiedades.FindAsync(id);
        if (propiedad is null) return false;
        propiedad.Activo = false;
        propiedad.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<FotoPropiedad> AddFotoAsync(FotoPropiedad foto)
    {
        foto.FechaCreacion = DateTime.UtcNow;
        _context.FotosPropiedad.Add(foto);
        await _context.SaveChangesAsync();
        return foto;
    }

    public async Task<FotoPropiedad?> GetFotoAsync(int propiedadId, int fotoId) =>
        await _context.FotosPropiedad
            .FirstOrDefaultAsync(f => f.Id == fotoId && f.PropiedadId == propiedadId);

    public async Task SetFotoPrincipalAsync(int propiedadId, int fotoId)
    {
        var fotos = await _context.FotosPropiedad
            .Where(f => f.PropiedadId == propiedadId)
            .ToListAsync();
        foreach (var f in fotos)
            f.EsPrincipal = f.Id == fotoId;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteFotoAsync(int propiedadId, int fotoId)
    {
        var foto = await _context.FotosPropiedad
            .FirstOrDefaultAsync(f => f.Id == fotoId && f.PropiedadId == propiedadId);
        if (foto is null) return false;
        _context.FotosPropiedad.Remove(foto);
        await _context.SaveChangesAsync();
        return true;
    }
}
