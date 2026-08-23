using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IGastoRepository
{
    Task<PagedResult<Gasto>> GetPagedAsync(
        PaginationParams paginacion,
        int? propiedadId = null,
        int? contratoId = null,
        ResponsableGasto? responsable = null,
        EstadoGasto? estado = null,
        CategoriaGasto? categoria = null,
        string? buscar = null);
    Task<Gasto?> GetByIdAsync(int id);
    Task<Gasto> CreateAsync(Gasto gasto);
    Task<Gasto?> UpdateAsync(int id, Gasto datos);
    Task<bool> DeleteAsync(int id);
    Task<Gasto?> MarcarResueltoAsync(int id, MedioPago medio, DateTime? fecha, string? referenciaCobro,
        string? chequeBanco, string? chequeNumero, DateTime? chequeFechaVencimiento, string? observaciones);
}
