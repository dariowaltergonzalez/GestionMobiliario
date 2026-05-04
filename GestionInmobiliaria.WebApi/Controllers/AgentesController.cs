using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/agentes")]
[Authorize]
public class AgentesController : ControllerBase
{
    private readonly IAgenteRepository _repo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AgentesController(
        IAgenteRepository repo,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _repo = repo;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? buscar,
        [FromQuery] bool? activo)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, buscar, activo);
        var paginado = new PagedResult<AgenteDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<AgenteDto>>.Ok(paginado));
    }

    [HttpGet("activos")]
    public async Task<IActionResult> GetActivos()
    {
        var lista = await _repo.GetActivosAsync();
        var dtos = lista.Select(a => new AgenteComboDto
        {
            Id = a.Id,
            NombreCompleto = $"{a.User.Apellido}, {a.User.Nombre}"
        });
        return Ok(ApiResponse<IEnumerable<AgenteComboDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var agente = await _repo.GetByIdAsync(id);
        if (agente is null) return NotFound(ApiResponse<AgenteDto>.Fail("Agente no encontrado."));
        return Ok(ApiResponse<AgenteDto>.Ok(MapToDto(agente)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAgenteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido))
            return BadRequest(ApiResponse<AgenteDto>.Fail("Nombre y apellido son requeridos."));

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(ApiResponse<AgenteDto>.Fail("El email es requerido."));

        var userExistente = await _userManager.FindByEmailAsync(request.Email);
        if (userExistente is not null)
            return BadRequest(ApiResponse<AgenteDto>.Fail("Ya existe un usuario con ese email."));

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Nombre = request.Nombre,
            Apellido = request.Apellido
        };

        var resultUser = await _userManager.CreateAsync(user, request.Password);
        if (!resultUser.Succeeded)
            return BadRequest(ApiResponse<AgenteDto>.Fail(string.Join(", ", resultUser.Errors.Select(e => e.Description))));

        if (!await _roleManager.RoleExistsAsync("Agente"))
            await _roleManager.CreateAsync(new IdentityRole("Agente"));
        await _userManager.AddToRoleAsync(user, "Agente");

        var agente = new Agente
        {
            UserId = user.Id,
            Zona = request.Zona,
            TelefonoInterno = request.TelefonoInterno,
            ComisionPorcentaje = request.ComisionPorcentaje,
            Notas = request.Notas
        };

        var creado = await _repo.CreateAsync(agente);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<object>.Ok(new { creado.Id }, "Agente creado correctamente."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAgenteRequest request)
    {
        var agente = await _repo.GetByIdAsync(id);
        if (agente is null) return NotFound(ApiResponse<AgenteDto>.Fail("Agente no encontrado."));

        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido))
            return BadRequest(ApiResponse<AgenteDto>.Fail("Nombre y apellido son requeridos."));

        agente.User.Nombre = request.Nombre;
        agente.User.Apellido = request.Apellido;
        agente.Zona = request.Zona;
        agente.TelefonoInterno = request.TelefonoInterno;
        agente.ComisionPorcentaje = request.ComisionPorcentaje;
        agente.Notas = request.Notas;
        agente.Activo = request.Activo;

        await _userManager.UpdateAsync(agente.User);
        await _repo.UpdateAsync(agente);

        return Ok(ApiResponse<object>.Ok(null, "Agente actualizado correctamente."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var agente = await _repo.GetByIdAsync(id);
        if (agente is null) return NotFound(ApiResponse<AgenteDto>.Fail("Agente no encontrado."));

        agente.User.Activo = false;
        await _userManager.UpdateAsync(agente.User);
        await _repo.DeleteAsync(id);

        return Ok(ApiResponse<object>.Ok(null, "Agente dado de baja correctamente."));
    }

    private static AgenteDto MapToDto(Agente a) => new()
    {
        Id = a.Id,
        UserId = a.UserId,
        Nombre = a.User.Nombre,
        Apellido = a.User.Apellido,
        Email = a.User.Email ?? string.Empty,
        Zona = a.Zona,
        TelefonoInterno = a.TelefonoInterno,
        ComisionPorcentaje = a.ComisionPorcentaje,
        Notas = a.Notas,
        Activo = a.Activo,
        FechaCreacion = a.FechaCreacion,
        CantidadPropiedades = a.Propiedades.Count,
        CantidadInquilinos = a.Inquilinos.Count
    };
}
