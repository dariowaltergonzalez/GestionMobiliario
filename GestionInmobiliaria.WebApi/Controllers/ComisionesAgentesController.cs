using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionInmobiliaria.WebApi.Controllers;

/// <summary>
/// Comisión de cierre del agente por contrato — distinta de Liquidaciones (que es la comisión de
/// administración de cobros al propietario, recurrente). Ver docs/logica-negocio.md, sección
/// COMISIONES DE AGENTES.
/// </summary>
[ApiController]
[Route("api/comisiones-agentes")]
[Authorize]
public class ComisionesAgentesController : ControllerBase
{
    private readonly IComisionAgenteRepository _repo;
    private readonly ApplicationDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;

    public ComisionesAgentesController(
        IComisionAgenteRepository repo, ApplicationDbContext context, IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _repo = repo;
        _context = context;
        _scopeFactory = scopeFactory;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] EstadoComisionAgente? estado,
        [FromQuery] int? agenteId,
        [FromQuery] string? buscar)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, estado, agenteId, buscar);
        var paginado = new PagedResult<ComisionAgenteDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas,
        };
        return Ok(ApiResponse<PagedResult<ComisionAgenteDto>>.Ok(paginado));
    }

    [HttpGet("metricas")]
    public async Task<IActionResult> GetMetricas()
    {
        var ahora = DateTime.UtcNow;

        var pendientes = await _context.ComisionesAgentes.CountAsync(c => c.Estado == EstadoComisionAgente.Pendiente);
        var montoPendiente = await _context.ComisionesAgentes
            .Where(c => c.Estado == EstadoComisionAgente.Pendiente)
            .SumAsync(c => (decimal?)c.MontoAgente) ?? 0;
        var pagadasMes = await _context.ComisionesAgentes.CountAsync(c =>
            c.Estado == EstadoComisionAgente.Pagado && c.FechaPago.HasValue &&
            c.FechaPago.Value.Month == ahora.Month && c.FechaPago.Value.Year == ahora.Year);
        var montoPagadoMes = await _context.ComisionesAgentes
            .Where(c => c.Estado == EstadoComisionAgente.Pagado && c.FechaPago.HasValue &&
                        c.FechaPago.Value.Month == ahora.Month && c.FechaPago.Value.Year == ahora.Year)
            .SumAsync(c => (decimal?)c.MontoAgente) ?? 0;

        return Ok(ApiResponse<ComisionAgenteMetricasDto>.Ok(new ComisionAgenteMetricasDto
        {
            PendientesCount = pendientes,
            MontoTotalPendiente = montoPendiente,
            PagadasMesCount = pagadasMes,
            MontoPagadoMes = montoPagadoMes,
        }));
    }

    [HttpGet("top-mes")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> GetTopMes([FromQuery] int top = 3)
    {
        var ahora = DateTime.UtcNow;

        var ranking = await _context.ComisionesAgentes
            .Where(c => c.Estado == EstadoComisionAgente.Pagado && c.FechaPago.HasValue &&
                        c.FechaPago.Value.Month == ahora.Month && c.FechaPago.Value.Year == ahora.Year)
            .GroupBy(c => new { c.AgenteId, c.Agente.User.Nombre, c.Agente.User.Apellido })
            .Select(g => new TopAgenteDto
            {
                AgenteId = g.Key.AgenteId,
                AgenteNombre = g.Key.Apellido + ", " + g.Key.Nombre,
                MontoPagadoMes = g.Sum(c => c.MontoAgente),
                CantidadPagos = g.Count(),
            })
            .OrderByDescending(d => d.MontoPagadoMes)
            .Take(top)
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<TopAgenteDto>>.Ok(ranking));
    }

    [HttpPost("{id}/pago")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> RegistrarPago(int id, [FromBody] RegistrarPagoComisionRequest request)
    {
        var actualizada = await _repo.RegistrarPagoAsync(id, request.Fecha ?? DateTime.UtcNow, (MedioPago)request.Medio, request.Observaciones?.Trim());
        if (actualizada is null) return NotFound(ApiResponse<ComisionAgenteDto>.Fail("Comisión no encontrada."));
        NotificarComisionPagada(actualizada);
        return Ok(ApiResponse<ComisionAgenteDto>.Ok(MapToDto(actualizada), "Pago registrado correctamente."));
    }

    [HttpPut("{id}/pago")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> ActualizarPago(int id, [FromBody] RegistrarPagoComisionRequest request)
    {
        var actualizada = await _repo.ActualizarPagoAsync(id, request.Fecha ?? DateTime.UtcNow, (MedioPago)request.Medio, request.Observaciones?.Trim());
        if (actualizada is null) return NotFound(ApiResponse<ComisionAgenteDto>.Fail("Comisión no encontrada o no está pagada."));
        return Ok(ApiResponse<ComisionAgenteDto>.Ok(MapToDto(actualizada), "Pago actualizado correctamente."));
    }

    [HttpDelete("{id}/pago")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> DeshacerPago(int id)
    {
        var actualizada = await _repo.DeshacerPagoAsync(id);
        if (actualizada is null) return NotFound(ApiResponse<ComisionAgenteDto>.Fail("Comisión no encontrada."));
        return Ok(ApiResponse<ComisionAgenteDto>.Ok(MapToDto(actualizada), "Se deshizo el pago, la comisión vuelve a estar Pendiente."));
    }

    // Fire-and-forget con su propio scope — mismo patrón que el resto de los avisos de la app.
    private void NotificarComisionPagada(ComisionAgente comision)
    {
        var tenantId = comision.TenantId;
        var contratoCodigo = comision.Contrato.Codigo;
        var agenteId = comision.AgenteId;
        var comisionId = comision.Id;
        var montoAgente = comision.MontoAgente;

        var plantillaSid = _config["WhatsApp:PlantillaAvisoVencimiento"];
        WhatsAppPlantilla? whatsApp = string.IsNullOrWhiteSpace(plantillaSid) ? null : new WhatsAppPlantilla
        {
            Sid = plantillaSid,
            Variables = new[] { contratoCodigo, $"pago $ {montoAgente:N0}" },
        };

        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificacion = scope.ServiceProvider.GetRequiredService<INotificacionService>();

            try
            {
                var agente = await ctx.Agentes.IgnoreQueryFilters()
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == agenteId && a.TenantId == tenantId);
                if (agente is null) return;

                var asunto = $"Pago de comisión — {contratoCodigo}";
                var cuerpo = $"""
                    <p>Te pagamos tu comisión del contrato <strong>{contratoCodigo}</strong>.</p>
                    <p style="font-size:18px;font-weight:bold;">$ {montoAgente:N2}</p>
                    """;
                var contexto = new NotificacionContexto
                {
                    TenantId = tenantId,
                    EntidadRelacionada = "EmailComisionAgente",
                    EntidadRelacionadaId = comisionId.ToString(),
                    DatosAdicionales = new { contrato = contratoCodigo, montoAgente },
                };

                await notificacion.NotificarAsync(agente, "ComisionPagada", asunto, cuerpo, contexto, whatsApp: whatsApp);
            }
            catch { /* best-effort */ }
        });
    }

    private static ComisionAgenteDto MapToDto(ComisionAgente c) => new()
    {
        Id = c.Id,
        ContratoId = c.ContratoId,
        ContratoCodigo = c.Contrato.Codigo,
        PropiedadDireccion = c.Contrato.Propiedad.Direccion,
        AgenteId = c.AgenteId,
        AgenteNombre = $"{c.Agente.User.Nombre} {c.Agente.User.Apellido}",
        MontoComisionInmobiliaria = c.MontoComisionInmobiliaria,
        PorcentajeAgente = c.PorcentajeAgente,
        MontoAgente = c.MontoAgente,
        Estado = c.Estado.ToString(),
        FechaPago = c.FechaPago,
        Medio = c.Medio?.ToString(),
        Observaciones = c.Observaciones,
        FechaCreacion = c.FechaCreacion,
    };
}
