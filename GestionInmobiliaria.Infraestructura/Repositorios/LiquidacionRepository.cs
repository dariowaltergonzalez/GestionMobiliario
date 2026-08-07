using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class LiquidacionRepository : ILiquidacionRepository
{
    private readonly ApplicationDbContext _context;

    public LiquidacionRepository(ApplicationDbContext context) => _context = context;

    private IQueryable<Liquidacion> QueryConIncludes() => _context.Liquidaciones
        .Include(l => l.Pago)
            .ThenInclude(p => p.Contrato)
                .ThenInclude(c => c.Propiedad)
        .AsQueryable();

    public async Task<PagedResult<Liquidacion>> GetPagedAsync(
        PaginationParams paginacion, EstadoLiquidacion? estado = null, int? propietarioId = null, string? buscar = null)
    {
        var query = QueryConIncludes();

        if (estado.HasValue)
            query = query.Where(l => l.Estado == estado.Value);

        if (propietarioId.HasValue)
            query = query.Where(l => l.Pago.Contrato.PropietarioRefId == propietarioId.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(l =>
                l.Pago.Contrato.Codigo.Contains(buscar) ||
                l.Pago.Contrato.LocadorNombre.Contains(buscar) ||
                l.Pago.Contrato.LocadorApellido.Contains(buscar) ||
                l.Pago.Contrato.Propiedad.Direccion.Contains(buscar));

        query = query.OrderByDescending(l => l.FechaCreacion);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<Liquidacion?> GetByIdAsync(int id) =>
        await QueryConIncludes().FirstOrDefaultAsync(l => l.Id == id);

    public async Task<Liquidacion?> GetByPagoIdAsync(int pagoId) =>
        await _context.Liquidaciones.FirstOrDefaultAsync(l => l.PagoId == pagoId);

    public async Task<Liquidacion> CreateAsync(Liquidacion liquidacion)
    {
        liquidacion.FechaCreacion = DateTime.UtcNow;
        liquidacion.FechaActualizacion = DateTime.UtcNow;
        _context.Liquidaciones.Add(liquidacion);
        await _context.SaveChangesAsync();
        return liquidacion;
    }

    public async Task<Liquidacion?> MarcarLiquidadaAsync(int id, DateTime fecha, string? observaciones)
    {
        var liquidacion = await _context.Liquidaciones.FirstOrDefaultAsync(l => l.Id == id);
        if (liquidacion is null) return null;

        liquidacion.Estado = EstadoLiquidacion.Liquidado;
        liquidacion.FechaLiquidacion = fecha;
        liquidacion.Observaciones = observaciones;
        liquidacion.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return liquidacion;
    }
}
