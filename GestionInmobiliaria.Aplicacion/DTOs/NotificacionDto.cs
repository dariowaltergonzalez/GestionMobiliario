namespace GestionInmobiliaria.Aplicacion.DTOs;

public class TemaNotificacionDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public static class TemasNotificacion
{
    public static readonly IReadOnlyList<TemaNotificacionDto> Propietario = new List<TemaNotificacionDto>
    {
        new() { Codigo = "NuevoContrato", Label = "Aviso de nuevo contrato" },
        new() { Codigo = "AvisoAumento", Label = "Aviso de aumento aplicado al contrato" },
        new() { Codigo = "AvisoCobro", Label = "Aviso de cobro registrado (recibo)" },
        new() { Codigo = "CambioEstadoContrato", Label = "Aviso de finalización, rescisión o anulación del contrato" },
    };

    public static readonly IReadOnlyList<TemaNotificacionDto> Inquilino = new List<TemaNotificacionDto>
    {
        new() { Codigo = "NuevoContrato", Label = "Aviso de nuevo contrato" },
        new() { Codigo = "AvisoAumento", Label = "Aviso de aumento de cuota" },
        new() { Codigo = "ReciboPago", Label = "Recibo de pago de cuota" },
        new() { Codigo = "CambioEstadoContrato", Label = "Aviso de finalización, rescisión o anulación del contrato" },
    };
}
