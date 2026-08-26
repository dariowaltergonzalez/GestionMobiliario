using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/contratos/{contratoId}/documentos")]
[Authorize]
public class DocumentosContratoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storage;
    private readonly ITenantService _tenantService;

    private static readonly long MaxTamanoBytes = 20 * 1024 * 1024; // 20 MB

    public DocumentosContratoController(ApplicationDbContext context, IStorageService storage, ITenantService tenantService)
    {
        _context = context;
        _storage = storage;
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int contratoId)
    {
        var existe = await _context.Contratos.AnyAsync(c => c.Id == contratoId);
        if (!existe) return NotFound(ApiResponse<object>.Fail("Contrato no encontrado."));

        var docs = await _context.DocumentosContrato
            .Where(d => d.ContratoId == contratoId)
            .OrderByDescending(d => d.FechaCreacion)
            .Select(d => new DocumentoContratoDto
            {
                Id             = d.Id,
                ContratoId     = d.ContratoId,
                NombreOriginal = d.NombreOriginal,
                TipoMime       = d.TipoMime,
                TamanoBytes    = d.TamanoBytes,
                Descripcion    = d.Descripcion,
                FechaCreacion  = d.FechaCreacion,
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<DocumentoContratoDto>>.Ok(docs));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operador")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload(int contratoId, IFormFile archivo, [FromForm] string? descripcion)
    {
        var contrato = await _context.Contratos.FindAsync(contratoId);
        if (contrato is null) return NotFound(ApiResponse<object>.Fail("Contrato no encontrado."));

        if (archivo is null || archivo.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No se recibió ningún archivo."));

        if (archivo.Length > MaxTamanoBytes)
            return BadRequest(ApiResponse<object>.Fail("El archivo supera el tamaño máximo de 20 MB."));

        var tenantId = _tenantService.TenantId ?? 0;
        string url;
        using (var stream = archivo.OpenReadStream())
            url = await _storage.GuardarArchivoAsync(stream, archivo.FileName, $"{tenantId}/documentos-contrato/{contratoId}");

        var doc = new DocumentoContrato
        {
            ContratoId     = contratoId,
            NombreOriginal = archivo.FileName,
            NombreArchivo  = archivo.FileName,
            RutaRelativa   = url,
            TipoMime       = archivo.ContentType,
            TamanoBytes    = archivo.Length,
            Descripcion    = descripcion?.Trim(),
            Activo         = true,
            FechaCreacion  = DateTime.UtcNow,
            FechaActualizacion = DateTime.UtcNow,
        };

        _context.DocumentosContrato.Add(doc);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<DocumentoContratoDto>.Ok(new DocumentoContratoDto
        {
            Id             = doc.Id,
            ContratoId     = doc.ContratoId,
            NombreOriginal = doc.NombreOriginal,
            TipoMime       = doc.TipoMime,
            TamanoBytes    = doc.TamanoBytes,
            Descripcion    = doc.Descripcion,
            FechaCreacion  = doc.FechaCreacion,
        }, "Documento subido correctamente."));
    }

    [HttpGet("{docId}")]
    public async Task<IActionResult> Download(int contratoId, int docId)
    {
        var doc = await _context.DocumentosContrato
            .FirstOrDefaultAsync(d => d.Id == docId && d.ContratoId == contratoId);

        if (doc is null) return NotFound(ApiResponse<object>.Fail("Documento no encontrado."));

        // RutaRelativa guarda la URL completa (relativa en disco local, absoluta en Cloudinary) —
        // redirigimos en vez de leer bytes, así funciona igual sin importar dónde vive el archivo.
        return Redirect(doc.RutaRelativa);
    }

    [HttpDelete("{docId}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Delete(int contratoId, int docId)
    {
        var doc = await _context.DocumentosContrato
            .FirstOrDefaultAsync(d => d.Id == docId && d.ContratoId == contratoId);

        if (doc is null) return NotFound(ApiResponse<object>.Fail("Documento no encontrado."));

        // Baja lógica + borrar archivo físico
        doc.Activo = false;
        doc.FechaActualizacion = DateTime.UtcNow;

        await _storage.EliminarArchivoAsync(doc.RutaRelativa);

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Documento eliminado correctamente."));
    }
}
