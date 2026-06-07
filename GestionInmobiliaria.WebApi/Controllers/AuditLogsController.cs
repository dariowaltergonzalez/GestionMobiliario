using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _repo;

    public AuditLogsController(IAuditLogRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? accion,
        [FromQuery] string? entidad)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, accion, entidad);
        var paginado = new PagedResult<AuditLogDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(paginado));
    }

    [HttpGet("entidades")]
    public async Task<IActionResult> GetEntidades()
    {
        var entidades = await _repo.GetEntidadesAsync();
        return Ok(ApiResponse<IEnumerable<string>>.Ok(entidades));
    }

    private static AuditLogDto MapToDto(AuditLog a) => new()
    {
        Id = a.Id,
        Timestamp = a.Timestamp,
        EntityName = a.EntityName,
        Action = a.Action,
        EntityId = a.EntityId,
        UserName = a.UserName,
        OldValues = a.OldValues,
        NewValues = a.NewValues,
        ChangedProperties = a.ChangedProperties
    };
}
