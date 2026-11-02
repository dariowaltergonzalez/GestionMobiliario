using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Corre una vez por día (como mínimo) <see cref="ITasaMoratoriaService.ActualizarAsync"/> para
/// mantener al día la serie TIM del BCRA. El mismo método se puede disparar a mano desde
/// TasasMoratoriasController — nunca hay lógica duplicada entre el disparo automático y el manual.
/// </summary>
public class TasaMoratoriaSchedulerService : BackgroundService
{
    private const string Origen = "Scheduler: Tasa Moratoria (TIM)";
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TasaMoratoriaSchedulerService> _logger;

    public TasaMoratoriaSchedulerService(IServiceScopeFactory scopeFactory, ILogger<TasaMoratoriaSchedulerService> logger)
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
                var servicio = scope.ServiceProvider.GetRequiredService<ITasaMoratoriaService>();
                var cantidad = await servicio.ActualizarAsync();
                _logger.LogInformation("TasaMoratoriaSchedulerService: {Cantidad} valores nuevos.", cantidad);
                if (cantidad > 0)
                    await logs.RegistrarAsync(NivelLog.Info, Origen, $"{cantidad} valor(es) nuevo(s) del BCRA.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando la Tasa de Intereses Moratorios (BCRA).");
                await logs.RegistrarAsync(NivelLog.Error, Origen, "No se pudo actualizar la Tasa de Intereses Moratorios del BCRA.", ex.ToString());
            }

            try { await Task.Delay(Intervalo, stoppingToken); }
            catch (TaskCanceledException) { }
        }
    }
}
