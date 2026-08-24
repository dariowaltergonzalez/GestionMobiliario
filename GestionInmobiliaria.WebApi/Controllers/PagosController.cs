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
    private readonly ILiquidacionRepository _liquidaciones;
    private readonly IPunitorioService _punitorio;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PagosController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PagosController(
        IPagoRepository pagos,
        IPdfReportService pdf,
        ILiquidacionRepository liquidaciones,
        IPunitorioService punitorio,
        ApplicationDbContext context,
        IWebHostEnvironment env,
        ILogger<PagosController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _pagos = pagos;
        _pdf = pdf;
        _liquidaciones = liquidaciones;
        _punitorio = punitorio;
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
        [FromQuery] int? anio,
        [FromQuery] string? buscar)
    {
        var resultado = await _pagos.GetPagedAsync(paginacion, contratoId, estado, mes, anio, buscar);
        var items = new List<PagoListDto>();
        foreach (var pago in resultado.Items)
        {
            var dto = MapToListDto(pago);
            var punitorio = await _punitorio.CalcularAsync(pago);
            dto.MontoPunitorio = punitorio.Monto;
            dto.DiasAtraso = punitorio.DiasAtraso;
            dto.TasaPunitorioUsada = punitorio.TasaUsada;
            items.Add(dto);
        }
        var paginado = new PagedResult<PagoListDto>
        {
            Items = items,
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

        // Una cuota Pagada no se vuelve a tocar por acá — no hay "anular"/"recobrar" en el sistema
        // (decisión de negocio, ver docs/logica-negocio.md sección PUNITORIOS/PENDIENTES GENERALES:
        // cualquier corrección se resuelve hacia adelante, ej. ajuste en la próxima cuota, nunca
        // reescribiendo un cobro ya asentado). Antes de esto la UI ya ocultaba el botón de cobro una
        // vez Pagada, pero nada lo garantizaba del lado del servidor.
        if (pago.Estado == EstadoPago.Pagado)
            return BadRequest(ApiResponse<PagoDto>.Fail("Esta cuota ya está pagada, no se puede modificar."));

        // Se calcula ANTES de tocar pago.Estado: CalcularAsync solo da un resultado > 0 si la cuota
        // todavía está Pendiente/Atrasado (ver PunitorioService). Se recalcula acá server-side en vez
        // de confiar en un monto que mande el cliente — es plata, nunca se toma un número de afuera.
        if (request.CobrarPunitorio && (EstadoPago)request.Estado == EstadoPago.Pagado)
        {
            var punitorio = await _punitorio.CalcularAsync(pago);
            if (punitorio.Monto > 0)
            {
                pago.MontoPunitorioCobrado = punitorio.Monto;
                pago.DiasAtrasoPunitorioCobrado = punitorio.DiasAtraso;
                pago.FechaVencimientoPunitorioCobrado = VencimientoCalculator.Calcular(pago.Periodo, pago.Contrato!.DiaVencimientoPago);
                pago.DetallePunitorioCobrado = punitorio.TasaUsada;
            }
        }

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

        if (pago.Estado == EstadoPago.Pagado)
        {
            await GenerarLiquidacionSiCorrespondeAsync(pago);

            var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst("nombre") is { } n && User.FindFirst("apellido") is { } a
                ? $"{n.Value} {a.Value}"
                : User.Identity?.Name;
            var tenantId = pago.TenantId;

            // Capturar todo lo necesario antes del Task.Run (scope del request, tenant activo)
            var contratoDto      = MapContratoDto(pago.Contrato!);
            var pagoDto          = MapPagoDto(actualizado);
            var propietarioRefId = pago.Contrato!.PropietarioRefId;
            var inquilinoRefId   = pago.Contrato.InquilinoRefId;
            var asuntoDireccion  = pago.Contrato.Propiedad.Direccion;
            var contratoCodigo   = pago.Contrato.Codigo;
            var periodo          = pago.Periodo.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-AR")).ToUpper();
            var monto            = $"$ {(pago.MontoPagado ?? pago.MontoEsperado):N0}";
            var detallesSnap     = detalles.ToList();
            // La config de empresa se carga aquí donde el tenant está disponible
            var pdfConfig        = await BuildConfig();

            _ = Task.Run(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var ctx          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var pdfSvc       = scope.ServiceProvider.GetRequiredService<IPdfReportService>();
                var notificacion = scope.ServiceProvider.GetRequiredService<INotificacionService>();

                var reciboPdf = pdfSvc.GenerarReciboPago(pagoDto, contratoDto, pdfConfig);
                var fileName  = $"Recibo_{contratoCodigo}_{periodo.Replace(" ", "_")}.pdf";
                var adjuntos  = new List<EmailAdjunto> { new() { NombreArchivo = fileName, Contenido = reciboPdf } };
                var asunto    = $"Recibo de pago — {asuntoDireccion} — {periodo}";
                var contexto  = new NotificacionContexto
                {
                    TenantId = tenantId,
                    UserId = userId,
                    UserName = userName,
                    EntidadRelacionada = "EmailRecibo",
                    EntidadRelacionadaId = pagoId.ToString(),
                    DatosAdicionales = new { contrato = contratoCodigo, periodo },
                };

                // Tenant filtrado a mano: este Task.Run corre sin HttpContext, así que
                // ITenantService no puede resolver el tenant activo (ver NotificacionService).
                if (propietarioRefId.HasValue)
                {
                    var propietario = await ctx.Propietarios.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.Id == propietarioRefId.Value && p.TenantId == tenantId);
                    if (propietario is not null)
                    {
                        var cuerpo = BuildEmailBody(pdfConfig.NombreEmpresa, contratoDto, pagoDto, detallesSnap, periodo, monto, paraLocatario: false);
                        await notificacion.NotificarAsync(propietario, "AvisoCobro", asunto, cuerpo, contexto, adjuntos);
                    }
                }

                if (inquilinoRefId.HasValue)
                {
                    var inquilino = await ctx.Inquilinos.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(i => i.Id == inquilinoRefId.Value && i.TenantId == tenantId);
                    if (inquilino is not null)
                    {
                        var cuerpo = BuildEmailBody(pdfConfig.NombreEmpresa, contratoDto, pagoDto, detallesSnap, periodo, monto, paraLocatario: true);
                        await notificacion.NotificarAsync(inquilino, "ReciboPago", asunto, cuerpo, contexto, adjuntos);
                    }
                }
            });
        }

        return Ok(ApiResponse<PagoDto>.Ok(MapPagoDto(actualizado), "Pago actualizado correctamente."));
    }

    private async Task GenerarLiquidacionSiCorrespondeAsync(Pago pago)
    {
        var contrato = pago.Contrato!;
        if (!contrato.AdministracionCobros) return;
        if (contrato.ComisionLocadorPorcentaje is null && contrato.ComisionLocadorMonto is null) return;

        if (await _liquidaciones.GetByPagoIdAsync(pago.Id) is not null) return;

        var montoCobrado = pago.MontoPagado ?? pago.MontoEsperado;
        var montoComision = contrato.ComisionLocadorMonto
            ?? Math.Round(montoCobrado * (contrato.ComisionLocadorPorcentaje!.Value / 100), 2);

        var gastosPendientes = await _context.Gastos
            .Where(g => g.Activo
                && g.PropiedadId == contrato.PropiedadId
                && g.Responsable == ResponsableGasto.Propietario
                && g.Estado == EstadoGasto.Pendiente)
            .ToListAsync();
        var montoGastos = gastosPendientes.Sum(g => g.Monto);

        var montoALiquidar = montoCobrado - montoComision - montoGastos;

        var liquidacion = new Liquidacion
        {
            PagoId = pago.Id,
            MontoCobrado = montoCobrado,
            ComisionPorcentaje = contrato.ComisionLocadorPorcentaje,
            ComisionMonto = contrato.ComisionLocadorMonto,
            MontoComision = montoComision,
            MontoGastos = montoGastos,
            MontoALiquidar = montoALiquidar,
            Estado = EstadoLiquidacion.Pendiente,
            TenantId = pago.TenantId,
        };
        await _liquidaciones.CreateAsync(liquidacion);

        if (gastosPendientes.Count > 0)
        {
            var ahora = DateTime.UtcNow;
            foreach (var gasto in gastosPendientes)
            {
                gasto.Estado = EstadoGasto.Resuelto;
                gasto.FechaResolucion = ahora;
                gasto.LiquidacionId = liquidacion.Id;
                gasto.FechaActualizacion = ahora;
            }
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Liquidación generada. PagoId={PagoId} Contrato={Contrato} MontoCobrado={MontoCobrado} Comision={Comision} Gastos={Gastos} MontoALiquidar={MontoALiquidar}",
            pago.Id, contrato.Codigo, montoCobrado, montoComision, montoGastos, montoALiquidar);
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

    private static string BuildEmailBody(string empresa, ContratoDto c, PagoDto p, IEnumerable<PagoDetalle> detalles, string periodo, string monto, bool paraLocatario)
    {
        var titulo        = paraLocatario ? "Recibo de pago registrado" : "Cobro de alquiler registrado";
        var nombreDestino  = paraLocatario ? $"{c.LocatarioNombre} {c.LocatarioApellido}" : $"{c.LocadorNombre} {c.LocadorApellido}";
        var textoIntro     = paraLocatario ? "Te confirmamos que se registró tu pago correspondiente a:" : "Le informamos que se registró el cobro correspondiente a:";

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
                <h2 style="color:#1e3a5f;margin:0 0 8px 0;">{titulo}</h2>
                <p>Estimado/a <strong>{nombreDestino}</strong>,</p>
                <p>{textoIntro}</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;">
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;width:40%;">Propiedad</td><td style="padding:10px;">{c.PropiedadDireccion}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Inquilino/a</td><td style="padding:10px;">{c.LocatarioNombre} {c.LocatarioApellido}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Período</td><td style="padding:10px;">{periodo}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Cuota N°</td><td style="padding:10px;">{p.NumeroCuota}</td></tr>
                  {(p.MontoPunitorioCobrado is { } montoPunitorio ? $"""
                  <tr><td style="padding:10px;font-weight:bold;">Cuota</td><td style="padding:10px;">$ {p.MontoEsperado:N2}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;color:#c62828;">Punitorio ({p.DiasAtrasoPunitorioCobrado} días de atraso)</td><td style="padding:10px;color:#c62828;">$ {montoPunitorio:N2}</td></tr>
                  """ : "")}
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
        MontoPunitorioCobrado = p.MontoPunitorioCobrado,
        DiasAtrasoPunitorioCobrado = p.DiasAtrasoPunitorioCobrado,
        FechaVencimientoPunitorioCobrado = p.FechaVencimientoPunitorioCobrado,
        DetallePunitorioCobrado = p.DetallePunitorioCobrado,
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
        MontoPunitorioCobrado = p.MontoPunitorioCobrado,
        DiasAtrasoPunitorioCobrado = p.DiasAtrasoPunitorioCobrado,
        FechaVencimientoPunitorioCobrado = p.FechaVencimientoPunitorioCobrado,
        DetallePunitorioCobrado = p.DetallePunitorioCobrado,
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
