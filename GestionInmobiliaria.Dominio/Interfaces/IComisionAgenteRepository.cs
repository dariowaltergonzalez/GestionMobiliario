using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IComisionAgenteRepository
{
    Task<PagedResult<ComisionAgente>> GetPagedAsync(
        PaginationParams paginacion, EstadoComisionAgente? estado = null, int? agenteId = null, string? buscar = null);
    Task<ComisionAgente?> GetByIdAsync(int id);
    Task<ComisionAgente?> GetByContratoIdAsync(int contratoId);
    Task<ComisionAgente> CreateAsync(ComisionAgente comision);

    Task<ComisionAgente?> RegistrarPagoAsync(int id, DateTime fecha, MedioPago medio, string? observaciones);
    Task<ComisionAgente?> ActualizarPagoAsync(int id, DateTime fecha, MedioPago medio, string? observaciones);
    Task<ComisionAgente?> DeshacerPagoAsync(int id);
}
