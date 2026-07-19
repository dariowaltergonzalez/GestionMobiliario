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
    private readonly IWebHostEnvironment _env;
    private readonly ITenantService _tenantService;

    private static readonly string[] ExtensionesPermitidas = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] ExtensionesVideoPermitidas = [".mp4", ".mov", ".webm"];
    private const long TamanoMaximoBytes = 10 * 1024 * 1024;
    private const long TamanoMaximoVideoBytes = 200L * 1024 * 1024;

    public PropiedadesController(
        IPropiedadRepository repo,
        IPropietarioRepository propietarioRepo,
        IWebHostEnvironment env,
        ITenantService tenantService)
    {
        _repo = repo;
        _propietarioRepo = propietarioRepo;
        _env = env;
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? buscar,
        [FromQuery] TipoPropiedad? tipo,
        [FromQuery] EstadoPropiedad? estado,
        [FromQuery] TipoOperacion? operacion,
        [FromQuery] int? propietarioId)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, buscar, tipo, estado, propietarioId, operacion);
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
            PrecioVenta = p.PrecioVenta,
            PropietarioNombre = $"{p.Propietario.Apellido}, {p.Propietario.Nombre}"
        });
        return Ok(ApiResponse<IEnumerable<PropiedadComboDto>>.Ok(dtos));
    }

    [HttpGet("para-reserva")]
    public async Task<IActionResult> GetParaReserva()
    {
        var lista = await _repo.GetDisponiblesAsync();
        var dtos = lista.Select(MapParaCombo);
        return Ok(ApiResponse<IEnumerable<object>>.Ok(dtos));
    }

    [HttpGet("para-contrato")]
    public async Task<IActionResult> GetParaContrato()
    {
        var paginacion = new PaginationParams { Pagina = 1, Tamano = 10000 };
        var resultado = await _repo.GetPagedAsync(paginacion, null, null, null);
        var dtos = resultado.Items.Select(MapParaCombo);
        return Ok(ApiResponse<IEnumerable<object>>.Ok(dtos));
    }

    [HttpGet("publicas")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicas()
    {
        var lista = await _repo.GetPublicasAsync();
        var dtos = lista.Select(MapToPublicaDto);
        return Ok(ApiResponse<IEnumerable<PropiedadPublicaDto>>.Ok(dtos));
    }

    [HttpGet("publica/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicaById(int id)
    {
        var p = await _repo.GetPublicaByIdAsync(id);
        if (p is null) return NotFound(ApiResponse<PropiedadPublicaDto>.Fail("Propiedad no encontrada."));
        return Ok(ApiResponse<PropiedadPublicaDto>.Ok(MapToPublicaDto(p)));
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

        ValidarPrecios(request.Operacion, request.PrecioAlquiler, request.PrecioVenta, out var errorPrecio);
        if (errorPrecio is not null)
            return BadRequest(ApiResponse<PropiedadDto>.Fail(errorPrecio));

        var propietario = await _propietarioRepo.GetByIdAsync(request.PropietarioId);
        if (propietario is null)
            return BadRequest(ApiResponse<PropiedadDto>.Fail("El propietario indicado no existe."));

        var entidad = new Propiedad
        {
            Tipo = request.Tipo,
            Operacion = request.Operacion,
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
            PrecioVenta = request.PrecioVenta,
            Expensas = request.Expensas,
            Estado = request.Estado,
            EstadoConservacion = request.EstadoConservacion,
            Cochera = request.Cochera,
            Antiguedad = request.Antiguedad,
            TieneCalefaccion = request.TieneCalefaccion,
            AceptaMascotas = request.AceptaMascotas,
            TienePiscina = request.TienePiscina,
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

        ValidarPrecios(request.Operacion, request.PrecioAlquiler, request.PrecioVenta, out var errorPrecio);
        if (errorPrecio is not null)
            return BadRequest(ApiResponse<PropiedadDto>.Fail(errorPrecio));

        var propietario = await _propietarioRepo.GetByIdAsync(request.PropietarioId);
        if (propietario is null)
            return BadRequest(ApiResponse<PropiedadDto>.Fail("El propietario indicado no existe."));

        existente.Tipo = request.Tipo;
        existente.Operacion = request.Operacion;
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
        existente.PrecioVenta = request.PrecioVenta;
        existente.Expensas = request.Expensas;
        existente.Estado = request.Estado;
        existente.EstadoConservacion = request.EstadoConservacion;
        existente.Cochera = request.Cochera;
        existente.Antiguedad = request.Antiguedad;
        existente.TieneCalefaccion = request.TieneCalefaccion;
        existente.AceptaMascotas = request.AceptaMascotas;
        existente.TienePiscina = request.TienePiscina;
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

    private static void ValidarPrecios(TipoOperacion operacion, decimal? precioAlquiler, decimal? precioVenta, out string? error)
    {
        error = operacion switch
        {
            TipoOperacion.Alquiler when (precioAlquiler is null || precioAlquiler <= 0)
                => "El precio de alquiler es requerido y debe ser mayor a cero.",
            TipoOperacion.Venta when (precioVenta is null || precioVenta <= 0)
                => "El precio de venta es requerido y debe ser mayor a cero.",
            TipoOperacion.AlquilerOVenta when (precioAlquiler is null || precioAlquiler <= 0) && (precioVenta is null || precioVenta <= 0)
                => "Debe ingresar al menos un precio (alquiler o venta).",
            _ => null
        };
    }

    private static PropiedadDto MapToDto(Propiedad p) => new()
    {
        Id = p.Id,
        Codigo = p.Codigo,
        Tipo = p.Tipo,
        Operacion = p.Operacion,
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
        PrecioVenta = p.PrecioVenta,
        Expensas = p.Expensas,
        Estado = p.Estado,
        EstadoConservacion = p.EstadoConservacion,
        Cochera = p.Cochera,
        Antiguedad = p.Antiguedad,
        TieneCalefaccion = p.TieneCalefaccion,
        AceptaMascotas = p.AceptaMascotas,
        TienePiscina = p.TienePiscina,
        NroCatastro = p.NroCatastro,
        Descripcion = p.Descripcion,
        Notas = p.Notas,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion,
        PropietarioId = p.PropietarioId,
        PropietarioNombre = $"{p.Propietario.Nombre} {p.Propietario.Apellido}",
        VideoUrl = p.VideoUrl,
        Fotos = p.Fotos.OrderBy(f => f.Orden).Select(f => new FotoPropiedadDto
        {
            Id = f.Id,
            Url = f.Url,
            NombreArchivo = f.NombreArchivo,
            EsPrincipal = f.EsPrincipal,
            Orden = f.Orden
        }).ToList()
    };

    private static object MapParaCombo(Propiedad p) => new
    {
        id = p.Id,
        direccion = $"[{p.Codigo}] {p.Tipo} — {p.Direccion}{(p.Barrio != null ? ", " + p.Barrio : "")}",
        propietarioId = p.Propietario.Id,
        propietarioNombre = p.Propietario.Nombre,
        propietarioApellido = p.Propietario.Apellido,
        propietarioDni = p.Propietario.Dni,
        propietarioTelefono = p.Propietario.Telefono,
        propietarioEmail = p.Propietario.Email,
        propietarioDireccion = p.Propietario.Direccion,
        propietarioBanco = p.Propietario.Banco,
        propietarioCbu = p.Propietario.CBU,
        propietarioCuit = p.Propietario.Cuit,
    };

    private static PropiedadPublicaDto MapToPublicaDto(Propiedad p) => new()
    {
        Id = p.Id,
        Codigo = p.Codigo,
        TipoNombre = p.Tipo.ToString(),
        OperacionNombre = p.Operacion.ToString(),
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
        PrecioVenta = p.PrecioVenta,
        Expensas = p.Expensas,
        Cochera = p.Cochera,
        TieneCalefaccion = p.TieneCalefaccion,
        AceptaMascotas = p.AceptaMascotas,
        Antiguedad = p.Antiguedad,
        EstadoConservacionNombre = p.EstadoConservacion?.ToString(),
        Descripcion = p.Descripcion,
        VideoUrl = p.VideoUrl,
        FotoPrincipalUrl = p.Fotos.FirstOrDefault(f => f.EsPrincipal)?.Url ?? p.Fotos.OrderBy(f => f.Orden).FirstOrDefault()?.Url,
        FotosUrls = p.Fotos.OrderBy(f => f.Orden).Select(f => f.Url).ToList()
    };

    // ---------- Fotos ----------

    [HttpPost("{id}/fotos")]
    public async Task<IActionResult> SubirFotos(int id, [FromForm] List<IFormFile> fotos)
    {
        var propiedad = await _repo.GetByIdAsync(id);
        if (propiedad is null)
            return NotFound(ApiResponse<object>.Fail("Propiedad no encontrada."));

        if (fotos is null || fotos.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("Debe enviar al menos una foto."));

        if (fotos.Count > 20)
            return BadRequest(ApiResponse<object>.Fail("Máximo 20 fotos por propiedad."));

        var tenantId = _tenantService.TenantId ?? 0;
        var carpeta = Path.Combine(_env.ContentRootPath, "FotosPropiedad", tenantId.ToString(), id.ToString());
        Directory.CreateDirectory(carpeta);

        var esPrimeraFoto = !propiedad.Fotos.Any();
        var ordenBase = propiedad.Fotos.Any() ? propiedad.Fotos.Max(f => f.Orden) + 1 : 1;
        var resultado = new List<FotoPropiedadDto>();

        for (var i = 0; i < fotos.Count; i++)
        {
            var archivo = fotos[i];
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
                return BadRequest(ApiResponse<object>.Fail($"Formato no permitido: {extension}. Use JPG, PNG o WebP."));
            if (archivo.Length > TamanoMaximoBytes)
                return BadRequest(ApiResponse<object>.Fail($"El archivo {archivo.FileName} supera el límite de 10 MB."));

            var nuevoNombre = $"{Guid.NewGuid()}{extension}";
            var rutaFisica = Path.Combine(carpeta, nuevoNombre);
            using (var stream = new FileStream(rutaFisica, FileMode.Create))
                await archivo.CopyToAsync(stream);

            var url = $"/fotos-propiedad/{tenantId}/{id}/{nuevoNombre}";
            var foto = await _repo.AddFotoAsync(new FotoPropiedad
            {
                PropiedadId = id,
                Url = url,
                NombreArchivo = archivo.FileName,
                EsPrincipal = esPrimeraFoto && i == 0,
                Orden = ordenBase + i,
                TenantId = tenantId
            });

            resultado.Add(new FotoPropiedadDto
            {
                Id = foto.Id,
                Url = foto.Url,
                NombreArchivo = foto.NombreArchivo,
                EsPrincipal = foto.EsPrincipal,
                Orden = foto.Orden
            });
        }

        return Ok(ApiResponse<List<FotoPropiedadDto>>.Ok(resultado, $"{resultado.Count} foto(s) subida(s) correctamente."));
    }

    [HttpPut("{id}/fotos/{fotoId}/principal")]
    public async Task<IActionResult> SetFotoPrincipal(int id, int fotoId)
    {
        var foto = await _repo.GetFotoAsync(id, fotoId);
        if (foto is null) return NotFound(ApiResponse<object>.Fail("Foto no encontrada."));
        await _repo.SetFotoPrincipalAsync(id, fotoId);
        return Ok(ApiResponse<object>.Ok(null, "Foto principal actualizada."));
    }

    [HttpDelete("{id}/fotos/{fotoId}")]
    public async Task<IActionResult> DeleteFoto(int id, int fotoId)
    {
        var foto = await _repo.GetFotoAsync(id, fotoId);
        if (foto is null) return NotFound(ApiResponse<object>.Fail("Foto no encontrada."));

        var tenantId = _tenantService.TenantId ?? 0;
        var rutaFisica = Path.Combine(_env.ContentRootPath, "FotosPropiedad",
            tenantId.ToString(), id.ToString(), Path.GetFileName(foto.Url));
        if (System.IO.File.Exists(rutaFisica)) System.IO.File.Delete(rutaFisica);

        await _repo.DeleteFotoAsync(id, fotoId);
        return Ok(ApiResponse<object>.Ok(null, "Foto eliminada correctamente."));
    }

    // ---------- Video ----------

    [HttpPost("{id}/video")]
    public async Task<IActionResult> SubirVideo(int id, IFormFile video)
    {
        var propiedad = await _repo.GetByIdAsync(id);
        if (propiedad is null)
            return NotFound(ApiResponse<object>.Fail("Propiedad no encontrada."));

        if (video is null || video.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("Debe enviar un video."));

        var extension = Path.GetExtension(video.FileName).ToLowerInvariant();
        if (!ExtensionesVideoPermitidas.Contains(extension))
            return BadRequest(ApiResponse<object>.Fail($"Formato no permitido: {extension}. Use MP4, MOV o WebM."));

        if (video.Length > TamanoMaximoVideoBytes)
            return BadRequest(ApiResponse<object>.Fail("El video supera el límite de 200 MB."));

        var tenantId = _tenantService.TenantId ?? 0;
        var carpeta = Path.Combine(_env.ContentRootPath, "VideosPropiedad", tenantId.ToString(), id.ToString());
        Directory.CreateDirectory(carpeta);

        // Borrar video anterior si existe
        if (!string.IsNullOrWhiteSpace(propiedad.VideoUrl))
        {
            var rutaAnterior = Path.Combine(_env.ContentRootPath, "VideosPropiedad",
                tenantId.ToString(), id.ToString(), Path.GetFileName(propiedad.VideoUrl));
            if (System.IO.File.Exists(rutaAnterior)) System.IO.File.Delete(rutaAnterior);
        }

        var nuevoNombre = $"{Guid.NewGuid()}{extension}";
        var rutaFisica = Path.Combine(carpeta, nuevoNombre);
        using (var stream = new FileStream(rutaFisica, FileMode.Create))
            await video.CopyToAsync(stream);

        var url = $"/videos-propiedad/{tenantId}/{id}/{nuevoNombre}";
        await _repo.SetVideoUrlAsync(id, url);

        return Ok(ApiResponse<object>.Ok(new { videoUrl = url }, "Video subido correctamente."));
    }

    [HttpDelete("{id}/video")]
    public async Task<IActionResult> DeleteVideo(int id)
    {
        var propiedad = await _repo.GetByIdAsync(id);
        if (propiedad is null)
            return NotFound(ApiResponse<object>.Fail("Propiedad no encontrada."));

        if (!string.IsNullOrWhiteSpace(propiedad.VideoUrl))
        {
            var tenantId = _tenantService.TenantId ?? 0;
            var rutaFisica = Path.Combine(_env.ContentRootPath, "VideosPropiedad",
                tenantId.ToString(), id.ToString(), Path.GetFileName(propiedad.VideoUrl));
            if (System.IO.File.Exists(rutaFisica)) System.IO.File.Delete(rutaFisica);
        }

        await _repo.SetVideoUrlAsync(id, null);
        return Ok(ApiResponse<object>.Ok(null, "Video eliminado correctamente."));
    }
}
