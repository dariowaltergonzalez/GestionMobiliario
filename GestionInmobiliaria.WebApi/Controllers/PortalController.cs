using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokenPortalHelper = GestionInmobiliaria.Dominio.Common.TokenPortal;

namespace GestionInmobiliaria.WebApi.Controllers;

/// <summary>
/// Portal de autoservicio para Inquilino/Propietario, sin login — acceso por token largo y
/// aleatorio en la URL (ver docs/logica-negocio.md, sección PORTAL DE AUTOSERVICIO). A propósito no
/// tiene [Authorize]: no hay JWT ni sesión, el token en sí ES la credencial. Corre siempre con
/// IgnoreQueryFilters() + filtro manual por TenantId (parseado del propio token, sin buscar en todos
/// los tenants) — mismo patrón que ya se usa en los background services.
/// </summary>
[ApiController]
[Route("api/portal")]
public class PortalController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPunitorioService _punitorio;

    public PortalController(ApplicationDbContext context, IPunitorioService punitorio)
    {
        _context = context;
        _punitorio = punitorio;
    }

    [HttpGet("inquilino/{token}")]
    public async Task<IActionResult> GetInquilino(string token)
    {
        var tenantId = TokenPortalHelper.ParsearTenantId(token);
        if (tenantId is null)
            return NotFound(ApiResponse<PortalInquilinoDto>.Fail("Link inválido."));

        var inquilino = await _context.Inquilinos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.TokenPortal == token && i.Activo);
        if (inquilino is null)
            return NotFound(ApiResponse<PortalInquilinoDto>.Fail("Link inválido."));

        var empresa = await _context.ConfiguracionEmpresa.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        var dto = new PortalInquilinoDto
        {
            NombreEmpresa = empresa?.NombreComercial ?? "",
            LogoUrl = empresa?.LogoUrl,
            InquilinoNombre = inquilino.Nombre,
            InquilinoApellido = inquilino.Apellido,
        };

        var contrato = await _context.Contratos.IgnoreQueryFilters()
            .Include(c => c.Propiedad)
            .Where(c => c.TenantId == tenantId && c.Activo &&
                        c.InquilinoRefId == inquilino.Id && c.Estado == EstadoContrato.Vigente)
            .OrderByDescending(c => c.FechaInicio)
            .FirstOrDefaultAsync();

        if (contrato is not null)
        {
            dto.Contrato = new PortalContratoDto
            {
                Codigo = contrato.Codigo,
                PropiedadDireccion = contrato.Propiedad.Direccion,
                MontoActual = contrato.MontoActual,
                Moneda = contrato.Moneda.ToString(),
                FechaInicio = contrato.FechaInicio,
                FechaFin = contrato.FechaFin,
            };

            var pagos = await _context.Pagos.IgnoreQueryFilters()
                .Where(p => p.TenantId == tenantId && p.Activo && p.ContratoId == contrato.Id)
                .OrderByDescending(p => p.Periodo)
                .ToListAsync();

            foreach (var pago in pagos)
            {
                pago.Contrato = contrato; // evita un query por cuota — IPunitorioService lo necesita cargado
                var punitorio = await _punitorio.CalcularAsync(pago);
                dto.Pagos.Add(new PortalPagoDto
                {
                    NumeroCuota = pago.NumeroCuota,
                    Periodo = pago.Periodo,
                    MontoEsperado = pago.MontoEsperado,
                    MontoPagado = pago.MontoPagado,
                    Estado = pago.Estado.ToString(),
                    FechaPago = pago.FechaPago,
                    MontoPunitorio = punitorio.Monto,
                    DiasAtraso = punitorio.DiasAtraso,
                });
            }

            // Solo gastos atados a ESTE contrato — evita mostrarle al inquilino actual algo que haya
            // quedado cargado sin ContratoId de una relación anterior con la misma propiedad.
            var gastos = await _context.Gastos.IgnoreQueryFilters()
                .Where(g => g.TenantId == tenantId && g.Activo && g.ContratoId == contrato.Id &&
                            g.Responsable == ResponsableGasto.Inquilino && g.VisibleParaInquilino)
                .OrderByDescending(g => g.Fecha)
                .ToListAsync();

            dto.Gastos = gastos.Select(g => new PortalGastoDto
            {
                Categoria = g.Categoria.ToString(),
                Descripcion = g.Descripcion,
                Monto = g.Monto,
                Fecha = g.Fecha,
                Estado = g.Estado.ToString(),
            }).ToList();
        }

        return Ok(ApiResponse<PortalInquilinoDto>.Ok(dto));
    }

    [HttpGet("propietario/{token}")]
    public async Task<IActionResult> GetPropietario(string token)
    {
        var tenantId = TokenPortalHelper.ParsearTenantId(token);
        if (tenantId is null)
            return NotFound(ApiResponse<PortalPropietarioDto>.Fail("Link inválido."));

        var propietario = await _context.Propietarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.TokenPortal == token && p.Activo);
        if (propietario is null)
            return NotFound(ApiResponse<PortalPropietarioDto>.Fail("Link inválido."));

        var empresa = await _context.ConfiguracionEmpresa.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        var liquidaciones = await _context.Liquidaciones.IgnoreQueryFilters()
            .Include(l => l.Pago).ThenInclude(p => p.Contrato).ThenInclude(c => c.Propiedad)
            .Include(l => l.Abonos.Where(a => a.Activo))
            .Include(l => l.Gastos)
            .Where(l => l.TenantId == tenantId && l.Activo && l.Pago.Contrato.PropietarioRefId == propietario.Id)
            .OrderByDescending(l => l.Pago.Periodo)
            .ToListAsync();

        var dto = new PortalPropietarioDto
        {
            NombreEmpresa = empresa?.NombreComercial ?? "",
            LogoUrl = empresa?.LogoUrl,
            PropietarioNombre = propietario.Nombre,
            PropietarioApellido = propietario.Apellido,
            Liquidaciones = liquidaciones.Select(l => new PortalLiquidacionDto
            {
                PropiedadDireccion = l.Pago.Contrato.Propiedad.Direccion,
                ContratoCodigo = l.Pago.Contrato.Codigo,
                Periodo = l.Pago.Periodo,
                MontoCobrado = l.MontoCobrado,
                MontoComision = l.MontoComision,
                MontoGastos = l.MontoGastos,
                MontoALiquidar = l.MontoALiquidar,
                MontoAbonado = l.Abonos.Sum(a => a.Monto),
                Estado = l.Estado.ToString(),
                FechaLiquidacion = l.FechaLiquidacion,
                Abonos = l.Abonos.Select(a => new PortalAbonoDto
                {
                    Monto = a.Monto,
                    Fecha = a.Fecha,
                    Medio = a.Medio.ToString(),
                    CbuCvuDestino = a.CbuCvuDestino,
                    EntidadDestino = a.EntidadDestino,
                    NumeroOperacion = a.NumeroOperacion,
                    ComprobanteUrl = a.ComprobanteUrl,
                }).OrderByDescending(a => a.Fecha).ToList(),
                Gastos = l.Gastos.Select(g => new PortalGastoDto
                {
                    Categoria = g.Categoria.ToString(),
                    Descripcion = g.Descripcion,
                    Monto = g.Monto,
                    Fecha = g.Fecha,
                    Estado = g.Estado.ToString(),
                }).ToList(),
            }).ToList(),
        };

        return Ok(ApiResponse<PortalPropietarioDto>.Ok(dto));
    }
}
