using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/propiedades")]
[Authorize]
public class PropiedadesController : ControllerBase
{
    private readonly IPropiedadRepository _repo;
    private readonly IPropietarioRepository _propietarioRepo;

    public PropiedadesController(IPropiedadRepository repo, IPropietarioRepository propietarioRepo)
    {
        _repo = repo;
        _propietarioRepo = propietarioRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? buscar,
        [FromQuery] TipoPropiedad? tipo,
        [FromQuery] EstadoPropiedad? estado,
        [FromQuery] int? propietarioId)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, buscar, tipo, estado, propietarioId);
        var paginado = new PagedResult<PropiedadDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<PropiedadDto>>.Ok(paginado));
    }

    [HttpGet("disponibles")]
    public async Task<IActionResult> GetDisponibles()
    {
        var lista = await _repo.GetDisponiblesAsync();
        var dtos = lista.Select(p => new PropiedadComboDto
        {
            Id = p.Id,
            Direccion = p.Direccion,
            TipoNombre = p.Tipo.ToString(),
            PrecioAlquiler = p.PrecioAlquiler,
            PropietarioNombre = $"{p.Propietario.Apellido}, {p.Propietario.Nombre}"
        });
        return Ok(ApiResponse<IEnumerable<PropiedadComboDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _repo.GetByIdAsync(id);
        if (p is null) return NotFound(ApiResponse<PropiedadDto>.Fail("Propiedad no encontrada."));
        return Ok(ApiResponse<PropiedadDto>.Ok(MapToDto(p)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePropiedadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Direccion))
            return BadRequest(ApiResponse<PropiedadDto>.Fail("La dirección es requerida."));

        if (request.PrecioAlquiler <= 0)
            return BadRequest(ApiResponse<PropiedadDto>.Fail("El precio de alquiler debe ser mayor a cero."));

        var propietario = await _propietarioRepo.GetByIdAsync(request.PropietarioId);
        if (propietario is null)
            return BadRequest(ApiResponse<PropiedadDto>.Fail("El propietario indicado no existe."));

        var entidad = new Propiedad
        {
            Tipo = request.Tipo,
            Direccion = request.Direccion,
            Barrio = request.Barrio,
            Ciudad = request.Ciudad,
            Provincia = request.Provincia,
            Ambientes = request.Ambientes,
            Dormitorios = request.Dormitorios,
            Banios = request.Banios,
            SuperficieTotal = request.SuperficieTotal,
            SuperficieCubierta = request.SuperficieCubierta,
            Piso = request.Piso,
            NumeroDepartamento = request.NumeroDepartamento,
            PrecioAlquiler = request.PrecioAlquiler,
            Expensas = request.Expensas,
            Estado = request.Estado,
            EstadoConservacion = request.EstadoConservacion,
            Cochera = request.Cochera,
            Antiguedad = request.Antiguedad,
            TieneCalefaccion = request.TieneCalefaccion,
            AceptaMascotas = request.AceptaMascotas,
            NroCatastro = request.NroCatastro,
            Descripcion = request.Descripcion,
            Notas = request.Notas,
            PropietarioId = request.PropietarioId
        };

        var creado = await _repo.CreateAsync(entidad);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<object>.Ok(new { creado.Id }, "Propiedad creada correctamente."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePropiedadRequest request)
    {
        var existente = await _repo.GetByIdAsync(id);
        if (existente is null) return NotFound(ApiResponse<PropiedadDto>.Fail("Propiedad no encontrada."));

        if (string.IsNullOrWhiteSpace(request.Direccion))
            return BadRequest(ApiResponse<PropiedadDto>.Fail("La dirección es requerida."));

        if (request.PrecioAlquiler <= 0)
            return BadRequest(ApiResponse<PropiedadDto>.Fail("El precio de alquiler debe ser mayor a cero."));

        var propietario = await _propietarioRepo.GetByIdAsync(request.PropietarioId);
        if (propietario is null)
            return BadRequest(ApiResponse<PropiedadDto>.Fail("El propietario indicado no existe."));

        existente.Tipo = request.Tipo;
        existente.Direccion = request.Direccion;
        existente.Barrio = request.Barrio;
        existente.Ciudad = request.Ciudad;
        existente.Provincia = request.Provincia;
        existente.Ambientes = request.Ambientes;
        existente.Dormitorios = request.Dormitorios;
        existente.Banios = request.Banios;
        existente.SuperficieTotal = request.SuperficieTotal;
        existente.SuperficieCubierta = request.SuperficieCubierta;
        existente.Piso = request.Piso;
        existente.NumeroDepartamento = request.NumeroDepartamento;
        existente.PrecioAlquiler = request.PrecioAlquiler;
        existente.Expensas = request.Expensas;
        existente.Estado = request.Estado;
        existente.EstadoConservacion = request.EstadoConservacion;
        existente.Cochera = request.Cochera;
        existente.Antiguedad = request.Antiguedad;
        existente.TieneCalefaccion = request.TieneCalefaccion;
        existente.AceptaMascotas = request.AceptaMascotas;
        existente.NroCatastro = request.NroCatastro;
        existente.Descripcion = request.Descripcion;
        existente.Notas = request.Notas;
        existente.PropietarioId = request.PropietarioId;

        await _repo.UpdateAsync(existente);
        return Ok(ApiResponse<object>.Ok(null, "Propiedad actualizada correctamente."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _repo.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Propiedad no encontrada."));
        return Ok(ApiResponse<object>.Ok(null, "Propiedad dada de baja correctamente."));
    }

    private static PropiedadDto MapToDto(Propiedad p) => new()
    {
        Id = p.Id,
        Tipo = p.Tipo,
        Direccion = p.Direccion,
        Barrio = p.Barrio,
        Ciudad = p.Ciudad,
        Provincia = p.Provincia,
        Ambientes = p.Ambientes,
        Dormitorios = p.Dormitorios,
        Banios = p.Banios,
        SuperficieTotal = p.SuperficieTotal,
        SuperficieCubierta = p.SuperficieCubierta,
        Piso = p.Piso,
        NumeroDepartamento = p.NumeroDepartamento,
        PrecioAlquiler = p.PrecioAlquiler,
        Expensas = p.Expensas,
        Estado = p.Estado,
        EstadoConservacion = p.EstadoConservacion,
        Cochera = p.Cochera,
        Antiguedad = p.Antiguedad,
        TieneCalefaccion = p.TieneCalefaccion,
        AceptaMascotas = p.AceptaMascotas,
        NroCatastro = p.NroCatastro,
        Descripcion = p.Descripcion,
        Notas = p.Notas,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion,
        PropietarioId = p.PropietarioId,
        PropietarioNombre = $"{p.Propietario.Nombre} {p.Propietario.Apellido}"
    };
}
