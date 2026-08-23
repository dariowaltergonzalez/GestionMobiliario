using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Aplicacion.Services;

public record PunitorioResultado(decimal Monto, int DiasAtraso, string? TasaUsada);

public interface IPunitorioService
{
    /// <summary>
    /// Calcula el punitorio por mora de una cuota, en vivo, a la fecha de hoy. Devuelve Monto=0 si el
    /// contrato no tiene DiaVencimientoPago cargado, si la cuota no está vencida, o si no hay tasa
    /// TIM cargada para calcular (fallback seguro: nunca inventa un número). Requiere que
    /// <paramref name="pago"/> tenga cargado su Contrato (Include).
    /// </summary>
    Task<PunitorioResultado> CalcularAsync(Pago pago);
}
