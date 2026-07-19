using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Infraestructura.Persistencia;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace GestionInmobiliaria.Infraestructura.Services;

public class SmtpEmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(ApplicationDbContext context, ILogger<SmtpEmailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task EnviarAsync(string destinatario, string asunto, string cuerpo,
        byte[]? adjunto = null, string? nombreAdjunto = null)
    {
        var mensaje = new EmailMessage
        {
            Destinatario = destinatario,
            Asunto = asunto,
            Cuerpo = cuerpo,
        };

        if (adjunto is { Length: > 0 } && !string.IsNullOrWhiteSpace(nombreAdjunto))
            mensaje.Adjuntos.Add(new EmailAdjunto { NombreArchivo = nombreAdjunto, Contenido = adjunto });

        return EnviarAsync(mensaje);
    }

    public async Task EnviarAsync(EmailMessage mensaje)
    {
        var config = await _context.ConfiguracionEmpresa.IgnoreQueryFilters().FirstOrDefaultAsync();

        if (config is null || !config.EmailHabilitado)
        {
            _logger.LogInformation("Email deshabilitado o sin configuración. Se omite envío a {Destinatario}: {Asunto}",
                mensaje.Destinatario, mensaje.Asunto);
            return;
        }

        if (string.IsNullOrWhiteSpace(config.EmailSmtpHost) ||
            string.IsNullOrWhiteSpace(config.EmailSmtpUsuario) ||
            string.IsNullOrWhiteSpace(config.EmailSmtpPassword))
        {
            _logger.LogWarning("Configuración SMTP incompleta (Host/Usuario/Password). Se omite envío a {Destinatario}",
                mensaje.Destinatario);
            return;
        }

        var remitente = config.EmailNombreRemitente ?? config.NombreComercial;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(remitente, config.EmailSmtpUsuario));
        mime.To.Add(new MailboxAddress(mensaje.NombreDestinatario ?? mensaje.Destinatario, mensaje.Destinatario));

        foreach (var cc in mensaje.Cc)
            mime.Cc.Add(MailboxAddress.Parse(cc));

        mime.Subject = mensaje.Asunto;

        var builder = new BodyBuilder { HtmlBody = mensaje.Cuerpo };

        foreach (var adj in mensaje.Adjuntos)
            builder.Attachments.Add(adj.NombreArchivo, adj.Contenido, ContentType.Parse(adj.TipoMime));

        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;
        await client.ConnectAsync(config.EmailSmtpHost, config.EmailSmtpPuerto, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(config.EmailSmtpUsuario, config.EmailSmtpPassword);
        await client.SendAsync(mime);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email enviado a {Destinatario}: {Asunto}", mensaje.Destinatario, mensaje.Asunto);
    }
}
