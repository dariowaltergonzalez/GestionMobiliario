using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/liquidaciones")]
[Authorize]
public class LiquidacionesController : ControllerBase
{
    private readonly ILiquidacionRepository _repo;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LiquidacionesController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStorageService _storage;
    private readonly IReciboIaService _reciboIa;
    private readonly ITenantService _tenantService;

    private static readonly string[] ExtensionesComprobantePermitidas = [".jpg", ".jpeg", ".png", ".webp", ".heic"];
    private const long TamanoMaximoComprobanteBytes = 10 * 1024 * 1024; // 10 MB

    public LiquidacionesController(
        ILiquidacionRepository repo,
        ApplicationDbContext context,
        ILogger<LiquidacionesController> logger,
        IServiceScopeFactory scopeFactory,
        IStorageService storage,
        IReciboIaService reciboIa,
        ITenantService tenantService)
    {
        _storage = storage;
        _reciboIa = reciboIa;
        _tenantService = tenantService;
        _repo = repo;
        _context = context;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] EstadoLiquidacion? estado,
        [FromQuery] int? propietarioId,
        [FromQuery] string? buscar)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, estado, propietarioId, buscar);
        var paginado = new PagedResult<LiquidacionDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<LiquidacionDto>>.Ok(paginado));
    }

    [HttpGet("metricas")]
    public async Task<IActionResult> GetMetricas()
    {
        var ahora = DateTime.UtcNow;

        var pendientesQuery = _context.Liquidaciones.Where(l => l.Estado != EstadoLiquidacion.Liquidado);
        var pendientesCount = await pendientesQuery.CountAsync();
        var montoPendienteTotal = await pendientesQuery
            .Select(l => l.MontoALiquidar - l.Abonos.Where(a => a.Activo).Sum(a => (decimal?)a.Monto ?? 0))
            .SumAsync();

        var liquidadasMesCount = await _context.Liquidaciones.CountAsync(l =>
            l.Estado == EstadoLiquidacion.Liquidado &&
            l.FechaLiquidacion.HasValue &&
            l.FechaLiquidacion.Value.Month == ahora.Month &&
            l.FechaLiquidacion.Value.Year == ahora.Year);
        var montoLiquidadoMes = await _context.Liquidaciones
            .Where(l => l.Estado == EstadoLiquidacion.Liquidado &&
                        l.FechaLiquidacion.HasValue &&
                        l.FechaLiquidacion.Value.Month == ahora.Month &&
                        l.FechaLiquidacion.Value.Year == ahora.Year)
            .SumAsync(l => (decimal?)l.MontoALiquidar) ?? 0;

        return Ok(ApiResponse<LiquidacionMetricasDto>.Ok(new LiquidacionMetricasDto
        {
            PendientesCount = pendientesCount,
            MontoPendienteTotal = montoPendienteTotal,
            LiquidadasMesCount = liquidadasMesCount,
            MontoLiquidadoMes = montoLiquidadoMes,
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var liquidacion = await _repo.GetByIdAsync(id);
        if (liquidacion is null) return NotFound(ApiResponse<LiquidacionDto>.Fail("Liquidación no encontrada."));
        return Ok(ApiResponse<LiquidacionDto>.Ok(MapToDto(liquidacion)));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var liquidacion = await _repo.GetByIdAsync(id);
        if (liquidacion is null) return NotFound(ApiResponse<string>.Fail("Liquidación no encontrada."));

        var eliminada = await _repo.EliminarAsync(id);
        if (!eliminada)
            return BadRequest(ApiResponse<string>.Fail(
                "No se puede eliminar: tiene abonos cargados. Eliminá los abonos primero."));

        return Ok(ApiResponse<string>.Ok("Liquidación eliminada."));
    }

    // Sube la foto del comprobante y le pide a la IA que extraiga los datos — no depende de una
    // Liquidacion puntual (se usa antes de saber a cuál abono va a terminar asociada). La imagen
    // queda guardada siempre que la subida en sí funcione, aunque la IA no encuentre nada o falle
    // (ver GeminiReciboIaService.ExtraerDatosAsync, que nunca tira excepción hacia afuera).
    [HttpPost("comprobantes/extraer")]
    [Authorize(Roles = "Admin,Operador")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ExtraerComprobante(IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No se recibió ningún archivo."));

        if (archivo.Length > TamanoMaximoComprobanteBytes)
            return BadRequest(ApiResponse<object>.Fail("El archivo supera el tamaño máximo de 10 MB."));

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesComprobantePermitidas.Contains(extension))
            return BadRequest(ApiResponse<object>.Fail("Formato no soportado. Usá JPG, PNG, WEBP o HEIC."));

        var tenantId = _tenantService.TenantId ?? 0;

        string comprobanteUrl;
        using (var streamParaGuardar = archivo.OpenReadStream())
            comprobanteUrl = await _storage.GuardarArchivoAsync(streamParaGuardar, archivo.FileName, $"{tenantId}/comprobantes");

        DatosComprobante datos;
        using (var streamParaIa = archivo.OpenReadStream())
            datos = await _reciboIa.ExtraerDatosAsync(streamParaIa, archivo.ContentType);

        return Ok(ApiResponse<ExtraccionComprobanteDto>.Ok(new ExtraccionComprobanteDto
        {
            ComprobanteUrl = comprobanteUrl,
            Monto = datos.Monto,
            Fecha = datos.Fecha,
            CbuCvuDestino = datos.CbuCvuDestino,
            EntidadDestino = datos.EntidadDestino,
            NumeroOperacion = datos.NumeroOperacion,
        }, "Comprobante subido."));
    }

    [HttpPost("{id}/abonos")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> AgregarAbono(int id, [FromBody] AbonoLiquidacionRequest request)
    {
        var liquidacion = await _repo.GetByIdAsync(id);
        if (liquidacion is null) return NotFound(ApiResponse<LiquidacionDto>.Fail("Liquidación no encontrada."));

        var error = ValidarAbono(liquidacion, request, abonoIdAEditar: null);
        if (error is not null) return BadRequest(ApiResponse<LiquidacionDto>.Fail(error));

        var abono = new LiquidacionAbono
        {
            Monto = request.Monto,
            Fecha = request.Fecha ?? DateTime.UtcNow,
            Medio = (MedioPago)request.Medio,
            CbuCvuDestino = request.CbuCvuDestino?.Trim(),
            EntidadDestino = request.EntidadDestino?.Trim(),
            NumeroOperacion = request.NumeroOperacion?.Trim(),
            Observaciones = request.Observaciones?.Trim(),
            ComprobanteUrl = request.ComprobanteUrl?.Trim(),
        };

        var actualizada = await _repo.AgregarAbonoAsync(id, abono);

        await NotificarAvisoLiquidacionAsync(liquidacion, abono, liquidacion.TenantId);

        return Ok(ApiResponse<LiquidacionDto>.Ok(MapToDto(actualizada!), "Abono registrado."));
    }

    [HttpPut("{id}/abonos/{abonoId}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> EditarAbono(int id, int abonoId, [FromBody] AbonoLiquidacionRequest request)
    {
        var liquidacion = await _repo.GetByIdAsync(id);
        if (liquidacion is null) return NotFound(ApiResponse<LiquidacionDto>.Fail("Liquidación no encontrada."));

        var error = ValidarAbono(liquidacion, request, abonoIdAEditar: abonoId);
        if (error is not null) return BadRequest(ApiResponse<LiquidacionDto>.Fail(error));

        var datos = new LiquidacionAbono
        {
            Monto = request.Monto,
            Fecha = request.Fecha ?? DateTime.UtcNow,
            Medio = (MedioPago)request.Medio,
            CbuCvuDestino = request.CbuCvuDestino?.Trim(),
            EntidadDestino = request.EntidadDestino?.Trim(),
            NumeroOperacion = request.NumeroOperacion?.Trim(),
            Observaciones = request.Observaciones?.Trim(),
            ComprobanteUrl = request.ComprobanteUrl?.Trim(),
        };

        var actualizada = await _repo.ActualizarAbonoAsync(id, abonoId, datos);
        if (actualizada is null) return NotFound(ApiResponse<LiquidacionDto>.Fail("Abono no encontrado."));
        return Ok(ApiResponse<LiquidacionDto>.Ok(MapToDto(actualizada), "Abono actualizado."));
    }

    [HttpDelete("{id}/abonos/{abonoId}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> EliminarAbono(int id, int abonoId)
    {
        var actualizada = await _repo.EliminarAbonoAsync(id, abonoId);
        if (actualizada is null) return NotFound(ApiResponse<LiquidacionDto>.Fail("Abono no encontrado."));
        return Ok(ApiResponse<LiquidacionDto>.Ok(MapToDto(actualizada), "Abono eliminado."));
    }

    private async Task NotificarAvisoLiquidacionAsync(Liquidacion liquidacion, LiquidacionAbono abono, int tenantId)
    {
        var propietarioRefId = liquidacion.Pago.Contrato.PropietarioRefId;
        if (!propietarioRefId.HasValue) return;

        var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst("nombre") is { } n && User.FindFirst("apellido") is { } a
            ? $"{n.Value} {a.Value}"
            : User.Identity?.Name;

        // Se lee acá (scope del request, tenant activo ya resuelto) porque en el Task.Run de abajo
        // no hay HttpContext para que ITenantService funcione.
        var empresa = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();
        var nombreEmpresa = empresa?.NombreComercial ?? "GestionInmobiliaria";

        var contratoCodigo = liquidacion.Pago.Contrato.Codigo;
        var propiedadDireccion = liquidacion.Pago.Contrato.Propiedad.Direccion;
        var moneda = liquidacion.Pago.Contrato.Moneda.ToString();
        var propietarioNombre = $"{liquidacion.Pago.Contrato.LocadorNombre} {liquidacion.Pago.Contrato.LocadorApellido}";
        var periodo = liquidacion.Pago.Periodo;
        var numeroCuota = liquidacion.Pago.NumeroCuota;
        var liquidacionId = liquidacion.Id;

        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ctx          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificacion = scope.ServiceProvider.GetRequiredService<INotificacionService>();

            try
            {
                var propietario = await ctx.Propietarios.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == propietarioRefId.Value && p.TenantId == tenantId);
                if (propietario is null) return;

                var periodoTexto = periodo.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-AR")).ToUpper();
                var asunto = $"Transferencia realizada — {contratoCodigo} — {periodoTexto}";
                var cuerpo = BuildAvisoLiquidacionEmailBody(
                    nombreEmpresa, propietarioNombre, contratoCodigo, propiedadDireccion, moneda,
                    periodoTexto, numeroCuota, abono);
                var contexto = new NotificacionContexto
                {
                    TenantId = tenantId,
                    UserId = userId,
                    UserName = userName,
                    EntidadRelacionada = "EmailAvisoLiquidacion",
                    EntidadRelacionadaId = liquidacionId.ToString(),
                    DatosAdicionales = new { contrato = contratoCodigo, montoAbono = abono.Monto },
                };

                await notificacion.NotificarAsync(propietario, "AvisoLiquidacion", asunto, cuerpo, contexto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar el aviso de liquidación. LiquidacionId={LiquidacionId}", liquidacionId);
            }
        });
    }

    private static string BuildAvisoLiquidacionEmailBody(
        string empresa, string propietarioNombre, string contratoCodigo, string propiedadDireccion,
        string moneda, string periodoTexto, int numeroCuota, LiquidacionAbono abono)
    {
        var monedaSimbolo = moneda == "USD" ? "U$S" : "$";
        var medioTexto = abono.Medio switch
        {
            MedioPago.Efectivo => "Efectivo",
            MedioPago.Debito   => "Transferencia / Débito",
            MedioPago.Credito  => "Tarjeta de crédito",
            MedioPago.Cheque   => "Cheque",
            _                  => abono.Medio.ToString(),
        };

        return $"""
            <!DOCTYPE html><html><head><meta charset="utf-8"></head>
            <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:0;">
              <div style="background:#1e3a5f;padding:20px 24px;border-radius:8px 8px 0 0;">
                <h1 style="color:white;margin:0;font-size:17px;">{empresa}</h1>
              </div>
              <div style="background:#f8f9fa;padding:24px;border:1px solid #e0e0e0;border-top:none;border-radius:0 0 8px 8px;">
                <h2 style="color:#1e3a5f;margin:0 0 8px 0;">Transferencia realizada</h2>
                <p>Estimado/a <strong>{propietarioNombre}</strong>,</p>
                <p>Le informamos que se realizó una transferencia correspondiente a la liquidación de su
                propiedad. Este servicio corresponde a:</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;">
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;width:40%;">Propiedad</td><td style="padding:10px;">{propiedadDireccion}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Contrato</td><td style="padding:10px;">{contratoCodigo}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Período</td><td style="padding:10px;">{periodoTexto}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Cuota N°</td><td style="padding:10px;">{numeroCuota}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Monto transferido</td><td style="padding:10px;color:#2e7d32;font-weight:bold;font-size:16px;">{monedaSimbolo} {abono.Monto:N2}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Fecha</td><td style="padding:10px;">{abono.Fecha:dd/MM/yyyy}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Medio</td><td style="padding:10px;">{medioTexto}</td></tr>
                  {(!string.IsNullOrWhiteSpace(abono.EntidadDestino) ? $"""<tr><td style="padding:10px;font-weight:bold;">Entidad destino</td><td style="padding:10px;">{abono.EntidadDestino}</td></tr>""" : "")}
                  {(!string.IsNullOrWhiteSpace(abono.CbuCvuDestino) ? $"""<tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">CBU/CVU destino</td><td style="padding:10px;">{abono.CbuCvuDestino}</td></tr>""" : "")}
                  {(!string.IsNullOrWhiteSpace(abono.NumeroOperacion) ? $"""<tr><td style="padding:10px;font-weight:bold;">N° de operación</td><td style="padding:10px;">{abono.NumeroOperacion}</td></tr>""" : "")}
                  {(!string.IsNullOrWhiteSpace(abono.Observaciones) ? $"""<tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Observaciones</td><td style="padding:10px;">{abono.Observaciones}</td></tr>""" : "")}
                </table>
                <hr style="border:none;border-top:1px solid #e0e0e0;margin:20px 0;">
                <p style="color:#666;font-size:12px;">Este mensaje fue enviado automáticamente por {empresa}.</p>
              </div>
            </body></html>
            """;
    }

    private static string? ValidarAbono(Liquidacion liquidacion, AbonoLiquidacionRequest request, int? abonoIdAEditar)
    {
        if (request.Monto <= 0)
            return "El monto del abono debe ser mayor a cero.";

        var sumaOtrosAbonos = liquidacion.Abonos
            .Where(a => a.Activo && a.Id != abonoIdAEditar)
            .Sum(a => a.Monto);
        var restante = liquidacion.MontoALiquidar - sumaOtrosAbonos;

        if (request.Monto > restante)
            return $"El monto supera lo que falta liquidar (quedan {restante:N2}).";

        return null;
    }

    private static LiquidacionDto MapToDto(Liquidacion l)
    {
        var abonosActivos = l.Abonos.Where(a => a.Activo).ToList();
        var montoAbonado = abonosActivos.Sum(a => a.Monto);

        return new LiquidacionDto
        {
            Id = l.Id,
            PagoId = l.PagoId,
            ContratoId = l.Pago.ContratoId,
            ContratoCodigo = l.Pago.Contrato.Codigo,
            PropiedadDireccion = l.Pago.Contrato.Propiedad.Direccion,
            PropietarioRefId = l.Pago.Contrato.PropietarioRefId,
            PropietarioNombre = l.Pago.Contrato.LocadorNombre,
            PropietarioApellido = l.Pago.Contrato.LocadorApellido,
            NumeroCuota = l.Pago.NumeroCuota,
            Periodo = l.Pago.Periodo,
            Moneda = l.Pago.Contrato.Moneda.ToString(),
            MontoCobrado = l.MontoCobrado,
            ComisionPorcentaje = l.ComisionPorcentaje,
            ComisionMonto = l.ComisionMonto,
            MontoComision = l.MontoComision,
            MontoGastos = l.MontoGastos,
            MontoALiquidar = l.MontoALiquidar,
            MontoAbonado = montoAbonado,
            MontoRestante = l.MontoALiquidar - montoAbonado,
            Estado = l.Estado.ToString(),
            FechaLiquidacion = l.FechaLiquidacion,
            Observaciones = l.Observaciones,
            FechaCreacion = l.FechaCreacion,
            Abonos = abonosActivos.Select(a => new LiquidacionAbonoDto
            {
                Id = a.Id,
                Monto = a.Monto,
                Fecha = a.Fecha,
                Medio = a.Medio.ToString(),
                CbuCvuDestino = a.CbuCvuDestino,
                EntidadDestino = a.EntidadDestino,
                NumeroOperacion = a.NumeroOperacion,
                Observaciones = a.Observaciones,
                ComprobanteUrl = a.ComprobanteUrl,
            }).OrderByDescending(a => a.Fecha).ToList(),
            Gastos = l.Gastos.Select(g => new LiquidacionGastoDto
            {
                Id = g.Id,
                Categoria = g.Categoria.ToString(),
                Descripcion = g.Descripcion,
                Monto = g.Monto,
            }).ToList(),
        };
    }
}
