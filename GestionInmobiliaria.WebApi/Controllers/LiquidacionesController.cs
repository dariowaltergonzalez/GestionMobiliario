using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/liquidaciones")]
[Authorize]
public class LiquidacionesController : ControllerBase
{
    private readonly ILiquidacionRepository _repo;
    private readonly ApplicationDbContext _context;

    public LiquidacionesController(ILiquidacionRepository repo, ApplicationDbContext context)
    {
        _repo = repo;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] EstadoLiquidacion? estado,
        [FromQuery] int? propietarioId,
        [FromQuery] string? buscar)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, estado, propietarioId, buscar);
        var paginado = new PagedResult<LiquidacionDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<LiquidacionDto>>.Ok(paginado));
    }

    [HttpGet("metricas")]
    public async Task<IActionResult> GetMetricas()
    {
        var ahora = DateTime.UtcNow;

        var pendientesCount = await _context.Liquidaciones.CountAsync(l => l.Estado == EstadoLiquidacion.Pendiente);
        var montoPendienteTotal = await _context.Liquidaciones
            .Where(l => l.Estado == EstadoLiquidacion.Pendiente)
            .SumAsync(l => (decimal?)l.MontoALiquidar) ?? 0;
        var liquidadasMesCount = await _context.Liquidaciones.CountAsync(l =>
            l.Estado == EstadoLiquidacion.Liquidado &&
            l.FechaLiquidacion.HasValue &&
            l.FechaLiquidacion.Value.Month == ahora.Month &&
            l.FechaLiquidacion.Value.Year == ahora.Year);
        var montoLiquidadoMes = await _context.Liquidaciones
            .Where(l => l.Estado == EstadoLiquidacion.Liquidado &&
                        l.FechaLiquidacion.HasValue &&
                        l.FechaLiquidacion.Value.Month == ahora.Month &&
                        l.FechaLiquidacion.Value.Year == ahora.Year)
            .SumAsync(l => (decimal?)l.MontoALiquidar) ?? 0;

        return Ok(ApiResponse<LiquidacionMetricasDto>.Ok(new LiquidacionMetricasDto
        {
            PendientesCount = pendientesCount,
            MontoPendienteTotal = montoPendienteTotal,
            LiquidadasMesCount = liquidadasMesCount,
            MontoLiquidadoMes = montoLiquidadoMes,
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var liquidacion = await _repo.GetByIdAsync(id);
        if (liquidacion is null) return NotFound(ApiResponse<LiquidacionDto>.Fail("Liquidación no encontrada."));
        return Ok(ApiResponse<LiquidacionDto>.Ok(MapToDto(liquidacion)));
    }

    [HttpPut("{id}/liquidar")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Liquidar(int id, [FromBody] MarcarLiquidadaRequest request)
    {
        var existente = await _repo.GetByIdAsync(id);
        if (existente is null) return NotFound(ApiResponse<LiquidacionDto>.Fail("Liquidación no encontrada."));

        if (existente.Estado == EstadoLiquidacion.Liquidado)
            return BadRequest(ApiResponse<LiquidacionDto>.Fail("Esta liquidación ya fue marcada como liquidada."));

        var actualizada = await _repo.MarcarLiquidadaAsync(id, request.Fecha ?? DateTime.UtcNow, request.Observaciones?.Trim());
        var resultado = await _repo.GetByIdAsync(actualizada!.Id);
        return Ok(ApiResponse<LiquidacionDto>.Ok(MapToDto(resultado!), "Liquidación marcada como liquidada."));
    }

    private static LiquidacionDto MapToDto(Liquidacion l) => new()
    {
        Id = l.Id,
        PagoId = l.PagoId,
        ContratoId = l.Pago.ContratoId,
        ContratoCodigo = l.Pago.Contrato.Codigo,
        PropiedadDireccion = l.Pago.Contrato.Propiedad.Direccion,
        PropietarioRefId = l.Pago.Contrato.PropietarioRefId,
        PropietarioNombre = l.Pago.Contrato.LocadorNombre,
        PropietarioApellido = l.Pago.Contrato.LocadorApellido,
        NumeroCuota = l.Pago.NumeroCuota,
        Periodo = l.Pago.Periodo,
        Moneda = l.Pago.Contrato.Moneda.ToString(),
        MontoCobrado = l.MontoCobrado,
        ComisionPorcentaje = l.ComisionPorcentaje,
        ComisionMonto = l.ComisionMonto,
        MontoComision = l.MontoComision,
        MontoALiquidar = l.MontoALiquidar,
        Estado = l.Estado.ToString(),
        FechaLiquidacion = l.FechaLiquidacion,
        Observaciones = l.Observaciones,
        FechaCreacion = l.FechaCreacion,
    };
}
