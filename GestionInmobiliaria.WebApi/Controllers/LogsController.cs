using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly IAppLogRepository _repo;

    public LogsController(IAppLogRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] NivelLog? nivel)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, nivel);
        var paginado = new PagedResult<AppLogDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<AppLogDto>>.Ok(paginado));
    }

    private static AppLogDto MapToDto(AppLog l) => new()
    {
        Id = l.Id,
        Timestamp = l.Timestamp,
        Nivel = l.Nivel,
        Origen = l.Origen,
        Mensaje = l.Mensaje,
        Detalle = l.Detalle
    };
}
