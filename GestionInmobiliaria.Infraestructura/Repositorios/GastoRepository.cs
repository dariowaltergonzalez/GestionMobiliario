using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class GastoRepository : IGastoRepository
{
    private readonly ApplicationDbContext _context;

    public GastoRepository(ApplicationDbContext context) => _context = context;

    private IQueryable<Gasto> QueryConIncludes() => _context.Gastos
        .Include(g => g.Propiedad)
        .Include(g => g.Contrato)
        .AsQueryable();

    public async Task<PagedResult<Gasto>> GetPagedAsync(
        PaginationParams paginacion,
        int? propiedadId = null,
        int? contratoId = null,
        ResponsableGasto? responsable = null,
        EstadoGasto? estado = null,
        CategoriaGasto? categoria = null,
        string? buscar = null)
    {
        var query = QueryConIncludes();

        if (propiedadId.HasValue)
            query = query.Where(g => g.PropiedadId == propiedadId.Value);

        if (contratoId.HasValue)
            query = query.Where(g => g.ContratoId == contratoId.Value);

        if (responsable.HasValue)
            query = query.Where(g => g.Responsable == responsable.Value);

        if (estado.HasValue)
            query = query.Where(g => g.Estado == estado.Value);

        if (categoria.HasValue)
            query = query.Where(g => g.Categoria == categoria.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(g =>
                g.Propiedad.Direccion.Contains(buscar) ||
                (g.Descripcion != null && g.Descripcion.Contains(buscar)));

        query = query.OrderByDescending(g => g.Fecha);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<Gasto?> GetByIdAsync(int id) =>
        await QueryConIncludes().FirstOrDefaultAsync(g => g.Id == id);

    public async Task<Gasto> CreateAsync(Gasto gasto)
    {
        gasto.FechaCreacion = DateTime.UtcNow;
        gasto.FechaActualizacion = DateTime.UtcNow;
        _context.Gastos.Add(gasto);
        await _context.SaveChangesAsync();
        return gasto;
    }

    public async Task<Gasto?> UpdateAsync(int id, Gasto datos)
    {
        var gasto = await _context.Gastos.FirstOrDefaultAsync(g => g.Id == id);
        if (gasto is null || gasto.Estado != EstadoGasto.Pendiente) return null;

        gasto.PropiedadId = datos.PropiedadId;
        gasto.ContratoId = datos.ContratoId;
        gasto.Categoria = datos.Categoria;
        gasto.Descripcion = datos.Descripcion;
        gasto.Monto = datos.Monto;
        gasto.Fecha = datos.Fecha;
        gasto.Responsable = datos.Responsable;
        gasto.VisibleParaInquilino = datos.VisibleParaInquilino;
        gasto.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return gasto;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var gasto = await _context.Gastos.FirstOrDefaultAsync(g => g.Id == id);
        if (gasto is null || gasto.Estado != EstadoGasto.Pendiente) return false;

        gasto.Activo = false;
        gasto.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Gasto?> MarcarResueltoAsync(int id, MedioPago medio, DateTime? fecha, string? referenciaCobro,
        string? chequeBanco, string? chequeNumero, DateTime? chequeFechaVencimiento, string? observaciones)
    {
        var gasto = await _context.Gastos.FirstOrDefaultAsync(g => g.Id == id);
        if (gasto is null || gasto.Estado != EstadoGasto.Pendiente) return null;

        gasto.Estado = EstadoGasto.Resuelto;
        gasto.FechaResolucion = DateTime.UtcNow;
        gasto.MedioCobro = medio;
        gasto.FechaCobro = fecha ?? DateTime.UtcNow;
        gasto.ReferenciaCobro = referenciaCobro;
        gasto.ChequeBanco = chequeBanco;
        gasto.ChequeNumero = chequeNumero;
        gasto.ChequeFechaVencimiento = chequeFechaVencimiento;
        gasto.ObservacionesResolucion = observaciones;
        gasto.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return gasto;
    }
}
