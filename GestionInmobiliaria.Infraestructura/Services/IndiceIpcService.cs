using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Trae y guarda la serie IPC Nacional Nivel General (serie "148.3_INIVELNAL_DICI_M_26") que publica
/// el INDEC en apis.datos.gob.ar/series — API distinta a la del BCRA, formato de respuesta distinto
/// (`{"data": [[fecha, valor], ...]}`, mensual en vez de diario). Ver docs/logica-negocio.md, sección
/// AJUSTE AUTOMÁTICO. Tabla separada a propósito (no se generaliza con IndiceIcl/IndiceUva).
/// </summary>
public class IndiceIpcService : IIndiceIpcService
{
    private const string SerieIpc = "148.3_INIVELNAL_DICI_M_26";
    private const int Limit = 1000; // la serie mensual completa (desde dic-2016) entra de sobra en una sola página

    // Mismo motivo que en IndiceIclService/TasaMoratoriaService: el disparo manual y el automático
    // podrían solaparse.
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IndiceIpcService> _logger;

    public IndiceIpcService(
        ApplicationDbContext context, IHttpClientFactory httpClientFactory, ILogger<IndiceIpcService> logger)
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
        var ultimaFecha = await _context.IndicesIpc
            .OrderByDescending(t => t.Fecha)
            .Select(t => (DateTime?)t.Fecha)
            .FirstOrDefaultAsync();

        var client = _httpClientFactory.CreateClient("Indec");
        var valores = await TraerAsync(client, ultimaFecha?.Date.AddMonths(1));

        if (valores.Count == 0)
        {
            _logger.LogInformation("IndiceIpc: no hay valores nuevos para traer del INDEC.");
            return 0;
        }

        var fechasExistentes = (await _context.IndicesIpc
                .Where(t => valores.Select(v => v.Fecha).Contains(t.Fecha))
                .Select(t => t.Fecha)
                .ToListAsync())
            .ToHashSet();

        var ahora = DateTime.UtcNow;
        var nuevas = valores
            .Where(v => !fechasExistentes.Contains(v.Fecha))
            .Select(v => new IndiceIpc
            {
                Fecha = v.Fecha,
                Valor = v.Valor,
                Origen = "INDEC",
                FechaConsulta = ahora,
                FechaCreacion = ahora,
                FechaActualizacion = ahora,
            })
            .ToList();

        if (nuevas.Count == 0) return 0;

        _context.IndicesIpc.AddRange(nuevas);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "IndiceIpc actualizado: {Cantidad} valores nuevos ({Desde} a {Hasta}).",
            nuevas.Count, nuevas.Min(t => t.Fecha).ToString("yyyy-MM-dd"), nuevas.Max(t => t.Fecha).ToString("yyyy-MM-dd"));

        return nuevas.Count;
    }

    private async Task<List<ValorMensual>> TraerAsync(HttpClient client, DateTime? desde)
    {
        var hasta = DateTime.UtcNow.Date;
        if (desde is not null && desde.Value > hasta) return [];

        var url = $"series/api/series/?ids={SerieIpc}&format=json&limit={Limit}&sort=asc" +
                  (desde is not null ? $"&start_date={desde:yyyy-MM-dd}" : "");

        var respuesta = await client.GetFromJsonAsync<IndecRespuesta>(url);
        if (respuesta?.Data is null) return [];

        var resultado = new List<ValorMensual>();
        foreach (var fila in respuesta.Data)
        {
            var elementos = fila.EnumerateArray().ToList();
            if (elementos.Count < 2) continue;
            var fechaStr = elementos[0].GetString();
            if (fechaStr is null) continue;
            var fecha = DateTime.Parse(fechaStr, CultureInfo.InvariantCulture, DateTimeStyles.None);
            var valor = elementos[1].GetDecimal();
            resultado.Add(new ValorMensual(fecha, valor));
        }
        return resultado;
    }

    private record ValorMensual(DateTime Fecha, decimal Valor);

    private class IndecRespuesta
    {
        [JsonPropertyName("data")]
        public List<JsonElement>? Data { get; set; }
    }
}
