namespace GestionInmobiliaria.Dominio.Common;

/// <summary>
/// Calcula la fecha de vencimiento real de una cuota a partir del período y el día de vencimiento
/// pactado en el contrato, clampeado al último día del mes si ese mes no tiene ese día (ej: 31 en
/// febrero). Antes vivía duplicado inline en RecordatorioVencimientoService; ahora también lo usa
/// el cálculo de Punitorios (ver docs/logica-negocio.md, sección PUNITORIOS).
/// </summary>
public static class VencimientoCalculator
{
    public static DateTime? Calcular(DateTime periodo, int? diaVencimientoPago)
    {
        if (diaVencimientoPago is null) return null;

        var diasEnMes = DateTime.DaysInMonth(periodo.Year, periodo.Month);
        var dia = Math.Min(diaVencimientoPago.Value, diasEnMes);
        return new DateTime(periodo.Year, periodo.Month, dia, 0, 0, 0, DateTimeKind.Utc);
    }
}
