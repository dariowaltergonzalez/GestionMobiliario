using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionInmobiliaria.Aplicacion.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Implementación de <see cref="IPropiedadesBotService"/> con Gemini — mismo patrón que
/// <see cref="GeminiReciboIaService"/> (JSON forzado con responseSchema), pero interpretando texto en
/// vez de una imagen. Ver docs/logica-negocio.md, NOTIFICACIONES → "WhatsApp — bot de propiedades".
/// </summary>
public class GeminiPropiedadesBotService : IPropiedadesBotService
{
    private const string Modelo = "gemini-3.6-flash";

    private const string Prompt = """
        Sos el asistente de una inmobiliaria que recibe mensajes de WhatsApp. Analizá el siguiente
        mensaje y decidí si la persona está buscando una propiedad disponible (alquiler o venta).

        Si NO es una búsqueda de propiedades (saludo, pregunta genérica, otra cosa), respondé
        esBusquedaPropiedades=false y dejá el resto en null.

        Si SÍ es una búsqueda, extraé los filtros que se puedan identificar. Dejá en null lo que no se
        mencione — nunca inventes un valor ni asumas uno que no esté en el mensaje.
        - operacion: "Alquiler" o "Venta" (null si no se especifica).
        - tipo: uno de "Departamento", "Casa", "Local", "Oficina", "Terreno", "Galpon", "PH", "Otro".
        - dormitoriosMinimo: número de dormitorios mínimo pedido, si lo menciona.
        - cochera, pileta, mascotas: true solo si lo pide explícitamente, si no null.

        Mensaje: "{0}"
        """;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiPropiedadesBotService> _logger;

    public GeminiPropiedadesBotService(
        IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<GeminiPropiedadesBotService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<ConsultaPropiedades> InterpretarAsync(string mensaje)
    {
        var vacio = new ConsultaPropiedades(false, null, null, null, null, null, null, null, null);

        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("GeminiPropiedadesBotService: falta configurar Gemini:ApiKey.");
            return vacio;
        }

        try
        {
            var request = new GeminiRequest
            {
                Contents =
                [
                    new GeminiContent
                    {
                        Parts = [new GeminiPart { Text = string.Format(Prompt, mensaje) }],
                    },
                ],
                GenerationConfig = new GeminiGenerationConfig
                {
                    ResponseMimeType = "application/json",
                    ResponseSchema = new GeminiSchema
                    {
                        Type = "OBJECT",
                        Properties = new Dictionary<string, GeminiSchema>
                        {
                            ["esBusquedaPropiedades"] = new() { Type = "BOOLEAN" },
                            ["barrio"] = new() { Type = "STRING", Nullable = true },
                            ["ciudad"] = new() { Type = "STRING", Nullable = true },
                            ["operacion"] = new() { Type = "STRING", Nullable = true },
                            ["tipo"] = new() { Type = "STRING", Nullable = true },
                            ["dormitoriosMinimo"] = new() { Type = "INTEGER", Nullable = true },
                            ["cochera"] = new() { Type = "BOOLEAN", Nullable = true },
                            ["pileta"] = new() { Type = "BOOLEAN", Nullable = true },
                            ["mascotas"] = new() { Type = "BOOLEAN", Nullable = true },
                        },
                    },
                },
            };

            var client = _httpClientFactory.CreateClient("Gemini");
            var url = $"v1beta/models/{Modelo}:generateContent?key={apiKey}";

            using var respuesta = await client.PostAsJsonAsync(url, request);
            if (!respuesta.IsSuccessStatusCode)
            {
                var body = await respuesta.Content.ReadAsStringAsync();
                _logger.LogError("GeminiPropiedadesBotService: la API devolvió {Status}. {Body}", respuesta.StatusCode, body);
                return vacio;
            }

            var resultado = await respuesta.Content.ReadFromJsonAsync<GeminiResponse>();
            var texto = resultado?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(texto))
                return vacio;

            var extraido = JsonSerializer.Deserialize<ExtraccionJson>(texto, JsonOptions);
            if (extraido is null) return vacio;

            return new ConsultaPropiedades(
                extraido.EsBusquedaPropiedades,
                extraido.Barrio,
                extraido.Ciudad,
                extraido.Operacion,
                extraido.Tipo,
                extraido.DormitoriosMinimo,
                extraido.Cochera,
                extraido.Pileta,
                extraido.Mascotas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeminiPropiedadesBotService: error interpretando el mensaje.");
            return vacio;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private class ExtraccionJson
    {
        public bool EsBusquedaPropiedades { get; set; }
        public string? Barrio { get; set; }
        public string? Ciudad { get; set; }
        public string? Operacion { get; set; }
        public string? Tipo { get; set; }
        public int? DormitoriosMinimo { get; set; }
        public bool? Cochera { get; set; }
        public bool? Pileta { get; set; }
        public bool? Mascotas { get; set; }
    }

    // DTOs internos del request/response de la API REST de Gemini (v1beta) — mismos que
    // GeminiReciboIaService, duplicados a propósito para no crear un acoplamiento entre dos features
    // que no tienen por qué compartir código más allá del proveedor.

    private class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class GeminiGenerationConfig
    {
        [JsonPropertyName("responseMimeType")]
        public string? ResponseMimeType { get; set; }

        [JsonPropertyName("responseSchema")]
        public GeminiSchema? ResponseSchema { get; set; }
    }

    private class GeminiSchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("nullable")]
        public bool? Nullable { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, GeminiSchema>? Properties { get; set; }
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}
