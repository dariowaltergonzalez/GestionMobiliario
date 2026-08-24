using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Trae y guarda la serie "Índice para Contratos de Locación" (ICL, idVariable=40) que publica el
/// BCRA en api.bcra.gob.ar. Es un índice acumulado, mismo mecanismo que TasaMoratoria (TIM) — ver
/// docs/logica-negocio.md, sección PENDIENTES GENERALES → "Automatizar el ajuste periódico de
/// cuotas". Tabla separada de TasaMoratoria a propósito (no se generaliza, ver esa sección).
/// </summary>
public class IndiceIclService : IIndiceIclService
{
    private const int IdVariableIcl = 40;
    private const int PageSize = 3000;

    // Mismo motivo que en TasaMoratoriaService: el disparo manual y el automático podrían solaparse.
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IndiceIclService> _logger;

    public IndiceIclService(
        ApplicationDbContext context, IHttpClientFactory httpClientFactory, ILogger<IndiceIclService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<int> ActualizarAsync()
    {
        await Lock.WaitAsync();
        try
        {
            return await ActualizarSinLockAsync();
        }
        finally
        {
            Lock.Release();
        }
    }

    private async Task<int> ActualizarSinLockAsync()
    {
        var ultimaFecha = await _context.IndicesIcl
            .OrderByDescending(t => t.Fecha)
            .Select(t => (DateTime?)t.Fecha)
            .FirstOrDefaultAsync();

        var client = _httpClientFactory.CreateClient("Bcra");

        var valores = ultimaFecha is null
            ? await TraerHistoricoCompletoAsync(client)
            : await TraerDesdeAsync(client, ultimaFecha.Value.Date.AddDays(1));

        if (valores.Count == 0)
        {
            _logger.LogInformation("IndiceIcl: no hay valores nuevos para traer del BCRA.");
            return 0;
        }

        var fechasExistentes = (await _context.IndicesIcl
                .Where(t => valores.Select(v => v.Fecha).Contains(t.Fecha))
                .Select(t => t.Fecha)
                .ToListAsync())
            .ToHashSet();

        var ahora = DateTime.UtcNow;
        var nuevas = valores
            .Where(v => !fechasExistentes.Contains(v.Fecha))
            .Select(v => new IndiceIcl
            {
                Fecha = v.Fecha,
                Valor = v.Valor,
                Origen = "BCRA",
                FechaConsulta = ahora,
                FechaCreacion = ahora,
                FechaActualizacion = ahora,
            })
            .ToList();

        if (nuevas.Count == 0) return 0;

        _context.IndicesIcl.AddRange(nuevas);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "IndiceIcl actualizado: {Cantidad} valores nuevos ({Desde} a {Hasta}).",
            nuevas.Count, nuevas.Min(t => t.Fecha).ToString("yyyy-MM-dd"), nuevas.Max(t => t.Fecha).ToString("yyyy-MM-dd"));

        return nuevas.Count;
    }

    private async Task<List<ValorDiario>> TraerHistoricoCompletoAsync(HttpClient client)
    {
        var todas = new List<ValorDiario>();
        var offset = 0;
        while (true)
        {
            var pagina = await TraerUrlAsync(client, $"estadisticas/v4.0/monetarias/{IdVariableIcl}?limit={PageSize}&offset={offset}");
            if (pagina.Count == 0) break;
            todas.AddRange(pagina);
            if (pagina.Count < PageSize) break;
            offset += PageSize;
        }
        return todas;
    }

    private async Task<List<ValorDiario>> TraerDesdeAsync(HttpClient client, DateTime desde)
    {
        var hasta = DateTime.UtcNow.Date;
        if (desde.Date > hasta) return [];
        var url = $"estadisticas/v4.0/monetarias/{IdVariableIcl}" +
                  $"?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}&limit={PageSize}";
        return await TraerUrlAsync(client, url);
    }

    private async Task<List<ValorDiario>> TraerUrlAsync(HttpClient client, string url)
    {
        var respuesta = await client.GetFromJsonAsync<BcraRespuesta>(url);
        var detalle = respuesta?.Results?.FirstOrDefault()?.Detalle;
        if (detalle is null) return [];
        return detalle.Select(d => new ValorDiario(d.Fecha, d.Valor)).ToList();
    }

    private record ValorDiario(DateTime Fecha, decimal Valor);

    private class BcraRespuesta
    {
        [JsonPropertyName("results")]
        public List<BcraSerie>? Results { get; set; }
    }

    private class BcraSerie
    {
        [JsonPropertyName("detalle")]
        public List<BcraDetalle>? Detalle { get; set; }
    }

    private class BcraDetalle
    {
        [JsonPropertyName("fecha")]
        public DateTime Fecha { get; set; }

        [JsonPropertyName("valor")]
        public decimal Valor { get; set; }
    }
}
