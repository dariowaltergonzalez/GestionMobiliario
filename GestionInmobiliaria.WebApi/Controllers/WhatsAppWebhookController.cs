using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace GestionInmobiliaria.WebApi.Controllers;

/// <summary>
/// Recibe los mensajes entrantes de WhatsApp (configurado en Twilio como el webhook de "cuando
/// llega un mensaje"). Twilio llama a este endpoint y espera una respuesta en formato TwiML con el
/// texto a contestar. Ver docs/logica-negocio.md, sección NOTIFICACIONES → "WhatsApp — bot de
/// búsqueda de propiedades".
/// </summary>
[ApiController]
[Route("api/whatsapp")]
[AllowAnonymous]
public class WhatsAppWebhookController : TwilioController
{
    [HttpPost("webhook")]
    public IActionResult Webhook([FromForm] SmsRequest request)
    {
        var respuesta = new MessagingResponse();
        respuesta.Message($"Recibí tu mensaje: \"{request.Body}\". El bot todavía está en construcción, pronto voy a poder ayudarte a buscar propiedades.");
        return TwiML(respuesta);
    }
}
