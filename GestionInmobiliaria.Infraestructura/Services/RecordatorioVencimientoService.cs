using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Revisa una vez por día (como mínimo) las cuotas Pendientes y avisa al inquilino por email cuando
/// faltan 7 días o 1 día para el vencimiento (tema "AvisoVencimientoProximo"). No corre dentro de un
/// request HTTP, así que no hay tenant "activo" — recorre todos los Tenants activos y filtra cada
/// consulta manualmente por TenantId (ver comentario en NotificacionService sobre el mismo problema).
/// </summary>
public class RecordatorioVencimientoService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<RecordatorioVencimientoService> _logger;

    public RecordatorioVencimientoService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<RecordatorioVencimientoService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RevisarVencimientosAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revisando vencimientos de cuotas.");
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (TaskCanceledException) { }
        }
    }

    private async Task RevisarVencimientosAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificacion = scope.ServiceProvider.GetRequiredService<INotificacionService>();

        var tenants = await ctx.Tenants.IgnoreQueryFilters()
            .Where(t => t.Activo)
            .ToListAsync(ct);

        var hoy = DateTime.UtcNow.Date;

        foreach (var tenant in tenants)
        {
            var pagos = await ctx.Pagos.IgnoreQueryFilters()
                .Include(p => p.Contrato)
                    .ThenInclude(c => c.Propiedad)
                .Where(p => p.TenantId == tenant.Id && p.Activo &&
                            p.Estado == EstadoPago.Pendiente &&
                            (!p.AvisoVencimiento7DiasEnviado || !p.AvisoVencimiento1DiaEnviado) &&
                            p.Contrato.DiaVencimientoPago != null)
                .ToListAsync(ct);

            foreach (var pago in pagos)
            {
                var fechaVencimiento = VencimientoCalculator.Calcular(pago.Periodo, pago.Contrato.DiaVencimientoPago)!.Value;
                var diasHastaVencimiento = (fechaVencimiento - hoy).Days;

                if (diasHastaVencimiento < 0)
                    continue; // ya venció, no se avisa nada (regla explícita del negocio)

                bool notificar1Dia = diasHastaVencimiento <= 1 && !pago.AvisoVencimiento1DiaEnviado;
                bool notificar7Dias = !notificar1Dia && diasHastaVencimiento <= 7 && !pago.AvisoVencimiento7DiasEnviado;

                if (!notificar1Dia && !notificar7Dias)
                    continue;

                var inquilinoRefId = pago.Contrato.InquilinoRefId;
                if (inquilinoRefId.HasValue)
                {
                    var inquilino = await ctx.Inquilinos.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(i => i.Id == inquilinoRefId.Value && i.TenantId == tenant.Id, ct);
                    if (inquilino is not null)
                    {
                        var asunto = diasHastaVencimiento <= 1
                            ? $"Recordatorio: tu cuota vence {(diasHastaVencimiento == 0 ? "hoy" : "mañana")} — {pago.Contrato.Codigo}"
                            : $"Recordatorio: tu cuota vence en {diasHastaVencimiento} días — {pago.Contrato.Codigo}";
                        var cuerpo = BuildEmailBody(pago, fechaVencimiento, diasHastaVencimiento);
                        var contexto = new NotificacionContexto
                        {
                            TenantId = tenant.Id,
                            EntidadRelacionada = "EmailAvisoVencimiento",
                            EntidadRelacionadaId = pago.Id.ToString(),
                            DatosAdicionales = new { contrato = pago.Contrato.Codigo, diasHastaVencimiento },
                        };

                        // Plantilla del sandbox de Twilio ("Appointment Reminders") — placeholder mientras
                        // se prueba el circuito de WhatsApp; al pasar a producción hay que crear una
                        // plantilla propia con el texto real ("Tu cuota de $X vence el...") y actualizar
                        // WhatsApp:PlantillaAvisoVencimiento. Ver docs/logica-negocio.md, NOTIFICACIONES.
                        var plantillaSid = _config["WhatsApp:PlantillaAvisoVencimiento"];
                        WhatsAppPlantilla? whatsApp = string.IsNullOrWhiteSpace(plantillaSid) ? null : new WhatsAppPlantilla
                        {
                            Sid = plantillaSid,
                            Variables = new[] { fechaVencimiento.ToString("dd/MM"), $"cuota {pago.Contrato.Codigo}" },
                        };

                        await notificacion.NotificarAsync(inquilino, "AvisoVencimientoProximo", asunto, cuerpo, contexto, whatsApp: whatsApp);
                    }
                }

                // Se marca el flag se haya podido notificar o no (sin ficha vinculada, tema apagado,
                // etc.) — lo que importa es no volver a evaluar esta misma ventana todos los días.
                if (notificar1Dia)
                {
                    pago.AvisoVencimiento1DiaEnviado = true;
                    pago.AvisoVencimiento7DiasEnviado = true; // esa ventana ya pasó, no tiene sentido después
                }
                else if (notificar7Dias)
                {
                    pago.AvisoVencimiento7DiasEnviado = true;
                }
            }

            if (pagos.Count > 0)
                await ctx.SaveChangesAsync(ct);
        }
    }

    private static string BuildEmailBody(Pago pago, DateTime fechaVencimiento, int diasHastaVencimiento)
    {
        var contrato = pago.Contrato;
        var monedaSimbolo = contrato.Moneda == Moneda.USD ? "U$S" : "$";
        var textoIntro = diasHastaVencimiento <= 1
            ? $"Te recordamos que tu cuota vence {(diasHastaVencimiento == 0 ? "hoy" : "mañana")}:"
            : $"Te recordamos que tu cuota vence en {diasHastaVencimiento} días:";

        return $"""
            <!DOCTYPE html><html><head><meta charset="utf-8"></head>
            <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:0;">
              <div style="background:#1e3a5f;padding:20px 24px;border-radius:8px 8px 0 0;">
                <h1 style="color:white;margin:0;font-size:17px;">Recordatorio de vencimiento</h1>
              </div>
              <div style="background:#f8f9fa;padding:24px;border:1px solid #e0e0e0;border-top:none;border-radius:0 0 8px 8px;">
                <p>Estimado/a <strong>{contrato.LocatarioNombre} {contrato.LocatarioApellido}</strong>,</p>
                <p>{textoIntro}</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;">
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;width:40%;">Propiedad</td><td style="padding:10px;">{contrato.Propiedad.Direccion}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Contrato</td><td style="padding:10px;">{contrato.Codigo}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Cuota N°</td><td style="padding:10px;">{pago.NumeroCuota}</td></tr>
                  <tr><td style="padding:10px;font-weight:bold;">Monto</td><td style="padding:10px;color:#1e3a5f;font-weight:bold;font-size:16px;">{monedaSimbolo} {pago.MontoEsperado:N2}</td></tr>
                  <tr style="background:#e8f0fe;"><td style="padding:10px;font-weight:bold;">Fecha de vencimiento</td><td style="padding:10px;">{fechaVencimiento:dd/MM/yyyy}</td></tr>
                </table>
                <hr style="border:none;border-top:1px solid #e0e0e0;margin:20px 0;">
                <p style="color:#666;font-size:12px;">Este mensaje fue enviado automáticamente.</p>
              </div>
            </body></html>
            """;
    }
}
