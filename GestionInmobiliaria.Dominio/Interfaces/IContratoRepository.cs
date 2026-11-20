using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IContratoRepository
{
    Task<PagedResult<Contrato>> GetPagedAsync(PaginationParams paginacion, string? buscar = null, TipoContrato? tipo = null, EstadoContrato? estado = null, int? propiedadId = null, int? agenteId = null);
    Task<Contrato?> GetByIdAsync(int id);
    Task<Contrato> CreateAsync(Contrato contrato);
    Task<Contrato> UpdateAsync(Contrato contrato);
    Task<bool> DeleteAsync(int id);
    Task<(bool Ok, string? Error, Contrato? Contrato)> TransicionEstadoAsync(int id, EstadoContrato nuevoEstado, string? motivo, DateTime? fecha);

    /// <summary>
    /// Cláusulas a usar para generar el PDF de este contrato: la foto congelada
    /// (<see cref="ContratoClausula"/>) si ya existe, o si todavía no pasó a Vigente (sin foto
    /// aún), la plantilla compartida en vivo (<see cref="ClausulaContrato"/>), como preview.
    /// </summary>
    Task<IEnumerable<ContratoClausula>> GetClausulasParaPdfAsync(int contratoId);
}

public interface IPagoRepository
{
    Task<IEnumerable<Pago>> GetByContratoAsync(int contratoId);
    Task<Pago?> GetByIdAsync(int id);
    Task<Pago?> GetByIdConContratoAsync(int id);
    Task<PagedResult<Pago>> GetPagedAsync(PaginationParams paginacion, int? contratoId = null, EstadoPago? estado = null, int? mes = null, int? anio = null, string? buscar = null, EstadoContrato? estadoContrato = null);
    Task<Pago> UpdateAsync(Pago pago);
    Task<Pago> UpdateWithDetallesAsync(Pago pago, IEnumerable<PagoDetalle> detalles);
}
