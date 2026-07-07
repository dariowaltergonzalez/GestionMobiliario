using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IReservaRepository
{
    Task<PagedResult<Reserva>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, EstadoReserva? estado = null, int? propiedadId = null);
    Task<Reserva?> GetByIdAsync(int id);
    Task<Reserva> CreateAsync(Reserva reserva);
    Task<Reserva> UpdateAsync(Reserva reserva);
    Task<bool> DeleteAsync(int id);
}
