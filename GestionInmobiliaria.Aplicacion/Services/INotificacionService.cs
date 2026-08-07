using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Aplicacion.Services;

public class NotificacionContexto
{
    public required int TenantId { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    /// <summary>EntityName con el que queda el registro en AuditLogs, ej: "EmailRecibo".</summary>
    public required string EntidadRelacionada { get; init; }
    /// <summary>EntityId con el que queda el registro en AuditLogs, ej: el pagoId.</summary>
    public required string EntidadRelacionadaId { get; init; }
    /// <summary>Datos extra del evento (ej: contrato, período) que se guardan en el detalle del log.</summary>
    public object? DatosAdicionales { get; init; }
}

public interface INotificacionService
{
    Task<bool> NotificarAsync(INotificable destinatario, string tema, string asunto, string cuerpo,
        NotificacionContexto contexto, IReadOnlyList<EmailAdjunto>? adjuntos = null);
}
