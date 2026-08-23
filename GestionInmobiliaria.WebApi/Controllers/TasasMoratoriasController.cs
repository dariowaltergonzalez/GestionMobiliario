using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.WebApi.Controllers;

/// <summary>
/// Serie "Tasa de Intereses Moratorios" (TIM) del BCRA, usada para calcular Punitorios cuando el
/// contrato no tiene un % fijo propio cargado. Ver docs/logica-negocio.md, sección PUNITORIOS.
/// </summary>
[ApiController]
[Route("api/tasas-moratorias")]
[Authorize]
public class TasasMoratoriasController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITasaMoratoriaService _servicio;

    public TasasMoratoriasController(ApplicationDbContext context, ITasaMoratoriaService servicio)
    {
        _context = context;
        _servicio = servicio;
    }

    [HttpGet("ultima")]
    public async Task<IActionResult> GetUltima()
    {
        var ultima = await _context.TasasMoratorias
            .OrderByDescending(t => t.Fecha)
            .FirstOrDefaultAsync();

        if (ultima is null)
            return NotFound(ApiResponse<TasaMoratoriaDto>.Fail(
                "Todavía no hay ningún valor cargado. Ejecutá la actualización manual."));

        return Ok(ApiResponse<TasaMoratoriaDto>.Ok(MapToDto(ultima)));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var query = _context.TasasMoratorias.AsQueryable();
        if (desde.HasValue) query = query.Where(t => t.Fecha >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(t => t.Fecha <= hasta.Value.Date);

        var lista = await query.OrderByDescending(t => t.Fecha).Take(100).ToListAsync();
        return Ok(ApiResponse<IEnumerable<TasaMoratoriaDto>>.Ok(lista.Select(MapToDto)));
    }

    [HttpPost("actualizar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizar()
    {
        var cantidad = await _servicio.ActualizarAsync();
        var ultima = await _context.TasasMoratorias
            .OrderByDescending(t => t.Fecha)
            .FirstOrDefaultAsync();

        var resultado = new ActualizarTasaMoratoriaResultDto
        {
            ValoresNuevos = cantidad,
            Ultima = ultima is null ? null : MapToDto(ultima),
        };

        var mensaje = cantidad > 0
            ? $"Se agregaron {cantidad} valores nuevos."
            : "No había valores nuevos para traer (ya está al día).";

        return Ok(ApiResponse<ActualizarTasaMoratoriaResultDto>.Ok(resultado, mensaje));
    }

    private static TasaMoratoriaDto MapToDto(Dominio.Entidades.TasaMoratoria t) => new()
    {
        Fecha = t.Fecha,
        Valor = t.Valor,
        Origen = t.Origen,
        FechaConsulta = t.FechaConsulta,
    };
}
