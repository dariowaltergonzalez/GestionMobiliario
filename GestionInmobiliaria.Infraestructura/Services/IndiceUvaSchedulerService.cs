using GestionInmobiliaria.Aplicacion.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Corre una vez por día (como mínimo) <see cref="IIndiceUvaService.ActualizarAsync"/> para mantener
/// al día la serie UVA del BCRA. El mismo método se puede disparar a mano — nunca hay lógica
/// duplicada entre el disparo automático y el manual.
/// </summary>
public class IndiceUvaSchedulerService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IndiceUvaSchedulerService> _logger;

    public IndiceUvaSchedulerService(IServiceScopeFactory scopeFactory, ILogger<IndiceUvaSchedulerService> logger)
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
                var servicio = scope.ServiceProvider.GetRequiredService<IIndiceUvaService>();
                var cantidad = await servicio.ActualizarAsync();
                _logger.LogInformation("IndiceUvaSchedulerService: {Cantidad} valores nuevos.", cantidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando el Índice UVA (BCRA).");
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (TaskCanceledException) { }
        }
    }
}
