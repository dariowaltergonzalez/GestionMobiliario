using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventosAgendaController : ControllerBase
{
    private readonly IEventoAgendaRepository _repo;

    public EventosAgendaController(IEventoAgendaRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] int? agenteId,
        [FromQuery] EstadoEvento? estado,
        [FromQuery] TipoEvento? tipo)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, agenteId, estado, tipo);
        var paginado = new PagedResult<EventoAgendaDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<EventoAgendaDto>>.Ok(paginado));
    }

    [HttpGet("agente/{agenteId:int}")]
    public async Task<IActionResult> GetByAgente(int agenteId)
    {
        var lista = await _repo.GetByAgenteAsync(agenteId);
        return Ok(ApiResponse<IEnumerable<EventoAgendaDto>>.Ok(lista.Select(MapToDto)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var evento = await _repo.GetByIdAsync(id);
        if (evento is null) return NotFound(ApiResponse<EventoAgendaDto>.Fail("Evento no encontrado."));
        return Ok(ApiResponse<EventoAgendaDto>.Ok(MapToDto(evento)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventoAgendaRequest request)
    {
        var evento = new EventoAgenda
        {
            Tipo = request.Tipo,
            FechaHora = request.FechaHora,
            Notas = request.Notas,
            AgenteId = request.AgenteId,
            PropiedadId = request.PropiedadId,
            LeadId = request.LeadId,
            InquilinoId = request.InquilinoId
        };
        var creado = await _repo.CreateAsync(evento);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, ApiResponse<EventoAgendaDto>.Ok(MapToDto(creado)));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventoAgendaRequest request)
    {
        var evento = await _repo.GetByIdAsync(id);
        if (evento is null) return NotFound(ApiResponse<EventoAgendaDto>.Fail("Evento no encontrado."));

        evento.Tipo = request.Tipo;
        evento.Estado = request.Estado;
        evento.FechaHora = request.FechaHora;
        evento.Notas = request.Notas;
        evento.AgenteId = request.AgenteId;
        evento.PropiedadId = request.PropiedadId;
        evento.LeadId = request.LeadId;
        evento.InquilinoId = request.InquilinoId;

        var actualizado = await _repo.UpdateAsync(evento);
        return Ok(ApiResponse<EventoAgendaDto>.Ok(MapToDto(actualizado)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _repo.DeleteAsync(id);
        if (!eliminado) return NotFound(ApiResponse<bool>.Fail("Evento no encontrado."));
        return Ok(ApiResponse<bool>.Ok(true, "Evento eliminado correctamente."));
    }

    private static EventoAgendaDto MapToDto(EventoAgenda e) => new()
    {
        Id = e.Id,
        Tipo = e.Tipo.ToString(),
        Estado = e.Estado.ToString(),
        FechaHora = e.FechaHora,
        Notas = e.Notas,
        AgenteId = e.AgenteId,
        AgenteNombre = $"{e.Agente.User.Nombre} {e.Agente.User.Apellido}",
        PropiedadId = e.PropiedadId,
        PropiedadDireccion = e.Propiedad?.Direccion,
        LeadId = e.LeadId,
        LeadNombre = e.Lead is not null ? $"{e.Lead.Nombre} {e.Lead.Apellido}" : null,
        InquilinoId = e.InquilinoId,
        InquilinoNombre = e.Inquilino is not null ? $"{e.Inquilino.Nombre} {e.Inquilino.Apellido}" : null
    };
}
