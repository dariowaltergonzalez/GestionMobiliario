using GestionInmobiliaria.Aplicacion.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Corre una vez por día (como mínimo) <see cref="IIndiceIclService.ActualizarAsync"/> para mantener
/// al día la serie ICL del BCRA. El mismo método se puede disparar a mano — nunca hay lógica
/// duplicada entre el disparo automático y el manual.
/// </summary>
public class IndiceIclSchedulerService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IndiceIclSchedulerService> _logger;

    public IndiceIclSchedulerService(IServiceScopeFactory scopeFactory, ILogger<IndiceIclSchedulerService> logger)
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
                var servicio = scope.ServiceProvider.GetRequiredService<IIndiceIclService>();
                var cantidad = await servicio.ActualizarAsync();
                _logger.LogInformation("IndiceIclSchedulerService: {Cantidad} valores nuevos.", cantidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando el Índice ICL (BCRA).");
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (TaskCanceledException) { }
        }
    }
}
