using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
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
    private const string Origen = "Scheduler: Índice UVA";
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var logs = scope.ServiceProvider.GetRequiredService<IAppLogRepository>();
            try
            {
                var servicio = scope.ServiceProvider.GetRequiredService<IIndiceUvaService>();
                var cantidad = await servicio.ActualizarAsync();
                _logger.LogInformation("IndiceUvaSchedulerService: {Cantidad} valores nuevos.", cantidad);
                // No se registra en AppLog cuando no hay valores nuevos — pasa la mayoría de los días
                // y llenaría el Visor de Logs de ruido sin valor (el dueño de la inmobiliaria no tiene
                // acceso a Render/Azure, así que esta pantalla tiene que ser señal, no ruido).
                if (cantidad > 0)
                    await logs.RegistrarAsync(NivelLog.Info, Origen, $"{cantidad} valor(es) nuevo(s) del BCRA.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando el Índice UVA (BCRA).");
                await logs.RegistrarAsync(NivelLog.Error, Origen, "No se pudo actualizar el Índice UVA del BCRA.", ex.ToString());
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (TaskCanceledException) { }
        }
    }
}
