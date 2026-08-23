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
        .Include(l => l.Abonos.Where(a => a.Activo).OrderByDescending(a => a.Fecha))
        .Include(l => l.Gastos)
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

    public async Task<bool> EliminarAsync(int id)
    {
        var liquidacion = await _context.Liquidaciones.FirstOrDefaultAsync(l => l.Id == id);
        if (liquidacion is null) return false;

        var tieneAbonos = await _context.LiquidacionAbonos.AnyAsync(a => a.LiquidacionId == id && a.Activo);
        if (tieneAbonos) return false;

        liquidacion.Activo = false;
        liquidacion.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Liquidacion?> AgregarAbonoAsync(int liquidacionId, LiquidacionAbono abono)
    {
        var liquidacion = await _context.Liquidaciones.FirstOrDefaultAsync(l => l.Id == liquidacionId);
        if (liquidacion is null) return null;

        abono.LiquidacionId = liquidacionId;
        abono.FechaCreacion = DateTime.UtcNow;
        abono.FechaActualizacion = DateTime.UtcNow;
        abono.TenantId = liquidacion.TenantId;
        _context.LiquidacionAbonos.Add(abono);

        await AplicarEstadoAsync(liquidacion, liquidacionId, excluirAbonoId: null, montoIncluido: abono.Monto, fechaIncluida: abono.Fecha);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(liquidacionId);
    }

    public async Task<Liquidacion?> ActualizarAbonoAsync(int liquidacionId, int abonoId, LiquidacionAbono datos)
    {
        var liquidacion = await _context.Liquidaciones.FirstOrDefaultAsync(l => l.Id == liquidacionId);
        var abono = await _context.LiquidacionAbonos
            .FirstOrDefaultAsync(a => a.Id == abonoId && a.LiquidacionId == liquidacionId && a.Activo);
        if (liquidacion is null || abono is null) return null;

        abono.Monto = datos.Monto;
        abono.Fecha = datos.Fecha;
        abono.Medio = datos.Medio;
        abono.CbuCvuDestino = datos.CbuCvuDestino;
        abono.EntidadDestino = datos.EntidadDestino;
        abono.NumeroOperacion = datos.NumeroOperacion;
        abono.Observaciones = datos.Observaciones;
        abono.FechaActualizacion = DateTime.UtcNow;

        await AplicarEstadoAsync(liquidacion, liquidacionId, excluirAbonoId: abonoId, montoIncluido: datos.Monto, fechaIncluida: datos.Fecha);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(liquidacionId);
    }

    public async Task<Liquidacion?> EliminarAbonoAsync(int liquidacionId, int abonoId)
    {
        var liquidacion = await _context.Liquidaciones.FirstOrDefaultAsync(l => l.Id == liquidacionId);
        var abono = await _context.LiquidacionAbonos
            .FirstOrDefaultAsync(a => a.Id == abonoId && a.LiquidacionId == liquidacionId && a.Activo);
        if (liquidacion is null || abono is null) return null;

        abono.Activo = false;
        abono.FechaActualizacion = DateTime.UtcNow;

        await AplicarEstadoAsync(liquidacion, liquidacionId, excluirAbonoId: abonoId, montoIncluido: 0, fechaIncluida: null);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(liquidacionId);
    }

    /// <summary>
    /// Recalcula Estado/FechaLiquidacion consultando directo contra LiquidacionAbonos (no contra una
    /// colección ya cargada por Include), para no depender de que el filtro global de "solo activos"
    /// se propague correctamente a través de la navegación. <paramref name="excluirAbonoId"/> saca de
    /// la suma al abono que se está por agregar/editar/borrar; <paramref name="montoIncluido"/> y
    /// <paramref name="fechaIncluida"/> son su valor nuevo (0/null si se está eliminando).
    /// </summary>
    private async Task AplicarEstadoAsync(
        Liquidacion liquidacion, int liquidacionId, int? excluirAbonoId, decimal montoIncluido, DateTime? fechaIncluida)
    {
        var otros = _context.LiquidacionAbonos.Where(a => a.LiquidacionId == liquidacionId && a.Activo);
        if (excluirAbonoId.HasValue)
            otros = otros.Where(a => a.Id != excluirAbonoId.Value);

        var sumaOtros = await otros.SumAsync(a => (decimal?)a.Monto) ?? 0;
        var fechaMaxOtros = await otros.MaxAsync(a => (DateTime?)a.Fecha);

        var sumaTotal = sumaOtros + montoIncluido;
        var fechaMax = fechaIncluida.HasValue && (!fechaMaxOtros.HasValue || fechaIncluida.Value > fechaMaxOtros.Value)
            ? fechaIncluida.Value
            : fechaMaxOtros;

        if (sumaTotal <= 0)
        {
            liquidacion.Estado = EstadoLiquidacion.Pendiente;
            liquidacion.FechaLiquidacion = null;
        }
        else if (sumaTotal >= liquidacion.MontoALiquidar)
        {
            liquidacion.Estado = EstadoLiquidacion.Liquidado;
            liquidacion.FechaLiquidacion = fechaMax;
        }
        else
        {
            liquidacion.Estado = EstadoLiquidacion.Parcial;
            liquidacion.FechaLiquidacion = null;
        }

        liquidacion.FechaActualizacion = DateTime.UtcNow;
    }
}
