using GestionInmobiliaria.Aplicacion.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Corre una vez por día (como mínimo) <see cref="IIndiceIpcService.ActualizarAsync"/> para mantener
/// al día la serie IPC del INDEC. La serie es mensual (el INDEC la publica una vez al mes), así que
/// la mayoría de los días no va a haber nada nuevo — igual se chequea todos los días, mismo patrón
/// que el resto de los índices, para no depender de una fecha exacta de publicación.
/// </summary>
public class IndiceIpcSchedulerService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IndiceIpcSchedulerService> _logger;

    public IndiceIpcSchedulerService(IServiceScopeFactory scopeFactory, ILogger<IndiceIpcSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var servicio = scope.ServiceProvider.GetRequiredService<IIndiceIpcService>();
                var cantidad = await servicio.ActualizarAsync();
                _logger.LogInformation("IndiceIpcSchedulerService: {Cantidad} valores nuevos.", cantidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando el Índice IPC (INDEC).");
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (TaskCanceledException) { }
        }
    }
}
