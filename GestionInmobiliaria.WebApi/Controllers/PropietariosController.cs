using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/propietarios")]
[Authorize]
public class PropietariosController : ControllerBase
{
    private readonly IPropietarioRepository _repo;

    public PropietariosController(IPropietarioRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? buscar,
        [FromQuery] bool? activo)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, buscar, activo);
        var paginado = new PagedResult<PropietarioDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<PropietarioDto>>.Ok(paginado));
    }

    [HttpGet("activos")]
    public async Task<IActionResult> GetActivos()
    {
        var lista = await _repo.GetActivosAsync();
        var dtos = lista.Select(p => new PropietarioComboDto
        {
            Id = p.Id,
            NombreCompleto = $"{p.Apellido}, {p.Nombre}"
        });
        return Ok(ApiResponse<IEnumerable<PropietarioComboDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _repo.GetByIdAsync(id);
        if (p is null) return NotFound(ApiResponse<PropietarioDto>.Fail("Propietario no encontrado."));
        return Ok(ApiResponse<PropietarioDto>.Ok(MapToDto(p)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePropietarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido))
            return BadRequest(ApiResponse<PropietarioDto>.Fail("Nombre y apellido son requeridos."));

        var entidad = new Propietario
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Dni = request.Dni,
            Cuit = request.Cuit,
            Email = request.Email,
            Telefono = request.Telefono,
            Telefono2 = request.Telefono2,
            Direccion = request.Direccion,
            Banco = request.Banco,
            CBU = request.CBU,
            Notas = request.Notas
        };

        var creado = await _repo.CreateAsync(entidad);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<object>.Ok(new { creado.Id }, "Propietario creado correctamente."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePropietarioRequest request)
    {
        var existente = await _repo.GetByIdAsync(id);
        if (existente is null) return NotFound(ApiResponse<PropietarioDto>.Fail("Propietario no encontrado."));

        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido))
            return BadRequest(ApiResponse<PropietarioDto>.Fail("Nombre y apellido son requeridos."));

        existente.Nombre = request.Nombre;
        existente.Apellido = request.Apellido;
        existente.Dni = request.Dni;
        existente.Cuit = request.Cuit;
        existente.Email = request.Email;
        existente.Telefono = request.Telefono;
        existente.Telefono2 = request.Telefono2;
        existente.Direccion = request.Direccion;
        existente.Banco = request.Banco;
        existente.CBU = request.CBU;
        existente.Notas = request.Notas;
        existente.Activo = request.Activo;

        await _repo.UpdateAsync(existente);
        return Ok(ApiResponse<object>.Ok(null, "Propietario actualizado correctamente."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _repo.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Propietario no encontrado."));
        return Ok(ApiResponse<object>.Ok(null, "Propietario dado de baja correctamente."));
    }

    private static PropietarioDto MapToDto(Propietario p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Apellido = p.Apellido,
        Dni = p.Dni,
        Cuit = p.Cuit,
        Email = p.Email,
        Telefono = p.Telefono,
        Telefono2 = p.Telefono2,
        Direccion = p.Direccion,
        Banco = p.Banco,
        CBU = p.CBU,
        Notas = p.Notas,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion,
        CantidadPropiedades = p.Propiedades.Count
    };
}
