using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace GestionInmobiliaria.WebApi.Controllers;

/// <summary>
/// Recibe los mensajes entrantes de WhatsApp (configurado en Twilio como el webhook de "cuando
/// llega un mensaje") y responde con propiedades disponibles según lo que pregunte el usuario — no
/// requiere que sea un Propietario/Inquilino registrado, es búsqueda pública (mismos datos que el
/// portal público). Ver docs/logica-negocio.md, NOTIFICACIONES → "WhatsApp — bot de propiedades".
///
/// Twilio no manda el header X-Tenant que usa el resto del sistema (no hay HttpContext de un usuario
/// logueado), así que por ahora el tenant a consultar es fijo por configuración
/// (WhatsApp:TenantIdBot) — al pasar a producción real cada tenant tendría su propio número de
/// WhatsApp Business y ahí sí se podría resolver por el campo "To" del mensaje.
/// </summary>
[ApiController]
[Route("api/whatsapp")]
[AllowAnonymous]
public class WhatsAppWebhookController : TwilioController
{
    private readonly IPropiedadesBotService _bot;
    private readonly IPropiedadRepository _propiedades;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IPropiedadesBotService bot, IPropiedadRepository propiedades, IConfiguration config,
        ILogger<WhatsAppWebhookController> logger)
    {
        _bot = bot;
        _propiedades = propiedades;
        _config = config;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromForm] SmsRequest request)
    {
        var respuesta = new MessagingResponse();
        var texto = await ArmarRespuestaAsync(request.Body ?? "");
        respuesta.Message(texto);
        return TwiML(respuesta);
    }

    private async Task<string> ArmarRespuestaAsync(string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return MensajeAyuda;

        var tenantId = _config.GetValue<int>("WhatsApp:TenantIdBot");
        if (tenantId <= 0)
        {
            _logger.LogWarning("WhatsAppWebhookController: falta configurar WhatsApp:TenantIdBot.");
            return MensajeAyuda;
        }

        ConsultaPropiedades consulta;
        try
        {
            consulta = await _bot.InterpretarAsync(mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsAppWebhookController: error interpretando el mensaje.");
            return MensajeAyuda;
        }

        if (!consulta.EsBusquedaPropiedades)
            return MensajeAyuda;

        Enum.TryParse<TipoOperacion>(consulta.Operacion, ignoreCase: true, out var operacion);
        Enum.TryParse<TipoPropiedad>(consulta.Tipo, ignoreCase: true, out var tipo);

        var resultados = await _propiedades.BuscarBotAsync(
            tenantId,
            consulta.Barrio,
            consulta.Ciudad,
            string.IsNullOrWhiteSpace(consulta.Operacion) ? null : operacion,
            string.IsNullOrWhiteSpace(consulta.Tipo) ? null : tipo,
            consulta.DormitoriosMinimo,
            consulta.Cochera,
            consulta.Pileta,
            consulta.Mascotas);

        var lista = resultados.ToList();
        if (lista.Count == 0)
            return "No encontramos propiedades disponibles con esas características por ahora. ¿Querés que probemos con otro barrio o menos filtros?";

        var items = lista.Select(FormatearPropiedad);
        return $"Encontramos {lista.Count} propiedad(es) disponible(s):\n\n{string.Join("\n\n", items)}";
    }

    private static string FormatearPropiedad(Propiedad p)
    {
        var precio = p.Operacion == TipoOperacion.Venta
            ? (p.PrecioVenta.HasValue ? $"U$S {p.PrecioVenta:N0}" : "consultar precio")
            : (p.PrecioAlquiler.HasValue ? $"$ {p.PrecioAlquiler:N0}" : "consultar precio");

        var extras = new List<string>();
        if (p.Cochera) extras.Add("cochera");
        if (p.TienePiscina) extras.Add("pileta");
        if (p.AceptaMascotas) extras.Add("acepta mascotas");
        var extrasTexto = extras.Count > 0 ? $" ({string.Join(", ", extras)})" : "";

        var dormitorios = p.Dormitorios.HasValue ? $"{p.Dormitorios} dorm." : "";

        return $"📍 {p.Direccion}{(string.IsNullOrWhiteSpace(p.Barrio) ? "" : $", {p.Barrio}")}\n{p.Tipo} · {dormitorios} · {precio}{extrasTexto}";
    }

    private const string MensajeAyuda =
        "¡Hola! Puedo ayudarte a buscar propiedades disponibles. Contame qué buscás, por ejemplo: " +
        "\"casas en alquiler en Villa Mercedes con cochera\" o \"departamentos en venta de 2 dormitorios\".";
}
