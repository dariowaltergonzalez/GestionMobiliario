using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/solicitudes-tasacion")]
[Authorize]
public class SolicitudesTasacionController : ControllerBase
{
    private readonly ISolicitudTasacionRepository _repo;
    private readonly ILeadRepository _leadRepo;
    private readonly IStorageService _storage;
    private readonly ITenantService _tenantService;

    private static readonly string[] ExtensionesPermitidas = [".jpg", ".jpeg", ".png", ".webp", ".heic"];
    private const long TamanoMaximoBytes = 10 * 1024 * 1024; // 10 MB por archivo

    public SolicitudesTasacionController(
        ISolicitudTasacionRepository repo,
        ILeadRepository leadRepo,
        IStorageService storage,
        ITenantService tenantService)
    {
        _repo = repo;
        _leadRepo = leadRepo;
        _storage = storage;
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? buscar,
        [FromQuery] EstadoSolicitudTasacion? estado,
        [FromQuery] int? agenteId)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, buscar, estado, agenteId);
        var paginado = new PagedResult<SolicitudTasacionDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<SolicitudTasacionDto>>.Ok(paginado));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var solicitud = await _repo.GetByIdAsync(id);
        if (solicitud is null)
            return NotFound(ApiResponse<SolicitudTasacionDto>.Fail("Solicitud no encontrada."));
        return Ok(ApiResponse<SolicitudTasacionDto>.Ok(MapToDto(solicitud)));
    }

    [AllowAnonymous]
    [HttpPost("solicitar")]
    public async Task<IActionResult> Solicitar([FromBody] CreateSolicitudTasacionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest(ApiResponse<SolicitudTasacionDto>.Fail("El nombre es requerido."));
        if (string.IsNullOrWhiteSpace(request.Telefono))
            return BadRequest(ApiResponse<SolicitudTasacionDto>.Fail("El teléfono es requerido."));
        if (string.IsNullOrWhiteSpace(request.Direccion))
            return BadRequest(ApiResponse<SolicitudTasacionDto>.Fail("La dirección de la propiedad es requerida."));

        var solicitud = new SolicitudTasacion
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Email = request.Email,
            Telefono = request.Telefono,
            TipoPropiedad = request.TipoPropiedad,
            Direccion = request.Direccion,
            Barrio = request.Barrio,
            Ciudad = request.Ciudad,
            SuperficieTotal = request.SuperficieTotal,
            SuperficieCubierta = request.SuperficieCubierta,
            Ambientes = request.Ambientes,
            Banios = request.Banios,
            Antiguedad = request.Antiguedad,
            EstadoConservacion = request.EstadoConservacion,
            Descripcion = request.Descripcion,
            TipoContactoPreferido = request.TipoContactoPreferido,
            Estado = EstadoSolicitudTasacion.Pendiente
        };

        var creada = await _repo.CreateAsync(solicitud);

        await _leadRepo.CreateAsync(new Lead
        {
            Nombre = creada.Nombre,
            Apellido = creada.Apellido,
            Email = creada.Email,
            Telefono = creada.Telefono,
            Origen = OrigenLead.Web,
            Estado = EstadoLead.Nuevo,
            Notas = $"Solicitud de tasación — {creada.TipoPropiedad} en {creada.Direccion}" +
                    (!string.IsNullOrWhiteSpace(creada.Barrio) ? $", {creada.Barrio}" : ""),
            TenantId = creada.TenantId
        });

        return Ok(ApiResponse<SolicitudTasacionDto>.Ok(MapToDto(creada), "Solicitud recibida. Un agente se comunicará con usted a la brevedad."));
    }

    [AllowAnonymous]
    [HttpPost("{id}/fotos")]
    public async Task<IActionResult> SubirFotos(int id, [FromForm] List<IFormFile> fotos)
    {
        var solicitud = await _repo.GetByIdAsync(id);
        if (solicitud is null)
            return NotFound(ApiResponse<object>.Fail("Solicitud no encontrada."));

        if (fotos is null || fotos.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("Debe enviar al menos una foto."));

        if (fotos.Count > 10)
            return BadRequest(ApiResponse<object>.Fail("Máximo 10 fotos por solicitud."));

        var fotosGuardadas = new List<FotoSolicitudDto>();
        var tenantId = solicitud.TenantId;

        foreach (var archivo in fotos)
        {
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
                return BadRequest(ApiResponse<object>.Fail($"Tipo de archivo no permitido: {extension}. Use JPG, PNG o WebP."));

            if (archivo.Length > TamanoMaximoBytes)
                return BadRequest(ApiResponse<object>.Fail($"El archivo {archivo.FileName} supera el tamaño máximo de 10 MB."));

            using var stream = archivo.OpenReadStream();
            var url = await _storage.GuardarArchivoAsync(stream, archivo.FileName, $"{tenantId}/{id}");

            var foto = await _repo.AddFotoAsync(new FotoSolicitud
            {
                SolicitudTasacionId = id,
                Url = url,
                NombreArchivo = archivo.FileName,
                TenantId = tenantId
            });

            fotosGuardadas.Add(new FotoSolicitudDto
            {
                Id = foto.Id,
                Url = foto.Url,
                NombreArchivo = foto.NombreArchivo,
                FechaSubida = foto.FechaSubida
            });
        }

        return Ok(ApiResponse<List<FotoSolicitudDto>>.Ok(fotosGuardadas, $"{fotosGuardadas.Count} foto(s) subida(s) correctamente."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSolicitudTasacionRequest request)
    {
        var solicitud = await _repo.GetByIdAsync(id);
        if (solicitud is null)
            return NotFound(ApiResponse<SolicitudTasacionDto>.Fail("Solicitud no encontrada."));

        solicitud.Estado = request.Estado;
        solicitud.AgenteId = request.AgenteId;
        solicitud.NotasInternas = request.NotasInternas;
        solicitud.ValorEstimado = request.ValorEstimado;

        var actualizada = await _repo.UpdateAsync(solicitud);
        return Ok(ApiResponse<SolicitudTasacionDto>.Ok(MapToDto(actualizada), "Solicitud actualizada correctamente."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminada = await _repo.DeleteAsync(id);
        if (!eliminada)
            return NotFound(ApiResponse<object>.Fail("Solicitud no encontrada."));
        return Ok(ApiResponse<object>.Ok(null, "Solicitud eliminada correctamente."));
    }

    [HttpDelete("{id}/fotos/{fotoId}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> DeleteFoto(int id, int fotoId)
    {
        var foto = await _repo.GetFotoAsync(fotoId);
        if (foto is null || foto.SolicitudTasacionId != id)
            return NotFound(ApiResponse<object>.Fail("Foto no encontrada."));

        await _storage.EliminarArchivoAsync(foto.Url);
        await _repo.DeleteFotoAsync(fotoId);

        return Ok(ApiResponse<object>.Ok(null, "Foto eliminada correctamente."));
    }

    private static SolicitudTasacionDto MapToDto(SolicitudTasacion s) => new()
    {
        Id = s.Id,
        Nombre = s.Nombre,
        Apellido = s.Apellido,
        Email = s.Email,
        Telefono = s.Telefono,
        TipoPropiedad = s.TipoPropiedad.ToString(),
        Direccion = s.Direccion,
        Barrio = s.Barrio,
        Ciudad = s.Ciudad,
        SuperficieTotal = s.SuperficieTotal,
        SuperficieCubierta = s.SuperficieCubierta,
        Ambientes = s.Ambientes,
        Banios = s.Banios,
        Antiguedad = s.Antiguedad,
        EstadoConservacion = s.EstadoConservacion.ToString(),
        Descripcion = s.Descripcion,
        TipoContactoPreferido = s.TipoContactoPreferido.ToString(),
        Estado = s.Estado.ToString(),
        NotasInternas = s.NotasInternas,
        ValorEstimado = s.ValorEstimado,
        AgenteId = s.AgenteId,
        NombreAgente = s.Agente is not null ? $"{s.Agente.User?.Nombre} {s.Agente.User?.Apellido}" : null,
        Fotos = s.Fotos.Select(f => new FotoSolicitudDto
        {
            Id = f.Id,
            Url = f.Url,
            NombreArchivo = f.NombreArchivo,
            FechaSubida = f.FechaSubida
        }).ToList(),
        FechaCreacion = s.FechaCreacion,
        FechaActualizacion = s.FechaActualizacion
    };
}
