using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/clausulas-contrato")]
[Authorize]
public class ClausulasContratoController : ControllerBase
{
    private readonly IClausulaContratoRepository _repo;

    public ClausulasContratoController(IClausulaContratoRepository repo)
        => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var lista = await _repo.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<ClausulaContratoDto>>.Ok(lista.Select(MapDto)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _repo.GetByIdAsync(id);
        if (c is null) return NotFound(ApiResponse<string>.Fail("Cláusula no encontrada."));
        return Ok(ApiResponse<ClausulaContratoDto>.Ok(MapDto(c)));
    }

    [HttpGet("placeholders")]
    public IActionResult GetPlaceholders()
    {
        var grupos = new[]
        {
            new {
                entidad = "locador",
                etiqueta = "Locador",
                campos = new[]
                {
                    new { clave = "{locador.nombreCompleto}", descripcion = "Nombre y apellido completo" },
                    new { clave = "{locador.nombre}",         descripcion = "Nombre de pila" },
                    new { clave = "{locador.apellido}",       descripcion = "Apellido" },
                    new { clave = "{locador.dni}",            descripcion = "DNI" },
                    new { clave = "{locador.email}",          descripcion = "Email" },
                    new { clave = "{locador.telefono}",       descripcion = "Teléfono / celular" },
                    new { clave = "{locador.domicilio}",      descripcion = "Domicilio particular" },
                    new { clave = "{locador.banco}",          descripcion = "Banco (para transferencia)" },
                    new { clave = "{locador.cbu}",            descripcion = "CBU" },
                    new { clave = "{locador.cuit}",           descripcion = "CUIT" },
                }
            },
            new {
                entidad = "locatario",
                etiqueta = "Locatario",
                campos = new[]
                {
                    new { clave = "{locatario.nombreCompleto}", descripcion = "Nombre y apellido completo" },
                    new { clave = "{locatario.nombre}",         descripcion = "Nombre de pila" },
                    new { clave = "{locatario.apellido}",       descripcion = "Apellido" },
                    new { clave = "{locatario.dni}",            descripcion = "DNI" },
                    new { clave = "{locatario.email}",          descripcion = "Email" },
                    new { clave = "{locatario.telefono}",       descripcion = "Teléfono / celular" },
                }
            },
            new {
                entidad = "propiedad",
                etiqueta = "Propiedad",
                campos = new[]
                {
                    new { clave = "{propiedad.direccion}", descripcion = "Dirección completa del inmueble" },
                    new { clave = "{propiedad.codigo}",    descripcion = "Código interno de la propiedad" },
                }
            },
            new {
                entidad = "garante",
                etiqueta = "Garante",
                campos = new[]
                {
                    new { clave = "{garante.nombreCompleto}", descripcion = "Nombre y apellido completo" },
                    new { clave = "{garante.nombre}",         descripcion = "Nombre de pila" },
                    new { clave = "{garante.apellido}",       descripcion = "Apellido" },
                    new { clave = "{garante.dni}",            descripcion = "DNI" },
                    new { clave = "{garante.telefono}",       descripcion = "Teléfono / celular" },
                    new { clave = "{garante.texto}",          descripcion = "Texto completo del garante (o texto genérico si no hay garante)" },
                }
            },
            new {
                entidad = "contrato",
                etiqueta = "Contrato",
                campos = new[]
                {
                    new { clave = "{contrato.montoAlquiler}",  descripcion = "Monto mensual (ej: $ 150.000)" },
                    new { clave = "{contrato.duracionMeses}",  descripcion = "Duración en meses" },
                    new { clave = "{contrato.fechaInicio}",    descripcion = "Fecha de inicio (dd/mm/aaaa)" },
                    new { clave = "{contrato.fechaFin}",       descripcion = "Fecha de fin (dd/mm/aaaa)" },
                    new { clave = "{contrato.mesInicio}",      descripcion = "Primer mes en letras (ej: ENERO 2026)" },
                    new { clave = "{contrato.ajusteTexto}",    descripcion = "Tipo de ajuste (ICL / porcentaje / fijo)" },
                    new { clave = "{contrato.periodicidad}",   descripcion = "Periodicidad del ajuste (ej: cada 6 meses)" },
                    new { clave = "{contrato.diaVencimiento}", descripcion = "Día de vencimiento (vacío si no se configuró)" },
                    new { clave = "{contrato.pagoMedio}",      descripcion = "Instrucciones de pago (CBU o texto genérico)" },
                    new { clave = "{contrato.garanteTexto}",   descripcion = "Cláusula de garantía completa" },
                }
            },
            new {
                entidad = "empresa",
                etiqueta = "Empresa",
                campos = new[]
                {
                    new { clave = "{empresa.nombre}", descripcion = "Nombre de la inmobiliaria" },
                    new { clave = "{empresa.ciudad}", descripcion = "Ciudad de la inmobiliaria" },
                }
            },
        };
        return Ok(ApiResponse<object>.Ok(grupos));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClausulaContratoRequest req)
    {
        var clausula = new ClausulaContrato
        {
            Numero = req.Numero.Trim(),
            Titulo = req.Titulo.Trim(),
            Texto = req.Texto.Trim(),
            Activo = true,
        };
        var created = await _repo.CreateAsync(clausula);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<ClausulaContratoDto>.Ok(MapDto(created)));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClausulaContratoRequest req)
    {
        var clausula = await _repo.GetByIdAsync(id);
        if (clausula is null) return NotFound(ApiResponse<string>.Fail("Cláusula no encontrada."));

        clausula.Numero = req.Numero.Trim();
        clausula.Titulo = req.Titulo.Trim();
        clausula.Texto = req.Texto.Trim();
        clausula.Activo = req.Activo;

        await _repo.UpdateAsync(clausula);
        return Ok(ApiResponse<ClausulaContratoDto>.Ok(MapDto(clausula)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repo.DeleteAsync(id);
        if (!deleted) return NotFound(ApiResponse<string>.Fail("Cláusula no encontrada."));
        return Ok(ApiResponse<string>.Ok("Cláusula eliminada."));
    }

    [HttpPost("inicializar")]
    public async Task<IActionResult> Inicializar()
    {
        await _repo.InicializarDefaultsAsync();
        var lista = await _repo.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<ClausulaContratoDto>>.Ok(lista.Select(MapDto)));
    }

    [HttpPut("{id:int}/mover")]
    public async Task<IActionResult> Mover(int id, [FromQuery] bool subir)
    {
        await _repo.MoverAsync(id, subir);
        return Ok(ApiResponse<string>.Ok("Orden actualizado."));
    }

    private static ClausulaContratoDto MapDto(ClausulaContrato c) => new()
    {
        Id = c.Id,
        Orden = c.Orden,
        Numero = c.Numero,
        Titulo = c.Titulo,
        Texto = c.Texto,
        Activo = c.Activo,
    };
}
