using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/inquilinos")]
[Authorize]
public class InquilinosController : ControllerBase
{
    private readonly IInquilinoRepository _repo;

    public InquilinosController(IInquilinoRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? buscar,
        [FromQuery] bool? activo)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, buscar, activo);
        var paginado = new PagedResult<InquilinoDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<InquilinoDto>>.Ok(paginado));
    }

    [HttpGet("activos")]
    public async Task<IActionResult> GetActivos()
    {
        var lista = await _repo.GetActivosAsync();
        var dtos = lista.Select(i => new InquilinoComboDto
        {
            Id = i.Id,
            NombreCompleto = $"{i.Apellido}, {i.Nombre}"
        });
        return Ok(ApiResponse<IEnumerable<InquilinoComboDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var i = await _repo.GetByIdAsync(id);
        if (i is null) return NotFound(ApiResponse<InquilinoDto>.Fail("Inquilino no encontrado."));
        return Ok(ApiResponse<InquilinoDto>.Ok(MapToDto(i)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInquilinoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido))
            return BadRequest(ApiResponse<InquilinoDto>.Fail("Nombre y apellido son requeridos."));

        var entidad = new Inquilino
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Dni = request.Dni,
            Cuit = request.Cuit,
            Email = request.Email,
            Telefono = request.Telefono,
            Telefono2 = request.Telefono2,
            Direccion = request.Direccion,
            Ocupacion = request.Ocupacion,
            NombreGarante = request.NombreGarante,
            TelefonoGarante = request.TelefonoGarante,
            DniGarante = request.DniGarante,
            Notas = request.Notas
        };

        var creado = await _repo.CreateAsync(entidad);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<object>.Ok(new { creado.Id }, "Inquilino creado correctamente."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInquilinoRequest request)
    {
        var existente = await _repo.GetByIdAsync(id);
        if (existente is null) return NotFound(ApiResponse<InquilinoDto>.Fail("Inquilino no encontrado."));

        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido))
            return BadRequest(ApiResponse<InquilinoDto>.Fail("Nombre y apellido son requeridos."));

        existente.Nombre = request.Nombre;
        existente.Apellido = request.Apellido;
        existente.Dni = request.Dni;
        existente.Cuit = request.Cuit;
        existente.Email = request.Email;
        existente.Telefono = request.Telefono;
        existente.Telefono2 = request.Telefono2;
        existente.Direccion = request.Direccion;
        existente.Ocupacion = request.Ocupacion;
        existente.NombreGarante = request.NombreGarante;
        existente.TelefonoGarante = request.TelefonoGarante;
        existente.DniGarante = request.DniGarante;
        existente.Notas = request.Notas;
        existente.Activo = request.Activo;

        await _repo.UpdateAsync(existente);
        return Ok(ApiResponse<object>.Ok(null, "Inquilino actualizado correctamente."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _repo.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Inquilino no encontrado."));
        return Ok(ApiResponse<object>.Ok(null, "Inquilino dado de baja correctamente."));
    }

    private static InquilinoDto MapToDto(Inquilino i) => new()
    {
        Id = i.Id,
        Nombre = i.Nombre,
        Apellido = i.Apellido,
        Dni = i.Dni,
        Cuit = i.Cuit,
        Email = i.Email,
        Telefono = i.Telefono,
        Telefono2 = i.Telefono2,
        Direccion = i.Direccion,
        Ocupacion = i.Ocupacion,
        NombreGarante = i.NombreGarante,
        TelefonoGarante = i.TelefonoGarante,
        DniGarante = i.DniGarante,
        Notas = i.Notas,
        Activo = i.Activo,
        FechaCreacion = i.FechaCreacion
    };
}
