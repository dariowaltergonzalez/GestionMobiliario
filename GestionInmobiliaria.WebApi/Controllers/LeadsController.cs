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
public class LeadsController : ControllerBase
{
    private readonly ILeadRepository _repo;
    private readonly IPropiedadRepository _propiedadRepo;
    private readonly IAgenteRepository _agenteRepo;
    private readonly IEventoAgendaRepository _eventoRepo;

    public LeadsController(
        ILeadRepository repo,
        IPropiedadRepository propiedadRepo,
        IAgenteRepository agenteRepo,
        IEventoAgendaRepository eventoRepo)
    {
        _repo = repo;
        _propiedadRepo = propiedadRepo;
        _agenteRepo = agenteRepo;
        _eventoRepo = eventoRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? buscar,
        [FromQuery] EstadoLead? estado,
        [FromQuery] int? agenteId)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, buscar, estado, agenteId);
        var paginado = new PagedResult<LeadDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<LeadDto>>.Ok(paginado));
    }

    [HttpGet("activos")]
    public async Task<IActionResult> GetActivos()
    {
        var lista = await _repo.GetActivosAsync();
        var dtos = lista.Select(l => new LeadComboDto
        {
            Id = l.Id,
            NombreCompleto = $"{l.Nombre} {l.Apellido}",
            Email = l.Email
        });
        return Ok(ApiResponse<IEnumerable<LeadComboDto>>.Ok(dtos));
    }

    [HttpGet("para-reserva")]
    public async Task<IActionResult> GetParaReserva()
    {
        var lista = await _repo.GetActivosAsync();
        var dtos = lista.Select(l => new
        {
            id = l.Id,
            nombre = l.Nombre,
            apellido = l.Apellido,
            telefono = l.Telefono,
            email = l.Email,
        });
        return Ok(ApiResponse<IEnumerable<object>>.Ok(dtos));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var lead = await _repo.GetByIdAsync(id);
        if (lead is null) return NotFound(ApiResponse<LeadDto>.Fail("Lead no encontrado."));
        return Ok(ApiResponse<LeadDto>.Ok(MapToDto(lead)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeadRequest request)
    {
        var lead = new Lead
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Email = request.Email,
            Telefono = request.Telefono,
            Origen = request.Origen,
            Notas = request.Notas,
            AgenteId = request.AgenteId,
            PropiedadId = request.PropiedadId
        };
        var creado = await _repo.CreateAsync(lead);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, ApiResponse<LeadDto>.Ok(MapToDto(creado)));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLeadRequest request)
    {
        var lead = await _repo.GetByIdAsync(id);
        if (lead is null) return NotFound(ApiResponse<LeadDto>.Fail("Lead no encontrado."));

        lead.Nombre = request.Nombre;
        lead.Apellido = request.Apellido;
        lead.Email = request.Email;
        lead.Telefono = request.Telefono;
        lead.Origen = request.Origen;
        lead.Estado = request.Estado;
        lead.Notas = request.Notas;
        lead.AgenteId = request.AgenteId;
        lead.PropiedadId = request.PropiedadId;

        var actualizado = await _repo.UpdateAsync(lead);
        return Ok(ApiResponse<LeadDto>.Ok(MapToDto(actualizado)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _repo.DeleteAsync(id);
        if (!eliminado) return NotFound(ApiResponse<bool>.Fail("Lead no encontrado."));
        return Ok(ApiResponse<bool>.Ok(true, "Lead eliminado correctamente."));
    }

    [AllowAnonymous]
    [HttpPost("solicitar-visita")]
    public async Task<IActionResult> SolicitarVisita([FromBody] SolicitarVisitaRequest request)
    {
        var propiedad = await _propiedadRepo.GetByIdAsync(request.PropiedadId);
        if (propiedad is null)
            return NotFound(ApiResponse<object>.Fail("Propiedad no encontrada."));

        var lead = new Lead
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Email = request.Email,
            Telefono = request.Telefono,
            Origen = OrigenLead.Web,
            Estado = EstadoLead.Nuevo,
            Notas = request.Notas,
            PropiedadId = propiedad.Id,
            AgenteId = propiedad.AgenteId
        };
        var leadCreado = await _repo.CreateAsync(lead);

        if (propiedad.AgenteId.HasValue)
        {
            var evento = new EventoAgenda
            {
                Tipo = TipoEvento.Visita,
                Estado = EstadoEvento.Pendiente,
                FechaHora = request.FechaHoraPreferida,
                AgenteId = propiedad.AgenteId.Value,
                PropiedadId = propiedad.Id,
                LeadId = leadCreado.Id,
                Notas = $"Visita solicitada desde el sitio web. Tel: {request.Telefono}"
            };
            await _eventoRepo.CreateAsync(evento);
        }

        return Ok(ApiResponse<object>.Ok(new { leadId = leadCreado.Id },
            "Tu solicitud fue registrada. Un agente se comunicará contigo a la brevedad."));
    }

    private static LeadDto MapToDto(Lead l) => new()
    {
        Id = l.Id,
        Nombre = l.Nombre,
        Apellido = l.Apellido,
        Email = l.Email,
        Telefono = l.Telefono,
        Origen = l.Origen.ToString(),
        Estado = l.Estado.ToString(),
        Notas = l.Notas,
        Activo = l.Activo,
        FechaCreacion = l.FechaCreacion,
        AgenteId = l.AgenteId,
        AgenteNombre = l.Agente is not null ? $"{l.Agente.User.Nombre} {l.Agente.User.Apellido}" : null,
        PropiedadId = l.PropiedadId,
        PropiedadDireccion = l.Propiedad?.Direccion
    };
}
