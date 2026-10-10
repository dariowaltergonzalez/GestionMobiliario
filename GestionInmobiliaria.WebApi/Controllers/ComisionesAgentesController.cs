using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.WebApi.Controllers;

/// <summary>
/// Comisión de cierre del agente por contrato — distinta de Liquidaciones (que es la comisión de
/// administración de cobros al propietario, recurrente). Ver docs/logica-negocio.md, sección
/// COMISIONES DE AGENTES.
/// </summary>
[ApiController]
[Route("api/comisiones-agentes")]
[Authorize]
public class ComisionesAgentesController : ControllerBase
{
    private readonly IComisionAgenteRepository _repo;
    private readonly ApplicationDbContext _context;

    public ComisionesAgentesController(IComisionAgenteRepository repo, ApplicationDbContext context)
    {
        _repo = repo;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] EstadoComisionAgente? estado,
        [FromQuery] int? agenteId,
        [FromQuery] string? buscar)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, estado, agenteId, buscar);
        var paginado = new PagedResult<ComisionAgenteDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas,
        };
        return Ok(ApiResponse<PagedResult<ComisionAgenteDto>>.Ok(paginado));
    }

    [HttpGet("metricas")]
    public async Task<IActionResult> GetMetricas()
    {
        var ahora = DateTime.UtcNow;

        var pendientes = await _context.ComisionesAgentes.CountAsync(c => c.Estado == EstadoComisionAgente.Pendiente);
        var montoPendiente = await _context.ComisionesAgentes
            .Where(c => c.Estado == EstadoComisionAgente.Pendiente)
            .SumAsync(c => (decimal?)c.MontoAgente) ?? 0;
        var pagadasMes = await _context.ComisionesAgentes.CountAsync(c =>
            c.Estado == EstadoComisionAgente.Pagado && c.FechaPago.HasValue &&
            c.FechaPago.Value.Month == ahora.Month && c.FechaPago.Value.Year == ahora.Year);
        var montoPagadoMes = await _context.ComisionesAgentes
            .Where(c => c.Estado == EstadoComisionAgente.Pagado && c.FechaPago.HasValue &&
                        c.FechaPago.Value.Month == ahora.Month && c.FechaPago.Value.Year == ahora.Year)
            .SumAsync(c => (decimal?)c.MontoAgente) ?? 0;

        return Ok(ApiResponse<ComisionAgenteMetricasDto>.Ok(new ComisionAgenteMetricasDto
        {
            PendientesCount = pendientes,
            MontoTotalPendiente = montoPendiente,
            PagadasMesCount = pagadasMes,
            MontoPagadoMes = montoPagadoMes,
        }));
    }

    [HttpPost("{id}/pago")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> RegistrarPago(int id, [FromBody] RegistrarPagoComisionRequest request)
    {
        var actualizada = await _repo.RegistrarPagoAsync(id, request.Fecha ?? DateTime.UtcNow, (MedioPago)request.Medio, request.Observaciones?.Trim());
        if (actualizada is null) return NotFound(ApiResponse<ComisionAgenteDto>.Fail("Comisión no encontrada."));
        return Ok(ApiResponse<ComisionAgenteDto>.Ok(MapToDto(actualizada), "Pago registrado correctamente."));
    }

    [HttpPut("{id}/pago")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> ActualizarPago(int id, [FromBody] RegistrarPagoComisionRequest request)
    {
        var actualizada = await _repo.ActualizarPagoAsync(id, request.Fecha ?? DateTime.UtcNow, (MedioPago)request.Medio, request.Observaciones?.Trim());
        if (actualizada is null) return NotFound(ApiResponse<ComisionAgenteDto>.Fail("Comisión no encontrada o no está pagada."));
        return Ok(ApiResponse<ComisionAgenteDto>.Ok(MapToDto(actualizada), "Pago actualizado correctamente."));
    }

    [HttpDelete("{id}/pago")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> DeshacerPago(int id)
    {
        var actualizada = await _repo.DeshacerPagoAsync(id);
        if (actualizada is null) return NotFound(ApiResponse<ComisionAgenteDto>.Fail("Comisión no encontrada."));
        return Ok(ApiResponse<ComisionAgenteDto>.Ok(MapToDto(actualizada), "Se deshizo el pago, la comisión vuelve a estar Pendiente."));
    }

    private static ComisionAgenteDto MapToDto(ComisionAgente c) => new()
    {
        Id = c.Id,
        ContratoId = c.ContratoId,
        ContratoCodigo = c.Contrato.Codigo,
        PropiedadDireccion = c.Contrato.Propiedad.Direccion,
        AgenteId = c.AgenteId,
        AgenteNombre = $"{c.Agente.User.Nombre} {c.Agente.User.Apellido}",
        MontoComisionInmobiliaria = c.MontoComisionInmobiliaria,
        PorcentajeAgente = c.PorcentajeAgente,
        MontoAgente = c.MontoAgente,
        Estado = c.Estado.ToString(),
        FechaPago = c.FechaPago,
        Medio = c.Medio?.ToString(),
        Observaciones = c.Observaciones,
        FechaCreacion = c.FechaCreacion,
    };
}
