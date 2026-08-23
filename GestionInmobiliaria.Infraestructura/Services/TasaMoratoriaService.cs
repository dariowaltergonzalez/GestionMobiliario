using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Trae y guarda la serie "Tasa de Intereses Moratorios" (TIM, idVariable=1197) que publica el BCRA
/// en api.bcra.gob.ar. Es un índice acumulado (no una tasa % periódica) — ver
/// docs/logica-negocio.md, sección PUNITORIOS, para el detalle de cómo se usa.
/// </summary>
public class TasaMoratoriaService : ITasaMoratoriaService
{
    private const int IdVariableTim = 1197;
    private const int PageSize = 3000;

    // El disparo manual (endpoint) y el automático (BackgroundService) llaman al mismo método y
    // podrían solaparse — sin esto, los dos leen "tabla vacía" a la vez y chocan al insertar las
    // mismas fechas (índice único en Fecha). Con el lock, el segundo espera y no encuentra nada
    // nuevo para traer.
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TasaMoratoriaService> _logger;

    public TasaMoratoriaService(
        ApplicationDbContext context, IHttpClientFactory httpClientFactory, ILogger<TasaMoratoriaService> logger)
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
        var ultimaFecha = await _context.TasasMoratorias
            .OrderByDescending(t => t.Fecha)
            .Select(t => (DateTime?)t.Fecha)
            .FirstOrDefaultAsync();

        var client = _httpClientFactory.CreateClient("Bcra");

        var valores = ultimaFecha is null
            ? await TraerHistoricoCompletoAsync(client)
            : await TraerDesdeAsync(client, ultimaFecha.Value.Date.AddDays(1));

        if (valores.Count == 0)
        {
            _logger.LogInformation("TasaMoratoria: no hay valores nuevos para traer del BCRA.");
            return 0;
        }

        // Defensivo: por si el rango pedido pisa una fecha que ya teníamos (no debería pasar con
        // desde=ultimaFecha+1, pero el índice único en Fecha reventaría el SaveChanges si pasara).
        var fechasExistentes = (await _context.TasasMoratorias
                .Where(t => valores.Select(v => v.Fecha).Contains(t.Fecha))
                .Select(t => t.Fecha)
                .ToListAsync())
            .ToHashSet();

        var ahora = DateTime.UtcNow;
        var nuevas = valores
            .Where(v => !fechasExistentes.Contains(v.Fecha))
            .Select(v => new TasaMoratoria
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

        _context.TasasMoratorias.AddRange(nuevas);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "TasaMoratoria actualizada: {Cantidad} valores nuevos ({Desde} a {Hasta}).",
            nuevas.Count, nuevas.Min(t => t.Fecha).ToString("yyyy-MM-dd"), nuevas.Max(t => t.Fecha).ToString("yyyy-MM-dd"));

        return nuevas.Count;
    }

    private async Task<List<ValorDiario>> TraerHistoricoCompletoAsync(HttpClient client)
    {
        var todas = new List<ValorDiario>();
        var offset = 0;
        while (true)
        {
            var pagina = await TraerUrlAsync(client, $"estadisticas/v4.0/monetarias/{IdVariableTim}?limit={PageSize}&offset={offset}");
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
        var url = $"estadisticas/v4.0/monetarias/{IdVariableTim}" +
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

    // DTOs internos, solo para deserializar la respuesta de api.bcra.gob.ar — no se exponen afuera.
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
