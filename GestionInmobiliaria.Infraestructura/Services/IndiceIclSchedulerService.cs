using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
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
    private const string Origen = "Scheduler: Índice ICL";
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var logs = scope.ServiceProvider.GetRequiredService<IAppLogRepository>();
            try
            {
                var servicio = scope.ServiceProvider.GetRequiredService<IIndiceIclService>();
                var cantidad = await servicio.ActualizarAsync();
                _logger.LogInformation("IndiceIclSchedulerService: {Cantidad} valores nuevos.", cantidad);
                if (cantidad > 0)
                    await logs.RegistrarAsync(NivelLog.Info, Origen, $"{cantidad} valor(es) nuevo(s) del BCRA.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando el Índice ICL (BCRA).");
                await logs.RegistrarAsync(NivelLog.Error, Origen, "No se pudo actualizar el Índice ICL del BCRA.", ex.ToString());
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (TaskCanceledException) { }
        }
    }
}
