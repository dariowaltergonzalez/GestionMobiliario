using GestionInmobiliaria.Aplicacion.DTOs;

namespace GestionInmobiliaria.Aplicacion.Services;

public interface IEmailService
{
    Task EnviarAsync(EmailMessage mensaje);

    Task EnviarAsync(string destinatario, string asunto, string cuerpo,
        byte[]? adjunto = null, string? nombreAdjunto = null);
}
