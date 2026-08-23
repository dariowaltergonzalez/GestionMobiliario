using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Calcula el recargo por mora de una cuota atrasada. Ver docs/logica-negocio.md, sección
/// PUNITORIOS, para el detalle de la fórmula y por qué se eligió así.
/// </summary>
public class PunitorioService : IPunitorioService
{
    private static readonly PunitorioResultado Cero = new(0, 0, null);

    private readonly ApplicationDbContext _context;

    public PunitorioService(ApplicationDbContext context) => _context = context;

    public async Task<PunitorioResultado> CalcularAsync(Pago pago)
    {
        if (pago.Estado != EstadoPago.Pendiente && pago.Estado != EstadoPago.Atrasado)
            return Cero;

        var contrato = pago.Contrato;
        if (!contrato.AplicaPunitorios)
            return Cero;

        var fechaVencimiento = VencimientoCalculator.Calcular(pago.Periodo, contrato.DiaVencimientoPago);
        if (fechaVencimiento is null)
            return Cero;

        var hoy = DateTime.UtcNow.Date;
        var diasAtraso = (hoy - fechaVencimiento.Value).Days;
        if (diasAtraso <= 0)
            return Cero;

        var monto = pago.MontoPagado ?? pago.MontoEsperado;

        if (contrato.PunitorioPorcentaje is { } porcentaje && porcentaje > 0)
        {
            var recargoFijo = monto * (porcentaje / 100) * diasAtraso;
            var detalleFijo = $"{porcentaje}%/día fijo del contrato × {diasAtraso} días de atraso " +
                               $"(vencimiento {fechaVencimiento.Value:dd/MM/yyyy})";
            return new PunitorioResultado(recargoFijo, diasAtraso, detalleFijo);
        }

        var valorVencimiento = await ValorEnFechaAsync(fechaVencimiento.Value);
        var valorHoy = await ValorEnFechaAsync(hoy);
        if (valorVencimiento is null || valorHoy is null || valorVencimiento == 0)
            return new PunitorioResultado(0, diasAtraso, null); // sin tasa cargada, no se inventa un número

        var recargoTim = monto * (valorHoy.Value / valorVencimiento.Value - 1);
        var detalleTim = $"TIM BCRA: {valorHoy.Value:N4} ({hoy:dd/MM/yyyy}) / " +
                          $"{valorVencimiento.Value:N4} ({fechaVencimiento.Value:dd/MM/yyyy})";
        return new PunitorioResultado(Math.Max(recargoTim, 0), diasAtraso, detalleTim);
    }

    private async Task<decimal?> ValorEnFechaAsync(DateTime fecha) =>
        await _context.TasasMoratorias
            .Where(t => t.Fecha <= fecha)
            .OrderByDescending(t => t.Fecha)
            .Select(t => (decimal?)t.Valor)
            .FirstOrDefaultAsync();
}
