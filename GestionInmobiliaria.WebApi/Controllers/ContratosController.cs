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
[Route("api/contratos")]
[Authorize]
public class ContratosController : ControllerBase
{
    private readonly IContratoRepository _repo;
    private readonly IPagoRepository _pagos;
    private readonly IClausulaContratoRepository _clausulas;
    private readonly IPdfReportService _pdf;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ContratosController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ContratosController(
        IContratoRepository repo,
        IPagoRepository pagos,
        IClausulaContratoRepository clausulas,
        IPdfReportService pdf,
        ApplicationDbContext context,
        IWebHostEnvironment env,
        ILogger<ContratosController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _repo = repo;
        _pagos = pagos;
        _clausulas = clausulas;
        _pdf = pdf;
        _context = context;
        _env = env;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? buscar,
        [FromQuery] TipoContrato? tipo,
        [FromQuery] EstadoContrato? estado,
        [FromQuery] int? propiedadId,
        [FromQuery] int? agenteId)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, buscar, tipo, estado, propiedadId, agenteId);
        var paginado = new PagedResult<ContratoDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<ContratoDto>>.Ok(paginado));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contrato = await _repo.GetByIdAsync(id);
        if (contrato is null)
            return NotFound(ApiResponse<ContratoDto>.Fail("Contrato no encontrado."));
        return Ok(ApiResponse<ContratoDto>.Ok(MapToDto(contrato)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Create([FromBody] CreateContratoRequest request)
    {
        var validacion = Validar(request);
        if (validacion is not null) return BadRequest(validacion);

        var contrato = MapFromRequest(request);
        var creado = await _repo.CreateAsync(contrato);
        var resultado = await _repo.GetByIdAsync(creado.Id);

        if (resultado!.Estado == EstadoContrato.Vigente)
            await NotificarNuevoContratoAsync(MapToDto(resultado), resultado.TenantId);

        return Ok(ApiResponse<ContratoDto>.Ok(MapToDto(resultado), "Contrato creado correctamente."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContratoRequest request)
    {
        var contrato = await _repo.GetByIdAsync(id);
        if (contrato is null)
            return NotFound(ApiResponse<ContratoDto>.Fail("Contrato no encontrado."));

        if (contrato.Estado != EstadoContrato.Borrador)
            return BadRequest(ApiResponse<ContratoDto>.Fail(
                $"Solo se pueden editar contratos en estado Borrador. Este contrato está en estado '{contrato.Estado}'."));

        var validacion = Validar(request);
        if (validacion is not null) return BadRequest(validacion);

        contrato.Tipo = (TipoContrato)request.Tipo;
        contrato.PropiedadId = request.PropiedadId;
        contrato.ReservaId = request.ReservaId;
        contrato.AgenteId = request.AgenteId;
        contrato.PropietarioRefId = request.PropietarioRefId;
        contrato.InquilinoRefId = request.InquilinoRefId;
        contrato.LocadorNombre = request.LocadorNombre;
        contrato.LocadorApellido = request.LocadorApellido;
        contrato.LocadorDni = request.LocadorDni;
        contrato.LocadorEmail = request.LocadorEmail;
        contrato.LocadorTelefono = request.LocadorTelefono;
        contrato.LocadorDomicilio = request.LocadorDomicilio;
        contrato.LocadorBanco = request.LocadorBanco;
        contrato.LocadorCbu = request.LocadorCbu;
        contrato.LocadorCuit = request.LocadorCuit;
        contrato.LocatarioNombre = request.LocatarioNombre;
        contrato.LocatarioApellido = request.LocatarioApellido;
        contrato.LocatarioDni = request.LocatarioDni;
        contrato.LocatarioEmail = request.LocatarioEmail;
        contrato.LocatarioTelefono = request.LocatarioTelefono;
        contrato.GaranteNombre = request.GaranteNombre;
        contrato.GaranteApellido = request.GaranteApellido;
        contrato.GaranteDni = request.GaranteDni;
        contrato.GaranteTelefono = request.GaranteTelefono;
        contrato.MontoBase = request.MontoBase;
        contrato.MontoActual = request.MontoBase;
        contrato.Moneda = (Moneda)request.Moneda;
        contrato.TipoAjuste = (TipoAjuste)request.TipoAjuste;
        contrato.PeriodicidadAjusteMeses = request.PeriodicidadAjusteMeses;
        contrato.PorcentajeAjuste = request.PorcentajeAjuste;
        contrato.DiaVencimientoPago = request.DiaVencimientoPago;
        contrato.ComisionLocadorPorcentaje = request.ComisionLocadorPorcentaje;
        contrato.ComisionLocadorMonto = request.ComisionLocadorMonto;
        contrato.ComisionLocatarioPorcentaje = request.ComisionLocatarioPorcentaje;
        contrato.ComisionLocatarioMonto = request.ComisionLocatarioMonto;
        contrato.AdministracionCobros = request.AdministracionCobros;
        contrato.FechaInicio = request.FechaInicio;
        contrato.FechaFin = request.FechaFin;
        contrato.FechaEscrituracion = request.FechaEscrituracion;
        contrato.Observaciones = request.Observaciones;

        var actualizado = await _repo.UpdateAsync(contrato);
        var resultado = await _repo.GetByIdAsync(actualizado.Id);
        return Ok(ApiResponse<ContratoDto>.Ok(MapToDto(resultado!), "Contrato actualizado correctamente."));
    }

    [HttpPut("{id}/estado")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> TransicionEstado(int id, [FromBody] TransicionEstadoRequest request)
    {
        var nuevoEstado = (EstadoContrato)request.Estado;

        if (nuevoEstado is EstadoContrato.Rescindido or EstadoContrato.Anulado
            && string.IsNullOrWhiteSpace(request.Motivo))
            return BadRequest(ApiResponse<ContratoDto>.Fail("El motivo es requerido para este cambio de estado."));

        var (ok, error, contrato) = await _repo.TransicionEstadoAsync(id, nuevoEstado, request.Motivo, request.Fecha);
        if (!ok) return BadRequest(ApiResponse<ContratoDto>.Fail(error!));

        var resultado = await _repo.GetByIdAsync(contrato!.Id);

        if (nuevoEstado == EstadoContrato.Vigente)
            await NotificarNuevoContratoAsync(MapToDto(resultado!), resultado!.TenantId);

        if (nuevoEstado is EstadoContrato.Finalizado or EstadoContrato.Rescindido or EstadoContrato.Anulado)
            await NotificarCambioEstadoAsync(MapToDto(resultado!), resultado!.TenantId, nuevoEstado, request.Motivo);

        return Ok(ApiResponse<ContratoDto>.Ok(MapToDto(resultado!), $"Contrato pasado a estado '{nuevoEstado}' correctamente."));
    }

    [HttpPost("{id}/ajustes")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> AplicarAjuste(int id, [FromBody] AplicarAjusteRequest request)
    {
        var contrato = await _repo.GetByIdAsync(id);
        if (contrato is null)
            return NotFound(ApiResponse<object>.Fail("Contrato no encontrado."));

        if (contrato.Estado != EstadoContrato.Vigente)
            return BadRequest(ApiResponse<object>.Fail("Solo se pueden ajustar contratos en estado Vigente."));

        if (request.Valor == 0)
            return BadRequest(ApiResponse<object>.Fail("El valor del ajuste no puede ser cero."));

        if (request.Tipo != "Porcentaje" && request.Tipo != "MontoFijo")
            return BadRequest(ApiResponse<object>.Fail("Tipo de ajuste inválido. Use 'Porcentaje' o 'MontoFijo'."));

        var montoAnterior = contrato.MontoActual;
        var montoNuevo = request.Tipo == "Porcentaje"
            ? Math.Round(montoAnterior * (1 + request.Valor / 100), 2)
            : Math.Round(montoAnterior + request.Valor, 2);

        if (montoNuevo <= 0)
            return BadRequest(ApiResponse<object>.Fail("El nuevo monto no puede ser menor o igual a cero."));

        // Actualizar cuotas pendientes y atrasadas
        var pagosPendientes = contrato.Pagos
            .Where(p => p.Estado == EstadoPago.Pendiente || p.Estado == EstadoPago.Atrasado)
            .ToList();

        foreach (var pago in pagosPendientes)
        {
            pago.MontoEsperado = montoNuevo;
            pago.FechaActualizacion = DateTime.UtcNow;
        }

        // Actualizar contrato
        contrato.MontoActual = montoNuevo;
        contrato.FechaUltimoAjuste = DateTime.UtcNow;
        contrato.FechaActualizacion = DateTime.UtcNow;

        // Registrar en historial
        var ajuste = new AjusteContrato
        {
            ContratoId      = id,
            FechaAplicacion = DateTime.UtcNow,
            MontoPrevio     = montoAnterior,
            MontoNuevo      = montoNuevo,
            Porcentaje      = request.Tipo == "Porcentaje" ? request.Valor : null,
            TipoAjuste      = request.Tipo,
            Observaciones   = request.Observaciones?.Trim(),
            Activo          = true,
            FechaCreacion   = DateTime.UtcNow,
            FechaActualizacion = DateTime.UtcNow,
        };
        _context.AjustesContrato.Add(ajuste);

        await _context.SaveChangesAsync();

        await NotificarAvisoAumentoAsync(MapToDto(contrato), contrato.TenantId, montoAnterior, montoNuevo, ajuste.Observaciones);

        var dto = new AjusteContratoDto
        {
            Id              = ajuste.Id,
            ContratoId      = ajuste.ContratoId,
            FechaAplicacion = ajuste.FechaAplicacion,
            MontoPrevio     = ajuste.MontoPrevio,
            MontoNuevo      = ajuste.MontoNuevo,
            Porcentaje      = ajuste.Porcentaje,
            TipoAjuste      = ajuste.TipoAjuste,
            Observaciones   = ajuste.Observaciones,
        };

        return Ok(ApiResponse<AjusteContratoDto>.Ok(dto,
            $"Ajuste aplicado: {pagosPendientes.Count} cuota(s) actualizada(s) de {montoAnterior:N2} a {montoNuevo:N2}."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var contrato = await _repo.GetByIdAsync(id);
        if (contrato is null)
            return NotFound(ApiResponse<object>.Fail("Contrato no encontrado."));

        if (contrato.Estado != EstadoContrato.Borrador)
            return BadRequest(ApiResponse<object>.Fail(
                $"Solo se pueden eliminar contratos en estado Borrador. Para otros estados, usá la transición de estado correspondiente."));

        var eliminado = await _repo.DeleteAsync(id);
        if (!eliminado)
            return NotFound(ApiResponse<object>.Fail("Contrato no encontrado."));
        return Ok(ApiResponse<object>.Ok(null, "Contrato eliminado correctamente."));
    }

    [HttpGet("{id}/pagos")]
    public async Task<IActionResult> GetPagos(int id)
    {
        var pagos = await _pagos.GetByContratoAsync(id);
        return Ok(ApiResponse<IEnumerable<PagoDto>>.Ok(pagos.Select(MapPagoToDto)));
    }

    private async Task NotificarNuevoContratoAsync(ContratoDto contratoDto, int tenantId)
    {
        var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst("nombre") is { } n && User.FindFirst("apellido") is { } a
            ? $"{n.Value} {a.Value}"
            : User.Identity?.Name;

        // Cláusulas y config de empresa se leen acá (en el scope del request, con el tenant activo
        // ya resuelto) porque en el Task.Run de abajo no hay HttpContext para resolverlo.
        var clausulas = (await _clausulas.GetActivasAsync())
            .Select(c => new ClausulaContratoDto
            {
                Id = c.Id, Orden = c.Orden, Numero = c.Numero,
                Titulo = c.Titulo, Texto = c.Texto, Activo = c.Activo,
            })
            .ToList();
        var pdfConfig = await BuildConfig();

        var propietarioRefId = contratoDto.PropietarioRefId;
        var inquilinoRefId   = contratoDto.InquilinoRefId;

        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ctx          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pdfSvc       = scope.ServiceProvider.GetRequiredService<IPdfReportService>();
            var notificacion = scope.ServiceProvider.GetRequiredService<INotificacionService>();

            try
            {
                var contratoPdf = pdfSvc.GenerarContrato(contratoDto, pdfConfig, clausulas);
                var fileName    = $"Contrato_{contratoDto.Codigo}.pdf";
                var adjuntos    = new List<EmailAdjunto> { new() { NombreArchivo = fileName, Contenido = contratoPdf } };
                var asunto      = $"Nuevo contrato — {contratoDto.PropiedadDireccion} — {contratoDto.Codigo}";
                var contexto    = new NotificacionContexto
                {
                    TenantId = tenantId,
                    UserId = userId,
                    UserName = userName,
                    EntidadRelacionada = "EmailNuevoContrato",
                    EntidadRelacionadaId = contratoDto.Id.ToString(),
                    DatosAdicionales = new { contrato = contratoDto.Codigo },
                };

                // Tenant filtrado a mano: ver comentario equivalente en PagosController.
                if (propietarioRefId.HasValue)
                {
                    var propietario = await ctx.Propietarios.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.Id == propietarioRefId.Value && p.TenantId == tenantId);
                    if (propietario is not null)
                    {
                        var cuerpo = BuildNuevoContratoEmailBody(pdfConfig.NombreEmpresa, contratoDto, paraLocatario: false);
                        await notificacion.NotificarAsync(propietario, "NuevoContrato", asunto, cuerpo, contexto, adjuntos);
                    }
                }

                if (inquilinoRefId.HasValue)
                {
                    var inquilino = await ctx.Inquilinos.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(i => i.Id == inquilinoRefId.Value && i.TenantId == tenantId);
                    if (inquilino is not null)
                    {
                        var cuerpo = BuildNuevoContratoEmailBody(pdfConfig.NombreEmpresa, contratoDto, paraLocatario: true);
                        await notificacion.NotificarAsync(inquilino, "NuevoContrato", asunto, cuerpo, contexto, adjuntos);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar la notificación de nuevo contrato. ContratoId={ContratoId}", contratoDto.Id);
            }
        });
    }

    private async Task NotificarCambioEstadoAsync(ContratoDto contratoDto, int tenantId, EstadoContrato nuevoEstado, string? motivo)
    {
        var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst("nombre") is { } n && User.FindFirst("apellido") is { } a
            ? $"{n.Value} {a.Value}"
            : User.Identity?.Name;

        var pdfConfig = await BuildConfig();
        var propietarioRefId = contratoDto.PropietarioRefId;
        var inquilinoRefId   = contratoDto.InquilinoRefId;

        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ctx          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificacion = scope.ServiceProvider.GetRequiredService<INotificacionService>();

            try
            {
                var asunto   = $"{EstadoLabel(nuevoEstado)} de contrato — {contratoDto.PropiedadDireccion} — {contratoDto.Codigo}";
                var contexto = new NotificacionContexto
                {
                    TenantId = tenantId,
                    UserId = userId,
                    UserName = userName,
                    EntidadRelacionada = "EmailCambioEstadoContrato",
                    EntidadRelacionadaId = contratoDto.Id.ToString(),
                    DatosAdicionales = new { contrato = contratoDto.Codigo, estado = nuevoEstado.ToString(), motivo },
                };

                // Tenant filtrado a mano: ver comentario equivalente en PagosController.
                if (propietarioRefId.HasValue)
                {
                    var propietario = await ctx.Propietarios.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.Id == propietarioRefId.Value && p.TenantId == tenantId);
                    if (propietario is not null)
                    {
                        var cuerpo = BuildCambioEstadoEmailBody(pdfConfig.NombreEmpresa, contratoDto, nuevoEstado, motivo, paraLocatario: false);
                        await notificacion.NotificarAsync(propietario, "CambioEstadoContrato", asunto, cuerpo, contexto);
                    }
                }

                if (inquilinoRefId.HasValue)
                {
                    var inquilino = await ctx.Inquilinos.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(i => i.Id == inquilinoRefId.Value && i.TenantId == tenantId);
                    if (inquilino is not null)
                    {
                        var cuerpo = BuildCambioEstadoEmailBody(pdfConfig.NombreEmpresa, contratoDto, nuevoEstado, motivo, paraLocatario: true);
                        await notificacion.NotificarAsync(inquilino, "CambioEstadoContrato", asunto, cuerpo, contexto);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar la notificación de cambio de estado. ContratoId={ContratoId}", contratoDto.Id);
            }
        });
    }

    private static string EstadoLabel(EstadoContrato estado) => estado switch
    {
        EstadoContrato.Finalizado => "Finalización",
        EstadoContrato.Rescindido => "Rescisión",
        EstadoContrato.Anulado    => "Anulación",
        _                         => "Cambio de estado",
    };

    private static string BuildCambioEstadoEmailBody(string empresa, ContratoDto c, EstadoContrato nuevoEstado, string? motivo, bool paraLocatario)
    {
        var titulo        = $"{EstadoLabel(nuevoEstado)} de contrato";
        var nombreDestino = paraLocatario ? $"{c.LocatarioNombre} {c.LocatarioApellido}" : $"{c.LocadorNombre} {c.LocadorApellido}";
        var textoIntro    = paraLocatario
            ? $"Te informamos que tu contrato cambió de estado a <strong>{nuevoEstado}</strong>."
            : $"Le informamos que el contrato sobre su propiedad cambió de estado a <strong>{nuevoEstado}</strong>.";

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
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;width:40%;">Código</td><td style="padding:10px;">{c.Codigo}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Propiedad</td><td style="padding:10px;">{c.PropiedadDireccion}</td></tr>
                  {(!string.IsNullOrWhiteSpace(motivo) ? $"""<tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Motivo</td><td style="padding:10px;">{motivo}</td></tr>""" : "")}
                </table>
                <p>Ante cualquier duda, comunicate con nosotros.</p>
                <hr style="border:none;border-top:1px solid #e0e0e0;margin:20px 0;">
                <p style="color:#666;font-size:12px;">Este mensaje fue enviado automáticamente por {empresa}.</p>
              </div>
            </body></html>
            """;
    }

    private async Task NotificarAvisoAumentoAsync(ContratoDto contratoDto, int tenantId, decimal montoAnterior, decimal montoNuevo, string? observaciones)
    {
        var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst("nombre") is { } n && User.FindFirst("apellido") is { } a
            ? $"{n.Value} {a.Value}"
            : User.Identity?.Name;

        var pdfConfig = await BuildConfig();
        var propietarioRefId = contratoDto.PropietarioRefId;
        var inquilinoRefId   = contratoDto.InquilinoRefId;

        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ctx          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificacion = scope.ServiceProvider.GetRequiredService<INotificacionService>();

            try
            {
                var asunto   = $"Aviso de aumento — {contratoDto.PropiedadDireccion} — {contratoDto.Codigo}";
                var contexto = new NotificacionContexto
                {
                    TenantId = tenantId,
                    UserId = userId,
                    UserName = userName,
                    EntidadRelacionada = "EmailAvisoAumento",
                    EntidadRelacionadaId = contratoDto.Id.ToString(),
                    DatosAdicionales = new { contrato = contratoDto.Codigo, montoAnterior, montoNuevo },
                };

                // Tenant filtrado a mano: ver comentario equivalente en PagosController.
                if (propietarioRefId.HasValue)
                {
                    var propietario = await ctx.Propietarios.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.Id == propietarioRefId.Value && p.TenantId == tenantId);
                    if (propietario is not null)
                    {
                        var cuerpo = BuildAvisoAumentoEmailBody(pdfConfig.NombreEmpresa, contratoDto, montoAnterior, montoNuevo, observaciones, paraLocatario: false);
                        await notificacion.NotificarAsync(propietario, "AvisoAumento", asunto, cuerpo, contexto);
                    }
                }

                if (inquilinoRefId.HasValue)
                {
                    var inquilino = await ctx.Inquilinos.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(i => i.Id == inquilinoRefId.Value && i.TenantId == tenantId);
                    if (inquilino is not null)
                    {
                        var cuerpo = BuildAvisoAumentoEmailBody(pdfConfig.NombreEmpresa, contratoDto, montoAnterior, montoNuevo, observaciones, paraLocatario: true);
                        await notificacion.NotificarAsync(inquilino, "AvisoAumento", asunto, cuerpo, contexto);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar el aviso de aumento. ContratoId={ContratoId}", contratoDto.Id);
            }
        });
    }

    private static string BuildAvisoAumentoEmailBody(string empresa, ContratoDto c, decimal montoAnterior, decimal montoNuevo, string? observaciones, bool paraLocatario)
    {
        var nombreDestino = paraLocatario ? $"{c.LocatarioNombre} {c.LocatarioApellido}" : $"{c.LocadorNombre} {c.LocadorApellido}";
        var textoIntro    = paraLocatario
            ? "Te informamos que se aplicó un ajuste al monto de tu cuota:"
            : "Le informamos que se aplicó un ajuste al monto de la cuota de su propiedad:";
        var monedaSimbolo = c.Moneda == "USD" ? "U$S" : "$";
        var diferencia    = montoNuevo - montoAnterior;
        var esAumento     = diferencia > 0;

        return $"""
            <!DOCTYPE html><html><head><meta charset="utf-8"></head>
            <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:0;">
              <div style="background:#1e3a5f;padding:20px 24px;border-radius:8px 8px 0 0;">
                <h1 style="color:white;margin:0;font-size:17px;">{empresa}</h1>
              </div>
              <div style="background:#f8f9fa;padding:24px;border:1px solid #e0e0e0;border-top:none;border-radius:0 0 8px 8px;">
                <h2 style="color:#1e3a5f;margin:0 0 8px 0;">Aviso de aumento de cuota</h2>
                <p>Estimado/a <strong>{nombreDestino}</strong>,</p>
                <p>{textoIntro}</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;">
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;width:40%;">Código</td><td style="padding:10px;">{c.Codigo}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Propiedad</td><td style="padding:10px;">{c.PropiedadDireccion}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Monto anterior</td><td style="padding:10px;">{monedaSimbolo} {montoAnterior:N0}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Monto nuevo</td><td style="padding:10px;color:{(esAumento ? "#2e7d32" : "#e65100")};font-weight:bold;font-size:16px;">{monedaSimbolo} {montoNuevo:N0}</td></tr>
                  {(!string.IsNullOrWhiteSpace(observaciones) ? $"""<tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Observaciones</td><td style="padding:10px;">{observaciones}</td></tr>""" : "")}
                </table>
                <p>Este nuevo monto aplica a partir de la próxima cuota pendiente.</p>
                <hr style="border:none;border-top:1px solid #e0e0e0;margin:20px 0;">
                <p style="color:#666;font-size:12px;">Este mensaje fue enviado automáticamente por {empresa}.</p>
              </div>
            </body></html>
            """;
    }

    private Task<PdfReportConfig> BuildConfig()
    {
        return BuildConfigInterno(_context, _env);
    }

    private static async Task<PdfReportConfig> BuildConfigInterno(ApplicationDbContext ctx, IWebHostEnvironment env)
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
            Titulo          = "Contrato de Locación",
            NombreEmpresa   = empresa?.NombreComercial ?? "GestionInmobiliaria",
            Slogan          = empresa?.Slogan,
            InfoEmpresa     = partes.Count > 0 ? string.Join("  |  ", partes) : null,
            LogoBytes       = logoBytes,
            FechaGeneracion = DateTime.Now,
        };
    }

    private static string BuildNuevoContratoEmailBody(string empresa, ContratoDto c, bool paraLocatario)
    {
        var titulo       = "Nuevo contrato vigente";
        var nombreDestino = paraLocatario ? $"{c.LocatarioNombre} {c.LocatarioApellido}" : $"{c.LocadorNombre} {c.LocadorApellido}";
        var textoIntro    = paraLocatario
            ? "Te confirmamos que tu contrato quedó vigente, con los siguientes datos:"
            : "Le confirmamos que el contrato sobre su propiedad quedó vigente, con los siguientes datos:";
        var monedaSimbolo = c.Moneda == "USD" ? "U$S" : "$";
        var fechaFin      = c.FechaFin.HasValue ? c.FechaFin.Value.ToString("dd/MM/yyyy") : "—";

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
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;width:40%;">Código</td><td style="padding:10px;">{c.Codigo}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Propiedad</td><td style="padding:10px;">{c.PropiedadDireccion}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Monto</td><td style="padding:10px;color:#2e7d32;font-weight:bold;font-size:16px;">{monedaSimbolo} {c.MontoBase:N0}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Fecha de inicio</td><td style="padding:10px;">{c.FechaInicio:dd/MM/yyyy}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Fecha de fin</td><td style="padding:10px;">{fechaFin}</td></tr>
                </table>
                <p>Se adjunta el contrato completo en formato PDF.</p>
                <hr style="border:none;border-top:1px solid #e0e0e0;margin:20px 0;">
                <p style="color:#666;font-size:12px;">Este mensaje fue enviado automáticamente por {empresa}.</p>
              </div>
            </body></html>
            """;
    }

    private static ApiResponse<ContratoDto>? Validar(CreateContratoRequest r)
    {
        if (r.PropiedadId <= 0)
            return ApiResponse<ContratoDto>.Fail("La propiedad es requerida.");
        if (string.IsNullOrWhiteSpace(r.LocadorNombre) || string.IsNullOrWhiteSpace(r.LocadorApellido))
            return ApiResponse<ContratoDto>.Fail("Nombre y apellido del locador son requeridos.");
        if (string.IsNullOrWhiteSpace(r.LocatarioNombre) || string.IsNullOrWhiteSpace(r.LocatarioApellido))
            return ApiResponse<ContratoDto>.Fail("Nombre y apellido del locatario son requeridos.");
        if (r.MontoBase <= 0)
            return ApiResponse<ContratoDto>.Fail("El monto base debe ser mayor a cero.");
        if (r.FechaFin.HasValue && r.FechaFin <= r.FechaInicio)
            return ApiResponse<ContratoDto>.Fail("La fecha de fin debe ser posterior a la de inicio.");
        return null;
    }

    private static Contrato MapFromRequest(CreateContratoRequest r) => new()
    {
        Tipo = (TipoContrato)r.Tipo,
        Estado = (EstadoContrato)r.Estado,
        PropiedadId = r.PropiedadId,
        ReservaId = r.ReservaId,
        AgenteId = r.AgenteId,
        PropietarioRefId = r.PropietarioRefId,
        InquilinoRefId = r.InquilinoRefId,
        LocadorNombre = r.LocadorNombre,
        LocadorApellido = r.LocadorApellido,
        LocadorDni = r.LocadorDni,
        LocadorEmail = r.LocadorEmail,
        LocadorTelefono = r.LocadorTelefono,
        LocadorDomicilio = r.LocadorDomicilio,
        LocadorBanco = r.LocadorBanco,
        LocadorCbu = r.LocadorCbu,
        LocadorCuit = r.LocadorCuit,
        LocatarioNombre = r.LocatarioNombre,
        LocatarioApellido = r.LocatarioApellido,
        LocatarioDni = r.LocatarioDni,
        LocatarioEmail = r.LocatarioEmail,
        LocatarioTelefono = r.LocatarioTelefono,
        GaranteNombre = r.GaranteNombre,
        GaranteApellido = r.GaranteApellido,
        GaranteDni = r.GaranteDni,
        GaranteTelefono = r.GaranteTelefono,
        MontoBase = r.MontoBase,
        Moneda = (Moneda)r.Moneda,
        TipoAjuste = (TipoAjuste)r.TipoAjuste,
        PeriodicidadAjusteMeses = r.PeriodicidadAjusteMeses,
        DiaVencimientoPago = r.DiaVencimientoPago,
        ComisionLocadorPorcentaje = r.ComisionLocadorPorcentaje,
        ComisionLocadorMonto = r.ComisionLocadorMonto,
        ComisionLocatarioPorcentaje = r.ComisionLocatarioPorcentaje,
        ComisionLocatarioMonto = r.ComisionLocatarioMonto,
        AdministracionCobros = r.AdministracionCobros,
        PorcentajeAjuste = r.PorcentajeAjuste,
        FechaInicio = r.FechaInicio,
        FechaFin = r.FechaFin,
        FechaEscrituracion = r.FechaEscrituracion,
        Observaciones = r.Observaciones,
    };

    private static ContratoDto MapToDto(Contrato c) => new()
    {
        Id = c.Id,
        Codigo = c.Codigo,
        Tipo = c.Tipo.ToString(),
        Estado = c.Estado.ToString(),
        PropiedadId = c.PropiedadId,
        PropiedadDireccion = c.Propiedad.Direccion,
        PropiedadCodigo = c.Propiedad.Codigo,
        ReservaId = c.ReservaId,
        AgenteId = c.AgenteId,
        AgenteNombre = c.Agente is not null ? $"{c.Agente.User?.Nombre} {c.Agente.User?.Apellido}" : null,
        PropietarioRefId = c.PropietarioRefId,
        InquilinoRefId = c.InquilinoRefId,
        LocadorNombre = c.LocadorNombre,
        LocadorApellido = c.LocadorApellido,
        LocadorDni = c.LocadorDni,
        LocadorEmail = c.LocadorEmail,
        LocadorTelefono = c.LocadorTelefono,
        LocadorDomicilio = c.LocadorDomicilio,
        LocadorBanco = c.LocadorBanco,
        LocadorCbu = c.LocadorCbu,
        LocadorCuit = c.LocadorCuit,
        LocatarioNombre = c.LocatarioNombre,
        LocatarioApellido = c.LocatarioApellido,
        LocatarioDni = c.LocatarioDni,
        LocatarioEmail = c.LocatarioEmail,
        LocatarioTelefono = c.LocatarioTelefono,
        GaranteNombre = c.GaranteNombre,
        GaranteApellido = c.GaranteApellido,
        GaranteDni = c.GaranteDni,
        GaranteTelefono = c.GaranteTelefono,
        MontoBase = c.MontoBase,
        Moneda = c.Moneda.ToString(),
        TipoAjuste = c.TipoAjuste.ToString(),
        PeriodicidadAjusteMeses = c.PeriodicidadAjusteMeses,
        DiaVencimientoPago = c.DiaVencimientoPago,
        ComisionLocadorPorcentaje = c.ComisionLocadorPorcentaje,
        ComisionLocadorMonto = c.ComisionLocadorMonto,
        ComisionLocatarioPorcentaje = c.ComisionLocatarioPorcentaje,
        ComisionLocatarioMonto = c.ComisionLocatarioMonto,
        AdministracionCobros = c.AdministracionCobros,
        PorcentajeAjuste = c.PorcentajeAjuste,
        MontoActual = c.MontoActual,
        FechaUltimoAjuste = c.FechaUltimoAjuste,
        FechaInicio = c.FechaInicio,
        FechaFin = c.FechaFin,
        FechaEscrituracion = c.FechaEscrituracion,
        MotivoRescision = c.MotivoRescision,
        FechaRescision = c.FechaRescision,
        MotivoAnulacion = c.MotivoAnulacion,
        FechaAnulacion = c.FechaAnulacion,
        Observaciones = c.Observaciones,
        ArchivoUrl = c.ArchivoUrl,
        Pagos = c.Pagos.Select(MapPagoToDto).ToList(),
        Ajustes = c.Ajustes.Select(a => new AjusteContratoDto
        {
            Id = a.Id,
            ContratoId = a.ContratoId,
            FechaAplicacion = a.FechaAplicacion,
            MontoPrevio = a.MontoPrevio,
            MontoNuevo = a.MontoNuevo,
            Porcentaje = a.Porcentaje,
            TipoAjuste = a.TipoAjuste,
            Observaciones = a.Observaciones,
        }).ToList(),
        FechaCreacion = c.FechaCreacion,
        FechaActualizacion = c.FechaActualizacion,
    };

    private static PagoDto MapPagoToDto(Pago p) => new()
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
}
