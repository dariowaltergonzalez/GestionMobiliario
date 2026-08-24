using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Revisa una vez por día (como mínimo) los contratos Vigentes con AjusteAutomatico=true y les
/// aplica el ajuste correspondiente si ya se cumplió PeriodicidadAjusteMeses desde el último ajuste
/// (o desde FechaInicio si nunca tuvo uno). Solo actualiza cuotas Pendiente/Atrasado — nunca
/// cuotas ya Pagadas. Ver docs/logica-negocio.md, sección PENDIENTES GENERALES → "Automatizar el
/// ajuste periódico de cuotas". No corre dentro de un request HTTP, así que no hay tenant "activo" —
/// mismo patrón que RecordatorioVencimientoService/TasaMoratoriaSchedulerService.
/// </summary>
public class AjusteAutomaticoService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AjusteAutomaticoService> _logger;

    public AjusteAutomaticoService(IServiceScopeFactory scopeFactory, ILogger<AjusteAutomaticoService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (revisados, aplicados) = await RevisarContratosAsync(stoppingToken);
                _logger.LogInformation(
                    "AjusteAutomaticoService: ciclo OK. {Revisados} contratos con ajuste automático revisados, {Aplicados} ajustes aplicados.",
                    revisados, aplicados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revisando ajustes automáticos de contratos.");
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (TaskCanceledException) { }
        }
    }

    private async Task<(int Revisados, int Aplicados)> RevisarContratosAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificacion = scope.ServiceProvider.GetRequiredService<INotificacionService>();

        var tenants = await ctx.Tenants.IgnoreQueryFilters().Where(t => t.Activo).ToListAsync(ct);
        var hoy = DateTime.UtcNow.Date;
        var revisados = 0;
        var aplicados = 0;

        foreach (var tenant in tenants)
        {
            var contratos = await ctx.Contratos.IgnoreQueryFilters()
                .Include(c => c.Propiedad)
                .Where(c => c.TenantId == tenant.Id && c.Activo &&
                            c.Estado == EstadoContrato.Vigente &&
                            c.AjusteAutomatico &&
                            c.PeriodicidadAjusteMeses != null)
                .ToListAsync(ct);

            revisados += contratos.Count;

            foreach (var contrato in contratos)
            {
                var fechaBase = contrato.FechaUltimoAjuste ?? contrato.FechaInicio;
                var proximoAjuste = fechaBase.AddMonths(contrato.PeriodicidadAjusteMeses!.Value);
                if (hoy < proximoAjuste.Date) continue;

                var (montoNuevo, detalle, porcentaje) = await CalcularNuevoMontoAsync(ctx, contrato, fechaBase, hoy);
                if (montoNuevo is null || montoNuevo.Value <= 0) continue; // Fijo/Otro, o falta el índice

                var montoAnterior = contrato.MontoActual;

                contrato.MontoActual = montoNuevo.Value;
                contrato.FechaUltimoAjuste = hoy;
                contrato.FechaActualizacion = hoy;

                var pagosPendientes = await ctx.Pagos.IgnoreQueryFilters()
                    .Where(p => p.TenantId == tenant.Id && p.Activo && p.ContratoId == contrato.Id &&
                                (p.Estado == EstadoPago.Pendiente || p.Estado == EstadoPago.Atrasado))
                    .ToListAsync(ct);
                foreach (var pago in pagosPendientes)
                {
                    pago.MontoEsperado = montoNuevo.Value;
                    pago.FechaActualizacion = hoy;
                }

                ctx.AjustesContrato.Add(new AjusteContrato
                {
                    ContratoId = contrato.Id,
                    FechaAplicacion = hoy,
                    MontoPrevio = montoAnterior,
                    MontoNuevo = montoNuevo.Value,
                    Porcentaje = porcentaje,
                    TipoAjuste = contrato.TipoAjuste.ToString(),
                    DetalleIndiceUsado = detalle,
                    Automatico = true,
                    TenantId = tenant.Id,
                });

                await ctx.SaveChangesAsync(ct);
                aplicados++;

                _logger.LogInformation(
                    "Ajuste automático aplicado. Contrato={Contrato} MontoAnterior={MontoAnterior} MontoNuevo={MontoNuevo} Cuotas={Cuotas}",
                    contrato.Codigo, montoAnterior, montoNuevo.Value, pagosPendientes.Count);

                await NotificarAsync(ctx, notificacion, contrato, tenant.Id, montoAnterior, montoNuevo.Value, ct);
            }
        }

        return (revisados, aplicados);
    }

    private async Task<(decimal? Monto, string? Detalle, decimal? Porcentaje)> CalcularNuevoMontoAsync(
        ApplicationDbContext ctx, Contrato contrato, DateTime fechaBase, DateTime hoy)
    {
        switch (contrato.TipoAjuste)
        {
            case TipoAjuste.Porcentaje:
                if (contrato.PorcentajeAjuste is not { } porcentaje || porcentaje == 0)
                    return (null, null, null);
                var montoPorcentaje = Math.Round(contrato.MontoActual * (1 + porcentaje / 100), 2);
                return (montoPorcentaje, $"{porcentaje}% aplicado automáticamente", porcentaje);

            case TipoAjuste.IndiceICL:
                return await CalcularPorIndiceAsync(contrato.MontoActual, fechaBase, hoy,
                    "ICL BCRA", f => ValorIclEnFechaAsync(ctx, f));

            case TipoAjuste.IndiceUVA:
                return await CalcularPorIndiceAsync(contrato.MontoActual, fechaBase, hoy,
                    "UVA BCRA", f => ValorUvaEnFechaAsync(ctx, f));

            case TipoAjuste.IndiceIPC:
                return await CalcularPorIndiceAsync(contrato.MontoActual, fechaBase, hoy,
                    "IPC INDEC", f => ValorIpcEnFechaAsync(ctx, f));

            default: // Fijo / Otro: no hay ningún cálculo automático definido, queda 100% manual
                return (null, null, null);
        }
    }

    private static async Task<(decimal? Monto, string? Detalle, decimal? Porcentaje)> CalcularPorIndiceAsync(
        decimal montoActual, DateTime fechaBase, DateTime hoy, string etiqueta,
        Func<DateTime, Task<(DateTime Fecha, decimal Valor)?>> valorEnFecha)
    {
        var valorBase = await valorEnFecha(fechaBase);
        var valorHoy = await valorEnFecha(hoy);
        if (valorBase is null || valorHoy is null || valorBase.Value.Valor == 0)
            return (null, null, null); // sin datos del índice, no se inventa un número

        var coeficiente = valorHoy.Value.Valor / valorBase.Value.Valor;
        var montoNuevo = Math.Round(montoActual * coeficiente, 2);
        // Se usa la fecha REAL del valor encontrado (no `hoy`/`fechaBase`) porque para índices
        // mensuales como el IPC el último valor disponible puede ser de semanas atrás.
        var detalle = $"{etiqueta}: {valorHoy.Value.Valor:N4} ({valorHoy.Value.Fecha:dd/MM/yyyy}) / " +
                       $"{valorBase.Value.Valor:N4} ({valorBase.Value.Fecha:dd/MM/yyyy})";
        return (montoNuevo, detalle, Math.Round((coeficiente - 1) * 100, 2));
    }

    private static async Task<(DateTime Fecha, decimal Valor)?> ValorIclEnFechaAsync(ApplicationDbContext ctx, DateTime fecha)
    {
        var fila = await ctx.IndicesIcl
            .Where(t => t.Fecha <= fecha)
            .OrderByDescending(t => t.Fecha)
            .Select(t => new { t.Fecha, t.Valor })
            .FirstOrDefaultAsync();
        return fila is null ? null : (fila.Fecha, fila.Valor);
    }

    private static async Task<(DateTime Fecha, decimal Valor)?> ValorUvaEnFechaAsync(ApplicationDbContext ctx, DateTime fecha)
    {
        var fila = await ctx.IndicesUva
            .Where(t => t.Fecha <= fecha)
            .OrderByDescending(t => t.Fecha)
            .Select(t => new { t.Fecha, t.Valor })
            .FirstOrDefaultAsync();
        return fila is null ? null : (fila.Fecha, fila.Valor);
    }

    private static async Task<(DateTime Fecha, decimal Valor)?> ValorIpcEnFechaAsync(ApplicationDbContext ctx, DateTime fecha)
    {
        var fila = await ctx.IndicesIpc
            .Where(t => t.Fecha <= fecha)
            .OrderByDescending(t => t.Fecha)
            .Select(t => new { t.Fecha, t.Valor })
            .FirstOrDefaultAsync();
        return fila is null ? null : (fila.Fecha, fila.Valor);
    }

    private async Task NotificarAsync(
        ApplicationDbContext ctx, INotificacionService notificacion, Contrato contrato,
        int tenantId, decimal montoAnterior, decimal montoNuevo, CancellationToken ct)
    {
        var asunto = $"Aviso de aumento — {contrato.Propiedad.Direccion} — {contrato.Codigo}";
        var contexto = new NotificacionContexto
        {
            TenantId = tenantId,
            EntidadRelacionada = "EmailAvisoAumento",
            EntidadRelacionadaId = contrato.Id.ToString(),
            DatosAdicionales = new { contrato = contrato.Codigo, montoAnterior, montoNuevo, automatico = true },
        };

        try
        {
            if (contrato.PropietarioRefId is { } propietarioId)
            {
                var propietario = await ctx.Propietarios.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == propietarioId && p.TenantId == tenantId, ct);
                if (propietario is not null)
                {
                    var cuerpo = BuildEmailBody(contrato, montoAnterior, montoNuevo, paraLocatario: false);
                    await notificacion.NotificarAsync(propietario, "AvisoAumento", asunto, cuerpo, contexto);
                }
            }

            if (contrato.InquilinoRefId is { } inquilinoId)
            {
                var inquilino = await ctx.Inquilinos.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(i => i.Id == inquilinoId && i.TenantId == tenantId, ct);
                if (inquilino is not null)
                {
                    var cuerpo = BuildEmailBody(contrato, montoAnterior, montoNuevo, paraLocatario: true);
                    await notificacion.NotificarAsync(inquilino, "AvisoAumento", asunto, cuerpo, contexto);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error avisando el ajuste automático. ContratoId={ContratoId}", contrato.Id);
        }
    }

    private static string BuildEmailBody(Contrato c, decimal montoAnterior, decimal montoNuevo, bool paraLocatario)
    {
        var nombreDestino = paraLocatario ? $"{c.LocatarioNombre} {c.LocatarioApellido}" : $"{c.LocadorNombre} {c.LocadorApellido}";
        var textoIntro = paraLocatario
            ? "Te informamos que se aplicó automáticamente un ajuste al monto de tu cuota, según lo pactado en el contrato:"
            : "Le informamos que se aplicó automáticamente un ajuste al monto de la cuota de su propiedad, según lo pactado en el contrato:";
        var monedaSimbolo = c.Moneda == Moneda.USD ? "U$S" : "$";

        return $"""
            <!DOCTYPE html><html><head><meta charset="utf-8"></head>
            <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:0;">
              <div style="background:#1e3a5f;padding:20px 24px;border-radius:8px 8px 0 0;">
                <h1 style="color:white;margin:0;font-size:17px;">Aviso de aumento</h1>
              </div>
              <div style="background:#f8f9fa;padding:24px;border:1px solid #e0e0e0;border-top:none;border-radius:0 0 8px 8px;">
                <p>Estimado/a <strong>{nombreDestino}</strong>,</p>
                <p>{textoIntro}</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;">
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;width:40%;">Propiedad</td><td style="padding:10px;">{c.Propiedad.Direccion}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Contrato</td><td style="padding:10px;">{c.Codigo}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Monto anterior</td><td style="padding:10px;">{monedaSimbolo} {montoAnterior:N2}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Monto nuevo</td><td style="padding:10px;color:#1e3a5f;font-weight:bold;font-size:16px;">{monedaSimbolo} {montoNuevo:N2}</td></tr>
                </table>
                <hr style="border:none;border-top:1px solid #e0e0e0;margin:20px 0;">
                <p style="color:#666;font-size:12px;">Este mensaje fue enviado automáticamente.</p>
              </div>
            </body></html>
            """;
    }
}
