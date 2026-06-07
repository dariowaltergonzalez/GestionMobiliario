using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class AppLogRepository : IAppLogRepository
{
    private readonly ApplicationDbContext _context;

    public AppLogRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<AppLog>> GetPagedAsync(PaginationParams paginacion, NivelLog? nivel = null)
    {
        var query = _context.AppLogs.AsQueryable();

        if (nivel.HasValue)
            query = query.Where(l => l.Nivel == nivel.Value);

        query = query.OrderByDescending(l => l.Timestamp);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task RegistrarAsync(NivelLog nivel, string origen, string mensaje, string? detalle = null)
    {
        _context.AppLogs.Add(new AppLog
        {
            Timestamp = DateTime.UtcNow,
            Nivel = nivel,
            Origen = origen,
            Mensaje = mensaje,
            Detalle = detalle
        });
        await _context.SaveChangesAsync();
    }
}
