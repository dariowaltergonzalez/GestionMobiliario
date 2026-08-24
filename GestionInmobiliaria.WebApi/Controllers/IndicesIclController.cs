using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.WebApi.Controllers;

/// <summary>
/// Serie "Índice para Contratos de Locación" (ICL) del BCRA, usada para el ajuste automático de
/// cuotas. Ver docs/logica-negocio.md, sección PENDIENTES GENERALES → "Automatizar el ajuste
/// periódico de cuotas".
/// </summary>
[ApiController]
[Route("api/indices-icl")]
[Authorize]
public class IndicesIclController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IIndiceIclService _servicio;

    public IndicesIclController(ApplicationDbContext context, IIndiceIclService servicio)
    {
        _context = context;
        _servicio = servicio;
    }

    [HttpGet("ultima")]
    public async Task<IActionResult> GetUltima()
    {
        var ultima = await _context.IndicesIcl
            .OrderByDescending(t => t.Fecha)
            .FirstOrDefaultAsync();

        if (ultima is null)
            return NotFound(ApiResponse<IndiceIclDto>.Fail(
                "Todavía no hay ningún valor cargado. Ejecutá la actualización manual."));

        return Ok(ApiResponse<IndiceIclDto>.Ok(MapToDto(ultima)));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var query = _context.IndicesIcl.AsQueryable();
        if (desde.HasValue) query = query.Where(t => t.Fecha >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(t => t.Fecha <= hasta.Value.Date);

        var lista = await query.OrderByDescending(t => t.Fecha).Take(100).ToListAsync();
        return Ok(ApiResponse<IEnumerable<IndiceIclDto>>.Ok(lista.Select(MapToDto)));
    }

    [HttpPost("actualizar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizar()
    {
        var cantidad = await _servicio.ActualizarAsync();
        var ultima = await _context.IndicesIcl
            .OrderByDescending(t => t.Fecha)
            .FirstOrDefaultAsync();

        var resultado = new ActualizarIndiceIclResultDto
        {
            ValoresNuevos = cantidad,
            Ultima = ultima is null ? null : MapToDto(ultima),
        };

        var mensaje = cantidad > 0
            ? $"Se agregaron {cantidad} valores nuevos."
            : "No había valores nuevos para traer (ya está al día).";

        return Ok(ApiResponse<ActualizarIndiceIclResultDto>.Ok(resultado, mensaje));
    }

    private static IndiceIclDto MapToDto(Dominio.Entidades.IndiceIcl t) => new()
    {
        Fecha = t.Fecha,
        Valor = t.Valor,
        Origen = t.Origen,
        FechaConsulta = t.FechaConsulta,
    };
}
