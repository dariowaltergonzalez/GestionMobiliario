using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IPdfReportService _pdf;
    private readonly IPropietarioRepository _propietarios;
    private readonly IPropiedadRepository _propiedades;
    private readonly IEventoAgendaRepository _eventos;
    private readonly ISolicitudTasacionRepository _tasaciones;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ReportesController(
        IPdfReportService pdf,
        IPropietarioRepository propietarios,
        IPropiedadRepository propiedades,
        IEventoAgendaRepository eventos,
        ISolicitudTasacionRepository tasaciones,
        ApplicationDbContext context,
        IWebHostEnvironment env)
    {
        _pdf = pdf;
        _propietarios = propietarios;
        _propiedades = propiedades;
        _eventos = eventos;
        _tasaciones = tasaciones;
        _context = context;
        _env = env;
    }

    [HttpGet("propietarios")]
    public async Task<IActionResult> Propietarios(
        [FromQuery] string? buscar,
        [FromQuery] bool? activo)
    {
        try
        {
            var paginacion = new PaginationParams { Pagina = 1, Tamano = 10000 };
            var resultado = await _propietarios.GetPagedAsync(paginacion, buscar, activo);
            var dtos = resultado.Items.Select(MapPropietario).ToList();
            var config = await BuildConfig("Reporte de Propietarios", BuildFiltrosPropietarios(buscar, activo));
            var bytes = _pdf.GenerarPropietarios(dtos, config);
            return File(bytes, "application/pdf", $"Propietarios_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Error al generar PDF: {ex.Message}"));
        }
    }

    [HttpGet("propiedades")]
    public async Task<IActionResult> Propiedades(
        [FromQuery] string? buscar,
        [FromQuery] TipoPropiedad? tipo,
        [FromQuery] EstadoPropiedad? estado,
        [FromQuery] TipoOperacion? operacion)
    {
        try
        {
            var paginacion = new PaginationParams { Pagina = 1, Tamano = 10000 };
            var resultado = await _propiedades.GetPagedAsync(paginacion, buscar, tipo, estado);
            var dtos = resultado.Items
                .Where(p => operacion == null || p.Operacion == operacion)
                .Select(MapPropiedad).ToList();
            var config = await BuildConfig("Reporte de Propiedades", BuildFiltrosPropiedades(buscar, tipo, estado, operacion));
            var bytes = _pdf.GenerarPropiedades(dtos, config);
            return File(bytes, "application/pdf", $"Propiedades_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Error al generar PDF: {ex.Message}"));
        }
    }

    [HttpGet("agenda")]
    public async Task<IActionResult> Agenda(
        [FromQuery] int? agenteId,
        [FromQuery] EstadoEvento? estado,
        [FromQuery] TipoEvento? tipo)
    {
        try
        {
            var paginacion = new PaginationParams { Pagina = 1, Tamano = 10000 };
            var resultado = await _eventos.GetPagedAsync(paginacion, agenteId, estado, tipo);
            var dtos = resultado.Items.Select(MapEvento).ToList();
            var filtros = BuildFiltrosAgenda(agenteId, estado, tipo);
            var config = await BuildConfig("Reporte de Agenda", filtros);
            var bytes = _pdf.GenerarAgenda(dtos, config);
            return File(bytes, "application/pdf", $"Agenda_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Error al generar PDF: {ex.Message}"));
        }
    }

    [HttpGet("tasaciones")]
    public async Task<IActionResult> Tasaciones(
        [FromQuery] string? buscar,
        [FromQuery] EstadoSolicitudTasacion? estado,
        [FromQuery] int? agenteId)
    {
        try
        {
            var paginacion = new PaginationParams { Pagina = 1, Tamano = 10000 };
            var resultado = await _tasaciones.GetPagedAsync(paginacion, buscar, estado, agenteId);
            var dtos = resultado.Items.Select(MapTasacion).ToList();
            var filtros = BuildFiltrosTasaciones(buscar, estado, agenteId);
            var config = await BuildConfig("Reporte de Tasaciones", filtros);
            var bytes = _pdf.GenerarTasaciones(dtos, config);
            return File(bytes, "application/pdf", $"Tasaciones_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Error al generar PDF: {ex.Message}"));
        }
    }

    private async Task<PdfReportConfig> BuildConfig(string titulo, string? filtros)
    {
        var empresa = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();
        var nombreEmpresa = empresa?.NombreComercial ?? "GestionInmobiliaria";

        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(empresa?.Cuit)) partes.Add($"CUIT: {empresa.Cuit}");
        if (!string.IsNullOrWhiteSpace(empresa?.Telefono)) partes.Add($"Tel: {empresa.Telefono}");
        if (!string.IsNullOrWhiteSpace(empresa?.Email)) partes.Add(empresa.Email);
        if (!string.IsNullOrWhiteSpace(empresa?.Ciudad)) partes.Add(empresa.Ciudad);

        byte[]? logoBytes = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(empresa?.LogoUrl))
            {
                // Intentar resolver el archivo desde la carpeta Logos del servidor
                var fileName = Path.GetFileName(empresa.LogoUrl.TrimEnd('/'));
                var logoPath = Path.Combine(_env.ContentRootPath, "Logos", fileName);
                if (System.IO.File.Exists(logoPath))
                    logoBytes = await System.IO.File.ReadAllBytesAsync(logoPath);
            }
        }
        catch
        {
            // Si no se puede cargar el logo, el PDF se genera sin él
        }

        return new PdfReportConfig
        {
            Titulo = titulo,
            NombreEmpresa = nombreEmpresa,
            Slogan = empresa?.Slogan,
            InfoEmpresa = partes.Count > 0 ? string.Join("  |  ", partes) : null,
            LogoBytes = logoBytes,
            FiltrosAplicados = filtros,
            FechaGeneracion = DateTime.Now,
        };
    }

    private static string? BuildFiltrosPropietarios(string? buscar, bool? activo)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(buscar)) partes.Add($"Búsqueda: \"{buscar}\"");
        if (activo.HasValue) partes.Add(activo.Value ? "Solo activos" : "Solo inactivos");
        return partes.Count > 0 ? string.Join("  ·  ", partes) : null;
    }

    private static string? BuildFiltrosPropiedades(string? buscar, TipoPropiedad? tipo, EstadoPropiedad? estado, TipoOperacion? operacion)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(buscar)) partes.Add($"Búsqueda: \"{buscar}\"");
        if (tipo.HasValue) partes.Add($"Tipo: {tipo}");
        if (estado.HasValue) partes.Add($"Estado: {estado}");
        if (operacion.HasValue) partes.Add($"Operación: {operacion}");
        return partes.Count > 0 ? string.Join("  ·  ", partes) : null;
    }

    private static string? BuildFiltrosAgenda(int? agenteId, EstadoEvento? estado, TipoEvento? tipo)
    {
        var partes = new List<string>();
        if (agenteId.HasValue) partes.Add($"Agente ID: {agenteId}");
        if (estado.HasValue) partes.Add($"Estado: {estado}");
        if (tipo.HasValue) partes.Add($"Tipo: {tipo}");
        return partes.Count > 0 ? string.Join("  ·  ", partes) : null;
    }

    private static EventoAgendaDto MapEvento(EventoAgenda e) => new()
    {
        Id = e.Id,
        Tipo = e.Tipo.ToString(),
        Estado = e.Estado.ToString(),
        FechaHora = e.FechaHora,
        Notas = e.Notas,
        AgenteId = e.AgenteId,
        AgenteNombre = $"{e.Agente.User.Apellido}, {e.Agente.User.Nombre}",
        PropiedadId = e.PropiedadId,
        PropiedadDireccion = e.Propiedad?.Direccion,
        LeadId = e.LeadId,
        LeadNombre = e.Lead is not null ? $"{e.Lead.Apellido}, {e.Lead.Nombre}" : null,
        InquilinoId = e.InquilinoId,
        InquilinoNombre = e.Inquilino is not null ? $"{e.Inquilino.Apellido}, {e.Inquilino.Nombre}" : null,
    };

    private static PropietarioDto MapPropietario(Propietario p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Apellido = p.Apellido,
        Dni = p.Dni,
        Cuit = p.Cuit,
        Email = p.Email,
        Telefono = p.Telefono,
        Direccion = p.Direccion,
        Activo = p.Activo,
        FechaCreacion = p.FechaCreacion,
        CantidadPropiedades = p.Propiedades?.Count ?? 0,
    };

    private static SolicitudTasacionDto MapTasacion(SolicitudTasacion s) => new()
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
        Fotos = [],
        FechaCreacion = s.FechaCreacion,
        FechaActualizacion = s.FechaActualizacion
    };

    private static string? BuildFiltrosTasaciones(string? buscar, EstadoSolicitudTasacion? estado, int? agenteId)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(buscar)) partes.Add($"Búsqueda: \"{buscar}\"");
        if (estado.HasValue) partes.Add($"Estado: {estado}");
        if (agenteId.HasValue) partes.Add($"Agente ID: {agenteId}");
        return partes.Count > 0 ? string.Join("  ·  ", partes) : null;
    }

    private static PropiedadDto MapPropiedad(Propiedad p) => new()
    {
        Id = p.Id,
        Tipo = p.Tipo,
        Operacion = p.Operacion,
        Direccion = p.Direccion,
        Barrio = p.Barrio,
        Ciudad = p.Ciudad,
        Provincia = p.Provincia,
        Estado = p.Estado,
        PrecioAlquiler = p.PrecioAlquiler,
        PrecioVenta = p.PrecioVenta,
        PropietarioNombre = p.Propietario is not null
            ? $"{p.Propietario.Apellido}, {p.Propietario.Nombre}"
            : string.Empty,
        FechaCreacion = p.FechaCreacion,
    };
}
