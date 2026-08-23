using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/gastos")]
[Authorize]
public class GastosController : ControllerBase
{
    private readonly IGastoRepository _repo;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GastosController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public GastosController(
        IGastoRepository repo,
        ApplicationDbContext context,
        ILogger<GastosController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _repo = repo;
        _context = context;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] int? propiedadId,
        [FromQuery] int? contratoId,
        [FromQuery] ResponsableGasto? responsable,
        [FromQuery] EstadoGasto? estado,
        [FromQuery] CategoriaGasto? categoria,
        [FromQuery] string? buscar)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, propiedadId, contratoId, responsable, estado, categoria, buscar);
        var paginado = new PagedResult<GastoDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<GastoDto>>.Ok(paginado));
    }

    [HttpGet("categorias")]
    public IActionResult GetCategorias()
    {
        var categorias = Enum.GetValues<CategoriaGasto>()
            .Select(c => new CategoriaGastoDto { Valor = (int)c, Nombre = c.ToString() });
        return Ok(ApiResponse<IEnumerable<CategoriaGastoDto>>.Ok(categorias));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var gasto = await _repo.GetByIdAsync(id);
        if (gasto is null) return NotFound(ApiResponse<GastoDto>.Fail("Gasto no encontrado."));
        return Ok(ApiResponse<GastoDto>.Ok(MapToDto(gasto)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Create([FromBody] CreateGastoRequest request)
    {
        var gasto = new Gasto
        {
            PropiedadId = request.PropiedadId,
            ContratoId = request.ContratoId,
            Categoria = (CategoriaGasto)request.Categoria,
            Descripcion = request.Descripcion,
            Monto = request.Monto,
            Fecha = request.Fecha,
            Responsable = (ResponsableGasto)request.Responsable,
            VisibleParaInquilino = request.VisibleParaInquilino,
        };

        var creado = await _repo.CreateAsync(gasto);
        var completo = await _repo.GetByIdAsync(creado.Id);

        if (completo!.Responsable == ResponsableGasto.Inquilino && completo.ContratoId.HasValue)
            await NotificarGastoPendienteAsync(completo);

        return Ok(ApiResponse<GastoDto>.Ok(MapToDto(completo), "Gasto creado correctamente."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGastoRequest request)
    {
        var datos = new Gasto
        {
            PropiedadId = request.PropiedadId,
            ContratoId = request.ContratoId,
            Categoria = (CategoriaGasto)request.Categoria,
            Descripcion = request.Descripcion,
            Monto = request.Monto,
            Fecha = request.Fecha,
            Responsable = (ResponsableGasto)request.Responsable,
            VisibleParaInquilino = request.VisibleParaInquilino,
        };

        var actualizado = await _repo.UpdateAsync(id, datos);
        if (actualizado is null)
            return BadRequest(ApiResponse<GastoDto>.Fail(
                "No se puede editar: el gasto no existe o ya fue resuelto/liquidado."));

        var completo = await _repo.GetByIdAsync(id);
        return Ok(ApiResponse<GastoDto>.Ok(MapToDto(completo!), "Gasto actualizado correctamente."));
    }

    [HttpPut("{id}/resolver")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> MarcarResuelto(int id, [FromBody] ResolverGastoRequest request)
    {
        var gasto = await _repo.MarcarResueltoAsync(
            id, (MedioPago)request.Medio, request.Fecha, request.ReferenciaCobro,
            request.ChequeBanco, request.ChequeNumero, request.ChequeFechaVencimiento, request.Observaciones);
        if (gasto is null)
            return BadRequest(ApiResponse<GastoDto>.Fail(
                "No se puede marcar como resuelto: el gasto no existe o ya fue resuelto."));

        var completo = await _repo.GetByIdAsync(id);
        return Ok(ApiResponse<GastoDto>.Ok(MapToDto(completo!), "Gasto marcado como resuelto."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _repo.DeleteAsync(id);
        if (!eliminado)
            return BadRequest(ApiResponse<string>.Fail(
                "No se puede eliminar: el gasto no existe o ya fue resuelto/liquidado."));

        return Ok(ApiResponse<string>.Ok("Gasto eliminado."));
    }

    private async Task NotificarGastoPendienteAsync(Gasto gasto)
    {
        var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst("nombre") is { } n && User.FindFirst("apellido") is { } a
            ? $"{n.Value} {a.Value}"
            : User.Identity?.Name;

        var contrato = await _context.Contratos
            .FirstOrDefaultAsync(c => c.Id == gasto.ContratoId!.Value);
        if (contrato?.InquilinoRefId is not { } inquilinoRefId) return;

        var tenantId = gasto.TenantId;
        var gastoId = gasto.Id;
        var categoria = gasto.Categoria.ToString();
        var descripcion = gasto.Descripcion;
        var monto = gasto.Monto;
        var codigoContrato = contrato.Codigo;
        var propiedadDireccion = (await _context.Propiedades.FirstOrDefaultAsync(p => p.Id == gasto.PropiedadId))?.Direccion ?? "";

        _ = Task.Run(async () =>
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ctx          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificacion = scope.ServiceProvider.GetRequiredService<INotificacionService>();

            try
            {
                var inquilino = await ctx.Inquilinos.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(i => i.Id == inquilinoRefId && i.TenantId == tenantId);
                if (inquilino is null) return;

                var asunto = $"Gasto a cargo tuyo — {propiedadDireccion} — {codigoContrato}";
                var cuerpo = BuildAvisoGastoEmailBody(inquilino.Nombre, propiedadDireccion, codigoContrato, categoria, descripcion, monto);
                var contexto = new NotificacionContexto
                {
                    TenantId = tenantId,
                    UserId = userId,
                    UserName = userName,
                    EntidadRelacionada = "EmailAvisoGastoPendiente",
                    EntidadRelacionadaId = gastoId.ToString(),
                    DatosAdicionales = new { contrato = codigoContrato, categoria, monto },
                };

                await notificacion.NotificarAsync(inquilino, "AvisoGastoPendiente", asunto, cuerpo, contexto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar el aviso de gasto pendiente. GastoId={GastoId}", gastoId);
            }
        });
    }

    private static string BuildAvisoGastoEmailBody(
        string nombreInquilino, string propiedadDireccion, string codigoContrato,
        string categoria, string? descripcion, decimal monto)
    {
        var montoTexto = monto.ToString("N2", new System.Globalization.CultureInfo("es-AR"));
        return $"""
            <!DOCTYPE html><html><head><meta charset="utf-8"></head>
            <body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;padding:0;">
              <div style="background:#1e3a5f;padding:20px 24px;border-radius:8px 8px 0 0;">
                <h1 style="color:white;margin:0;font-size:17px;">Aviso de gasto</h1>
              </div>
              <div style="padding:24px;border:1px solid #eee;border-top:none;border-radius:0 0 8px 8px;">
                <p>Hola {nombreInquilino},</p>
                <p>Te informamos que quedó registrado un gasto a tu cargo sobre la propiedad
                   <strong>{propiedadDireccion}</strong> (contrato {codigoContrato}):</p>
                <table style="width:100%;border-collapse:collapse;margin:16px 0;">
                  <tr><td style="padding:6px 0;color:#666;">Categoría</td><td style="padding:6px 0;text-align:right;">{categoria}</td></tr>
                  {(string.IsNullOrWhiteSpace(descripcion) ? "" : $"""<tr><td style="padding:6px 0;color:#666;">Detalle</td><td style="padding:6px 0;text-align:right;">{descripcion}</td></tr>""")}
                  <tr><td style="padding:6px 0;color:#666;font-weight:bold;">Monto</td><td style="padding:6px 0;text-align:right;font-weight:bold;">$ {montoTexto}</td></tr>
                </table>
                <p>Por favor coordiná con la inmobiliaria la forma de pago de este importe.</p>
              </div>
            </body></html>
            """;
    }

    private static GastoDto MapToDto(Gasto g) => new()
    {
        Id = g.Id,
        PropiedadId = g.PropiedadId,
        PropiedadDireccion = g.Propiedad?.Direccion ?? string.Empty,
        ContratoId = g.ContratoId,
        ContratoCodigo = g.Contrato?.Codigo,
        Categoria = g.Categoria.ToString(),
        Descripcion = g.Descripcion,
        Monto = g.Monto,
        Fecha = g.Fecha,
        Responsable = g.Responsable.ToString(),
        Estado = g.Estado.ToString(),
        FechaResolucion = g.FechaResolucion,
        MedioCobro = g.MedioCobro?.ToString(),
        FechaCobro = g.FechaCobro,
        ReferenciaCobro = g.ReferenciaCobro,
        ChequeBanco = g.ChequeBanco,
        ChequeNumero = g.ChequeNumero,
        ChequeFechaVencimiento = g.ChequeFechaVencimiento,
        ObservacionesResolucion = g.ObservacionesResolucion,
        LiquidacionId = g.LiquidacionId,
        VisibleParaInquilino = g.VisibleParaInquilino,
        FechaCreacion = g.FechaCreacion,
    };
}
