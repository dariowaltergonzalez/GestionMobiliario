using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionInmobiliaria.Aplicacion.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Implementación de <see cref="IReciboIaService"/> con Gemini (Google AI), elegido para arrancar
/// porque tiene tier gratis real (sin tarjeta) — ver docs/logica-negocio.md, sección LIQUIDACIÓN. Si
/// se cambia de proveedor más adelante, esta es la ÚNICA clase que hay que reemplazar/agregar; el
/// resto del sistema solo conoce <see cref="IReciboIaService"/>.
/// </summary>
public class GeminiReciboIaService : IReciboIaService
{
    private const string Modelo = "gemini-3.6-flash";

    private const string Prompt = """
        Esta es una foto o captura de pantalla de un comprobante de transferencia bancaria o pago
        (puede ser de Mercado Pago, home banking, o cualquier billetera/banco argentino). Extraé
        estos datos y devolvé SOLO el JSON pedido, sin texto adicional:
        - monto: el importe transferido, como número (sin símbolo de moneda ni separadores de miles).
        - fecha: la fecha de la operación, en formato YYYY-MM-DD.
        - cbuCvuDestino: el CBU o CVU de destino, si figura.
        - entidadDestino: el nombre del banco o billetera destino (ej. "Banco Nación", "Mercado Pago").
        - numeroOperacion: el número de operación/comprobante/transacción.
        Si no encontrás alguno de estos datos con confianza, devolvé null en ese campo — nunca
        inventes un valor.
        """;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiReciboIaService> _logger;

    public GeminiReciboIaService(
        IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<GeminiReciboIaService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<DatosComprobante> ExtraerDatosAsync(Stream imagen, string contentType)
    {
        var vacio = new DatosComprobante(null, null, null, null, null);

        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("GeminiReciboIaService: falta configurar Gemini:ApiKey, no se extraen datos.");
            return vacio;
        }

        try
        {
            await using var buffer = new MemoryStream();
            await imagen.CopyToAsync(buffer);
            var base64 = Convert.ToBase64String(buffer.ToArray());

            var request = new GeminiRequest
            {
                Contents =
                [
                    new GeminiContent
                    {
                        Parts =
                        [
                            new GeminiPart { Text = Prompt },
                            new GeminiPart { InlineData = new GeminiInlineData { MimeType = contentType, Data = base64 } },
                        ],
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
                            ["monto"] = new() { Type = "NUMBER", Nullable = true },
                            ["fecha"] = new() { Type = "STRING", Nullable = true },
                            ["cbuCvuDestino"] = new() { Type = "STRING", Nullable = true },
                            ["entidadDestino"] = new() { Type = "STRING", Nullable = true },
                            ["numeroOperacion"] = new() { Type = "STRING", Nullable = true },
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
                _logger.LogError("GeminiReciboIaService: la API devolvió {Status}. {Body}", respuesta.StatusCode, body);
                return vacio;
            }

            var resultado = await respuesta.Content.ReadFromJsonAsync<GeminiResponse>();
            var texto = resultado?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(texto))
            {
                _logger.LogWarning("GeminiReciboIaService: la respuesta no trajo ningún texto para parsear.");
                return vacio;
            }

            var extraido = JsonSerializer.Deserialize<ExtraccionJson>(texto, JsonOptions);
            if (extraido is null) return vacio;

            DateTime? fecha = null;
            if (!string.IsNullOrWhiteSpace(extraido.Fecha) &&
                DateTime.TryParse(extraido.Fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f))
                fecha = f;

            return new DatosComprobante(
                extraido.Monto,
                fecha,
                extraido.CbuCvuDestino,
                extraido.EntidadDestino,
                extraido.NumeroOperacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GeminiReciboIaService: error extrayendo datos del comprobante.");
            return vacio;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private class ExtraccionJson
    {
        public decimal? Monto { get; set; }
        public string? Fecha { get; set; }
        public string? CbuCvuDestino { get; set; }
        public string? EntidadDestino { get; set; }
        public string? NumeroOperacion { get; set; }
    }

    // DTOs internos del request/response de la API REST de Gemini (v1beta), formato JSON en camelCase
    // (mapeo estándar de Google para protobuf-JSON).

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

        [JsonPropertyName("inlineData")]
        public GeminiInlineData? InlineData { get; set; }
    }

    private class GeminiInlineData
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
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
