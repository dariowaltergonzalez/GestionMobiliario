using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class SolicitudTasacionRepository : ISolicitudTasacionRepository
{
    private readonly ApplicationDbContext _context;

    public SolicitudTasacionRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<SolicitudTasacion>> GetPagedAsync(
        PaginationParams paginacion,
        string? buscar = null,
        EstadoSolicitudTasacion? estado = null,
        int? agenteId = null)
    {
        var query = _context.SolicitudesTasacion
            .Include(s => s.Agente).ThenInclude(a => a!.User)
            .Include(s => s.Fotos)
            .AsQueryable();

        if (estado.HasValue)
            query = query.Where(s => s.Estado == estado.Value);

        if (agenteId.HasValue)
            query = query.Where(s => s.AgenteId == agenteId.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(s =>
                s.Nombre.Contains(buscar) ||
                s.Apellido.Contains(buscar) ||
                s.Direccion.Contains(buscar) ||
                (s.Email != null && s.Email.Contains(buscar)) ||
                (s.Telefono != null && s.Telefono.Contains(buscar)));

        query = query.OrderByDescending(s => s.FechaCreacion);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<SolicitudTasacion?> GetByIdAsync(int id) =>
        await _context.SolicitudesTasacion
            .Include(s => s.Agente).ThenInclude(a => a!.User)
            .Include(s => s.Fotos)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<SolicitudTasacion> CreateAsync(SolicitudTasacion solicitud)
    {
        solicitud.FechaCreacion = DateTime.UtcNow;
        solicitud.FechaActualizacion = DateTime.UtcNow;
        _context.SolicitudesTasacion.Add(solicitud);
        await _context.SaveChangesAsync();
        return solicitud;
    }

    public async Task<SolicitudTasacion> UpdateAsync(SolicitudTasacion solicitud)
    {
        solicitud.FechaActualizacion = DateTime.UtcNow;
        _context.SolicitudesTasacion.Update(solicitud);
        await _context.SaveChangesAsync();
        return solicitud;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var solicitud = await _context.SolicitudesTasacion.FindAsync(id);
        if (solicitud is null) return false;
        solicitud.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<FotoSolicitud> AddFotoAsync(FotoSolicitud foto)
    {
        foto.FechaSubida = DateTime.UtcNow;
        _context.FotosSolicitud.Add(foto);
        await _context.SaveChangesAsync();
        return foto;
    }

    public async Task<FotoSolicitud?> GetFotoAsync(int fotoId) =>
        await _context.FotosSolicitud.FindAsync(fotoId);

    public async Task<bool> DeleteFotoAsync(int fotoId)
    {
        var foto = await _context.FotosSolicitud.FindAsync(fotoId);
        if (foto is null) return false;
        _context.FotosSolicitud.Remove(foto);
        await _context.SaveChangesAsync();
        return true;
    }
}
