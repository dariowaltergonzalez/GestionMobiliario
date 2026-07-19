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
[Route("api/pagos")]
[Authorize]
public class PagosController : ControllerBase
{
    private readonly IPagoRepository _pagos;
    private readonly IPdfReportService _pdf;
    private readonly IEmailService _email;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PagosController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PagosController(
        IPagoRepository pagos,
        IPdfReportService pdf,
        IEmailService email,
        ApplicationDbContext context,
        IWebHostEnvironment env,
        ILogger<PagosController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _pagos = pagos;
        _pdf = pdf;
        _email = email;
        _context = context;
        _env = env;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] int? contratoId,
        [FromQuery] EstadoPago? estado,
        [FromQuery] int? mes,
        [FromQuery] int? anio)
    {
        var resultado = await _pagos.GetPagedAsync(paginacion, contratoId, estado, mes, anio);
        var paginado = new PagedResult<PagoListDto>
        {
            Items = resultado.Items.Select(MapToListDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<PagoListDto>>.Ok(paginado));
    }

    [HttpGet("metricas")]
    public async Task<IActionResult> GetMetricas()
    {
        var ahora = DateTime.UtcNow;

        var pendientes    = await _context.Pagos.CountAsync(p => p.Estado == EstadoPago.Pendiente);
        var atrasados     = await _context.Pagos.CountAsync(p => p.Estado == EstadoPago.Atrasado);
        var pagadosMes    = await _context.Pagos.CountAsync(p =>
            p.Estado == EstadoPago.Pagado &&
            p.FechaPago.HasValue &&
            p.FechaPago.Value.Month == ahora.Month &&
            p.FechaPago.Value.Year == ahora.Year);
        var cobradoMes    = await _context.Pagos
            .Where(p => p.Estado == EstadoPago.Pagado &&
                        p.FechaPago.HasValue &&
                        p.FechaPago.Value.Month == ahora.Month &&
                        p.FechaPago.Value.Year == ahora.Year)
            .SumAsync(p => (decimal?)(p.MontoPagado ?? 0)) ?? 0;
        var totalPendiente = await _context.Pagos
            .Where(p => p.Estado == EstadoPago.Pendiente || p.Estado == EstadoPago.Atrasado)
            .SumAsync(p => (decimal?)p.MontoEsperado) ?? 0;

        return Ok(ApiResponse<PagoMetricasDto>.Ok(new PagoMetricasDto
        {
            PendientesCount    = pendientes,
            AtrasadosCount     = atrasados,
            PagadosMesCount    = pagadosMes,
            MontoCobradoMes    = cobradoMes,
            MontoTotalPendiente = totalPendiente
        }));
    }

    [HttpPut("{contratoId}/pagos/{pagoId}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> UpdatePago(int contratoId, int pagoId, [FromBody] UpdatePagoRequest request)
    {
        var pago = await _pagos.GetByIdConContratoAsync(pagoId);
        if (pago is null || pago.ContratoId != contratoId)
            return NotFound(ApiResponse<PagoDto>.Fail("Pago no encontrado."));

        pago.Estado        = (EstadoPago)request.Estado;
        pago.FechaPago     = request.FechaPago;
        pago.Observaciones = request.Observaciones;
        pago.MontoPagado   = request.Detalles.Any() ? request.Detalles.Sum(d => d.Monto) : null;

        var detalles = request.Detalles.Select(d => new PagoDetalle
        {
            Medio = (MedioPago)d.Medio,
            Monto = d.Monto,
            Referencia = d.Referencia,
            ChequeBanco = d.ChequeBanco,
            ChequeNumero = d.ChequeNumero,
            ChequeFechaVencimiento = string.IsNullOrWhiteSpace(d.ChequeFechaVencimiento)
                ? null : DateTime.Parse(d.ChequeFechaVencimiento),
            Activo = true,
        }).ToList();

        var actualizado = await _pagos.UpdateWithDetallesAsync(pago, detalles);

        if (pago.Estado == EstadoPago.Pagado && !string.IsNullOrWhiteSpace(pago.Contrato?.LocadorEmail))
        {
            var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst("nombre") is { } n && User.FindFirst("apellido") is { } a
                ? $"{n.Value} {a.Value}"
                : User.Identity?.Name;
            var tenantId = pago.TenantId;

            // Capturar todo lo necesario antes del Task.Run (scope del request, tenant activo)
            var contratoDto     = MapContratoDto(pago.Contrato!);
            var pagoDto         = MapPagoDto(actualizado);
            var locadorEmail    = pago.Contrato!.LocadorEmail!;
            var locadorNombre   = $"{pago.Contrato.LocadorNombre} {pago.Contrato.LocadorApellido}";
            var asuntoDireccion = pago.Contrato.Propiedad.Direccion;
            var contratoCodigo  = pago.Contrato.Codigo;
            var periodo         = pago.Periodo.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-AR")).ToUpper();
            var monto           = $"$ {(pago.MontoPagado ?? pago.MontoEsperado):N0}";
            var detallesSnap    = detalles.ToList();
            // La config de empresa se carga aquí donde el tenant está disponible
            var pdfConfig       = await BuildConfig();

            _ = Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var ctx      = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var pdfSvc   = scope.ServiceProvider.GetRequiredService<IPdfReportService>();
                try
                {
                    var reciboPdf = pdfSvc.GenerarReciboPago(pagoDto, contratoDto, pdfConfig);
                    var cuerpo    = BuildEmailBody(pdfConfig.NombreEmpresa, contratoDto, pagoDto, detallesSnap, periodo, monto);
                    var fileName  = $"Recibo_{contratoCodigo}_{periodo.Replace(" ", "_")}.pdf";

                    await emailSvc.EnviarAsync(new EmailMessage
                    {
                        Destinatario       = locadorEmail,
                        NombreDestinatario = locadorNombre,
                        Asunto             = $"Cobro registrado — {asuntoDireccion} — {periodo}",
                        Cuerpo             = cuerpo,
                        Adjuntos           = [new EmailAdjunto { NombreArchivo = fileName, Contenido = reciboPdf }]
                    });

                    _logger.LogInformation("Recibo enviado. PagoId={PagoId} Destinatario={Email}", pagoId, locadorEmail);

                    ctx.AuditLogs.Add(new AuditLog
                    {
                        EntityName = "EmailRecibo",
                        Action     = "ENVIADO",
                        EntityId   = pagoId.ToString(),
                        UserId     = userId,
                        UserName   = userName,
                        NewValues  = System.Text.Json.JsonSerializer.Serialize(new { destinatario = locadorEmail, contrato = contratoCodigo, periodo }),
                        Timestamp  = DateTime.UtcNow,
                        TenantId   = tenantId,
                    });
                    await ctx.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar email de recibo. PagoId={PagoId} Destinatario={Email}", pagoId, locadorEmail);
                    try
                    {
                        ctx.AuditLogs.Add(new AuditLog
                        {
                            EntityName = "EmailRecibo",
                            Action     = "ERROR",
                            EntityId   = pagoId.ToString(),
                            UserId     = userId,
                            UserName   = userName,
                            NewValues  = System.Text.Json.JsonSerializer.Serialize(new { destinatario = locadorEmail, error = ex.Message, tipo = ex.GetType().Name, origen = ex.StackTrace?.Split('\n').Take(3).ToArray() }),
                            Timestamp  = DateTime.UtcNow,
                            TenantId   = tenantId,
                        });
                        await ctx.SaveChangesAsync();
                    }
                    catch { }
                }
            });
        }

        return Ok(ApiResponse<PagoDto>.Ok(MapPagoDto(actualizado), "Pago actualizado correctamente."));
    }

    [HttpGet("{contratoId}/pagos/{pagoId}/recibo")]
    public async Task<IActionResult> GetRecibo(int contratoId, int pagoId)
    {
        var pago = await _pagos.GetByIdConContratoAsync(pagoId);
        if (pago is null || pago.ContratoId != contratoId)
            return NotFound(ApiResponse<string>.Fail("Pago no encontrado."));

        try
        {
            var config      = await BuildConfig();
            var contratoDto = MapContratoDto(pago.Contrato!);
            var pagoDto     = MapPagoDto(pago);
            var bytes       = _pdf.GenerarReciboPago(pagoDto, contratoDto, config);
            var periodo     = pago.Periodo.ToString("MMMMyyyy", new System.Globalization.CultureInfo("es-AR"));
            return File(bytes, "application/pdf", $"Recibo_{pago.Contrato!.Codigo}_{periodo}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Error al generar recibo: {ex.Message}"));
        }
    }

    private Task<PdfReportConfig> BuildConfig() =>
        BuildConfigFromContext(_context, _env);

    private static async Task<PdfReportConfig> BuildConfigFromContext(ApplicationDbContext ctx, IWebHostEnvironment env)
    {
        var empresa = await ctx.ConfiguracionEmpresa.FirstOrDefaultAsync();
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(empresa?.Cuit))     partes.Add($"CUIT: {empresa.Cuit}");
        if (!string.IsNullOrWhiteSpace(empresa?.Telefono)) partes.Add($"Tel: {empresa.Telefono}");
        if (!string.IsNullOrWhiteSpace(empresa?.Email))    partes.Add(empresa.Email);
        if (!string.IsNullOrWhiteSpace(empresa?.Ciudad))   partes.Add(empresa.Ciudad);

        byte[]? logoBytes = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(empresa?.LogoUrl))
            {
                var fileName = Path.GetFileName(empresa.LogoUrl.TrimEnd('/'));
                var logoPath = Path.Combine(env.ContentRootPath, "Logos", fileName);
                if (System.IO.File.Exists(logoPath))
                    logoBytes = await System.IO.File.ReadAllBytesAsync(logoPath);
            }
        }
        catch { }

        return new PdfReportConfig
        {
            Titulo          = "Recibo de Pago",
            NombreEmpresa   = empresa?.NombreComercial ?? "GestionInmobiliaria",
            Slogan          = empresa?.Slogan,
            InfoEmpresa     = partes.Count > 0 ? string.Join("  |  ", partes) : null,
            LogoBytes       = logoBytes,
            FechaGeneracion = DateTime.Now,
        };
    }

    private static string BuildEmailBody(string empresa, ContratoDto c, PagoDto p, IEnumerable<PagoDetalle> detalles, string periodo, string monto)
    {
        var fechaPago = p.FechaPago.HasValue
            ? p.FechaPago.Value.ToLocalTime().ToString("dd/MM/yyyy")
            : DateTime.Now.ToString("dd/MM/yyyy");

        var detallesList = detalles.ToList();
        var filasMedios = new System.Text.StringBuilder();
        for (int i = 0; i < detallesList.Count; i++)
        {
            var d = detallesList[i];
            var bg = i % 2 == 0 ? "" : "background:#e8f0fe;";
            var descripcion = d.Medio switch
            {
                MedioPago.Efectivo => "Efectivo",
                MedioPago.Debito   => "Transferencia / Débito",
                MedioPago.Credito  => "Tarjeta de crédito",
                MedioPago.Cheque   => BuildDescripcionCheque(d),
                _                  => d.Medio.ToString()
            };
            if (!string.IsNullOrWhiteSpace(d.Referencia) && d.Medio != MedioPago.Cheque)
                descripcion += $" — {d.Referencia}";
            filasMedios.Append($"<tr style=\"{bg}\"><td style=\"padding:8px;\">{descripcion}</td><td style=\"padding:8px;text-align:right;font-weight:bold;\">$ {d.Monto:N0}</td></tr>");
        }

        return $"""
            <!DOCTYPE html><html><head><meta charset="utf-8"></head>
            <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:0;">
              <div style="background:#1e3a5f;padding:20px 24px;border-radius:8px 8px 0 0;">
                <h1 style="color:white;margin:0;font-size:17px;">{empresa}</h1>
              </div>
              <div style="background:#f8f9fa;padding:24px;border:1px solid #e0e0e0;border-top:none;border-radius:0 0 8px 8px;">
                <h2 style="color:#1e3a5f;margin:0 0 8px 0;">Cobro de alquiler registrado</h2>
                <p>Estimado/a <strong>{c.LocadorNombre} {c.LocadorApellido}</strong>,</p>
                <p>Le informamos que se registró el cobro correspondiente a:</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;">
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;width:40%;">Propiedad</td><td style="padding:10px;">{c.PropiedadDireccion}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Inquilino/a</td><td style="padding:10px;">{c.LocatarioNombre} {c.LocatarioApellido}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Período</td><td style="padding:10px;">{periodo}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Cuota N°</td><td style="padding:10px;">{p.NumeroCuota}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Monto abonado</td><td style="padding:10px;color:#2e7d32;font-weight:bold;font-size:16px;">{monto}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Fecha de pago</td><td style="padding:10px;">{fechaPago}</td></tr>
                </table>
                {(detallesList.Count > 0 ? $"""
                <p style="font-weight:bold;margin:16px 0 6px 0;">Detalle de formas de pago:</p>
                <table style="width:100%;border-collapse:collapse;font-size:13px;margin-bottom:16px;">
                  <tr style="background:#1e3a5f;color:white;"><th style="padding:8px;text-align:left;">Forma de pago</th><th style="padding:8px;text-align:right;">Monto</th></tr>
                  {filasMedios}
                </table>
                """ : "")}
                <p>Se adjunta el recibo de pago en formato PDF.</p>
                <hr style="border:none;border-top:1px solid #e0e0e0;margin:20px 0;">
                <p style="color:#666;font-size:12px;">Este mensaje fue enviado automáticamente por {empresa}.</p>
              </div>
            </body></html>
            """;
    }

    private static string BuildDescripcionCheque(PagoDetalle d)
    {
        var partes = new System.Text.StringBuilder("Cheque");
        if (!string.IsNullOrWhiteSpace(d.ChequeBanco)) partes.Append($" — {d.ChequeBanco}");
        if (!string.IsNullOrWhiteSpace(d.ChequeNumero)) partes.Append($" N° {d.ChequeNumero}");
        if (d.ChequeFechaVencimiento.HasValue) partes.Append($" — vence {d.ChequeFechaVencimiento.Value:dd/MM/yyyy}");
        return partes.ToString();
    }

    private static PagoListDto MapToListDto(Pago p) => new()
    {
        Id                 = p.Id,
        ContratoId         = p.ContratoId,
        ContratoCodigo     = p.Contrato.Codigo,
        PropiedadDireccion = p.Contrato.Propiedad.Direccion,
        LocatarioNombre    = p.Contrato.LocatarioNombre,
        LocatarioApellido  = p.Contrato.LocatarioApellido,
        LocadorNombre      = p.Contrato.LocadorNombre,
        LocadorApellido    = p.Contrato.LocadorApellido,
        LocadorEmail       = p.Contrato.LocadorEmail,
        NumeroCuota        = p.NumeroCuota,
        Periodo            = p.Periodo,
        MontoEsperado      = p.MontoEsperado,
        MontoPagado        = p.MontoPagado,
        FechaPago          = p.FechaPago,
        Estado             = p.Estado.ToString(),
        Observaciones      = p.Observaciones,
        Detalles           = p.Detalles.Where(d => d.Activo).Select(d => new PagoDetalleDto
        {
            Id                     = d.Id,
            Medio                  = d.Medio.ToString(),
            Monto                  = d.Monto,
            Referencia             = d.Referencia,
            ChequeBanco            = d.ChequeBanco,
            ChequeNumero           = d.ChequeNumero,
            ChequeFechaVencimiento = d.ChequeFechaVencimiento,
        }).ToList(),
        FechaCreacion      = p.FechaCreacion,
        FechaActualizacion = p.FechaActualizacion,
    };

    private static PagoDto MapPagoDto(Pago p) => new()
    {
        Id                 = p.Id,
        ContratoId         = p.ContratoId,
        NumeroCuota        = p.NumeroCuota,
        Periodo            = p.Periodo,
        MontoEsperado      = p.MontoEsperado,
        MontoPagado        = p.MontoPagado,
        FechaPago          = p.FechaPago,
        Estado             = p.Estado.ToString(),
        Observaciones      = p.Observaciones,
        Detalles           = p.Detalles.Where(d => d.Activo).Select(d => new PagoDetalleDto
        {
            Id                     = d.Id,
            Medio                  = d.Medio.ToString(),
            Monto                  = d.Monto,
            Referencia             = d.Referencia,
            ChequeBanco            = d.ChequeBanco,
            ChequeNumero           = d.ChequeNumero,
            ChequeFechaVencimiento = d.ChequeFechaVencimiento,
        }).ToList(),
        FechaCreacion      = p.FechaCreacion,
        FechaActualizacion = p.FechaActualizacion,
    };

    private static ContratoDto MapContratoDto(Contrato c) => new()
    {
        Id                  = c.Id,
        Codigo              = c.Codigo,
        Tipo                = c.Tipo.ToString(),
        Estado              = c.Estado.ToString(),
        PropiedadId         = c.PropiedadId,
        PropiedadDireccion  = c.Propiedad.Direccion,
        PropiedadCodigo     = c.Propiedad.Codigo,
        LocadorNombre       = c.LocadorNombre,
        LocadorApellido     = c.LocadorApellido,
        LocadorDni          = c.LocadorDni,
        LocadorEmail        = c.LocadorEmail,
        LocatarioNombre     = c.LocatarioNombre,
        LocatarioApellido   = c.LocatarioApellido,
        LocatarioDni        = c.LocatarioDni,
        MontoBase           = c.MontoBase,
        Moneda              = c.Moneda.ToString(),
        FechaInicio         = c.FechaInicio,
        FechaFin            = c.FechaFin,
    };
}
