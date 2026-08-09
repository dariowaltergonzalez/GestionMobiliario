using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface ILiquidacionRepository
{
    Task<PagedResult<Liquidacion>> GetPagedAsync(
        PaginationParams paginacion, EstadoLiquidacion? estado = null, int? propietarioId = null, string? buscar = null);
    Task<Liquidacion?> GetByIdAsync(int id);
    Task<Liquidacion?> GetByPagoIdAsync(int pagoId);
    Task<Liquidacion> CreateAsync(Liquidacion liquidacion);
    Task<bool> EliminarAsync(int id);

    Task<Liquidacion?> AgregarAbonoAsync(int liquidacionId, LiquidacionAbono abono);
    Task<Liquidacion?> ActualizarAbonoAsync(int liquidacionId, int abonoId, LiquidacionAbono datos);
    Task<Liquidacion?> EliminarAbonoAsync(int liquidacionId, int abonoId);
}
