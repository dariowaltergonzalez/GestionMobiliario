using System.Text.Json;
using GestionInmobiliaria.Aplicacion.Services;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Implementación de <see cref="IWhatsAppService"/> con el Sandbox de WhatsApp de Twilio — se usa
/// mientras se prueba el circuito (no requiere aprobación de negocio de Meta). Ver
/// docs/logica-negocio.md, sección NOTIFICACIONES → "WhatsApp".
/// </summary>
public class TwilioWhatsAppService : IWhatsAppService
{
    private readonly string _from;

    public TwilioWhatsAppService(IConfiguration config)
    {
        var accountSid = config["WhatsApp:AccountSid"];
        var authToken = config["WhatsApp:AuthToken"];
        _from = config["WhatsApp:From"] ?? "";

        TwilioClient.Init(accountSid, authToken);
    }

    public async Task EnviarAsync(string telefono, WhatsAppPlantilla plantilla)
    {
        var variables = new Dictionary<string, string>();
        for (var i = 0; i < plantilla.Variables.Count; i++)
            variables[(i + 1).ToString()] = plantilla.Variables[i];

        await MessageResource.CreateAsync(
            to: new PhoneNumber($"whatsapp:{telefono}"),
            from: new PhoneNumber($"whatsapp:{_from}"),
            contentSid: plantilla.Sid,
            contentVariables: JsonSerializer.Serialize(variables)
        );
    }
}
