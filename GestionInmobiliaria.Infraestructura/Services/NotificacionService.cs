using System.Text.Json;
using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

public class NotificacionService : INotificacionService
{
    private readonly IEmailService _email;
    private readonly IWhatsAppService _whatsApp;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificacionService> _logger;

    public NotificacionService(IEmailService email, IWhatsAppService whatsApp, ApplicationDbContext context, ILogger<NotificacionService> logger)
    {
        _email = email;
        _whatsApp = whatsApp;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> NotificarAsync(INotificable destinatario, string tema, string asunto, string cuerpo,
        NotificacionContexto contexto, IReadOnlyList<EmailAdjunto>? adjuntos = null, WhatsAppPlantilla? whatsApp = null)
    {
        var enviadoEmail = await EnviarEmailAsync(destinatario, tema, asunto, cuerpo, contexto, adjuntos);

        // whatsApp es opcional: solo se intenta para los eventos que ya lo tienen conectado (ver
        // llamador). Un tema sin plantilla WhatsApp simplemente no manda nada por ese canal, sin
        // afectar el envío de email.
        if (whatsApp is not null)
            await EnviarWhatsAppAsync(destinatario, tema, whatsApp, contexto);

        return enviadoEmail;
    }

    private async Task<bool> EnviarEmailAsync(INotificable destinatario, string tema, string asunto, string cuerpo,
        NotificacionContexto contexto, IReadOnlyList<EmailAdjunto>? adjuntos)
    {
        string? motivoOmision =
            !destinatario.Activo ? "destinatario inactivo" :
            string.IsNullOrWhiteSpace(destinatario.Email) ? "sin email cargado" :
            !DebeEnviar(destinatario.Notificaciones, tema) ? "tema no habilitado" :
            null;

        if (motivoOmision is not null)
        {
            await RegistrarAsync("OMITIDO", tema, destinatario.Email, contexto, motivoOmision);
            return false;
        }

        try
        {
            var mensaje = new EmailMessage
            {
                Destinatario = destinatario.Email!,
                Asunto = asunto,
                Cuerpo = cuerpo,
            };
            if (adjuntos is { Count: > 0 })
                mensaje.Adjuntos.AddRange(adjuntos);

            await _email.EnviarAsync(mensaje);
            await RegistrarAsync("ENVIADO", tema, destinatario.Email, contexto);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación. Tema={Tema} Destinatario={Email}", tema, destinatario.Email);
            await RegistrarAsync("ERROR", tema, destinatario.Email, contexto, ex.Message);
            return false;
        }
    }

    private async Task EnviarWhatsAppAsync(INotificable destinatario, string tema, WhatsAppPlantilla plantilla, NotificacionContexto contexto)
    {
        string? motivoOmision =
            !destinatario.Activo ? "destinatario inactivo" :
            string.IsNullOrWhiteSpace(destinatario.Telefono) ? "sin teléfono cargado" :
            !DebeEnviar(destinatario.NotificacionesWhatsApp, tema) ? "tema no habilitado" :
            null;

        if (motivoOmision is not null)
        {
            await RegistrarAsync("OMITIDO_WHATSAPP", tema, destinatario.Telefono, contexto, motivoOmision);
            return;
        }

        try
        {
            await _whatsApp.EnviarAsync(destinatario.Telefono!, plantilla);
            await RegistrarAsync("ENVIADO_WHATSAPP", tema, destinatario.Telefono, contexto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar WhatsApp. Tema={Tema} Destinatario={Telefono}", tema, destinatario.Telefono);
            await RegistrarAsync("ERROR_WHATSAPP", tema, destinatario.Telefono, contexto, ex.Message);
        }
    }

    private async Task RegistrarAsync(string resultado, string tema, string? destinatarioIdentificador, NotificacionContexto contexto, string? motivo = null)
    {
        _logger.LogInformation("Notificación {Resultado}. Tema={Tema} Destinatario={Destinatario} Motivo={Motivo}",
            resultado, tema, destinatarioIdentificador, motivo);

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = contexto.EntidadRelacionada,
            Action = resultado,
            EntityId = contexto.EntidadRelacionadaId,
            UserId = contexto.UserId,
            UserName = contexto.UserName,
            NewValues = JsonSerializer.Serialize(new { tema, destinatario = destinatarioIdentificador, motivo, detalle = contexto.DatosAdicionales }),
            Timestamp = DateTime.UtcNow,
            TenantId = contexto.TenantId,
        });

        try { await _context.SaveChangesAsync(); }
        catch (Exception ex) { _logger.LogError(ex, "No se pudo registrar la auditoría de la notificación."); }
    }

    private static bool DebeEnviar(string? notificacionesJson, string tema)
    {
        if (string.IsNullOrWhiteSpace(notificacionesJson)) return false;
        var prefs = JsonSerializer.Deserialize<Dictionary<string, bool>>(notificacionesJson);
        return prefs is not null && prefs.TryGetValue(tema, out var habilitado) && habilitado;
    }
}
