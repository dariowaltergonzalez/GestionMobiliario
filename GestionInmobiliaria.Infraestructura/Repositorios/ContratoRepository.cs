using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class ContratoRepository : IContratoRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;

    public ContratoRepository(ApplicationDbContext context, IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _context = context;
        _scopeFactory = scopeFactory;
        _config = config;
    }

    public async Task<PagedResult<Contrato>> GetPagedAsync(
        PaginationParams paginacion,
        string? buscar = null,
        TipoContrato? tipo = null,
        EstadoContrato? estado = null,
        int? propiedadId = null,
        int? agenteId = null)
    {
        var query = _context.Contratos
            .Include(c => c.Propiedad)
            .Include(c => c.Agente).ThenInclude(a => a!.User)
            .AsQueryable();

        if (tipo.HasValue)
            query = query.Where(c => c.Tipo == tipo.Value);

        if (estado.HasValue)
            query = query.Where(c => c.Estado == estado.Value);

        if (propiedadId.HasValue)
            query = query.Where(c => c.PropiedadId == propiedadId.Value);

        if (agenteId.HasValue)
            query = query.Where(c => c.AgenteId == agenteId.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(c =>
                c.Codigo.Contains(buscar) ||
                c.LocadorNombre.Contains(buscar) ||
                c.LocadorApellido.Contains(buscar) ||
                c.LocatarioNombre.Contains(buscar) ||
                c.LocatarioApellido.Contains(buscar) ||
                c.Propiedad.Direccion.Contains(buscar));

        query = query.OrderByDescending(c => c.FechaCreacion);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<Contrato?> GetByIdAsync(int id)
    {
        return await _context.Contratos
            .Include(c => c.Propiedad)
            .Include(c => c.Agente).ThenInclude(a => a!.User)
            .Include(c => c.Pagos.OrderBy(p => p.NumeroCuota))
                .ThenInclude(p => p.Detalles)
            .Include(c => c.Ajustes.OrderByDescending(a => a.FechaAplicacion))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Contrato> CreateAsync(Contrato contrato)
    {
        contrato.FechaCreacion = DateTime.UtcNow;
        contrato.FechaActualizacion = DateTime.UtcNow;
        contrato.MontoActual = contrato.MontoBase;
        _context.Contratos.Add(contrato);
        await _context.SaveChangesAsync();

        // Código post-save
        contrato.Codigo = $"CON-{contrato.FechaCreacion.Year}-{contrato.Id:D4}";

        // Sincronizar estado de la propiedad
        await SincronizarEstadoPropiedadAsync(contrato);

        // Generar pagos si corresponde
        if (contrato.AdministracionCobros && contrato.Estado == EstadoContrato.Vigente)
            await GenerarPagosAsync(contrato);

        // Comisión de cierre del agente (si se crea directo en Vigente con agente asignado)
        ComisionAgente? comisionGenerada = null;
        if (contrato.Estado == EstadoContrato.Vigente && contrato.AgenteId.HasValue)
            comisionGenerada = await GenerarComisionAgenteSiCorrespondeAsync(contrato);

        await _context.SaveChangesAsync();
        if (comisionGenerada is not null)
            NotificarComisionGenerada(comisionGenerada, contrato);
        return contrato;
    }

    public async Task<Contrato> UpdateAsync(Contrato contrato)
    {
        contrato.FechaActualizacion = DateTime.UtcNow;

        // Si pasa a Vigente y tiene administración de cobros y aún no tiene pagos, generarlos
        if (contrato.AdministracionCobros && contrato.Estado == EstadoContrato.Vigente)
        {
            var tienePagos = await _context.Pagos.AnyAsync(p => p.ContratoId == contrato.Id && p.Activo);
            if (!tienePagos)
                await GenerarPagosAsync(contrato);
        }

        // Comisión de cierre del agente (si se edita y pasa/queda en Vigente con agente asignado)
        ComisionAgente? comisionGenerada = null;
        if (contrato.Estado == EstadoContrato.Vigente && contrato.AgenteId.HasValue)
            comisionGenerada = await GenerarComisionAgenteSiCorrespondeAsync(contrato);

        await SincronizarEstadoPropiedadAsync(contrato);
        await _context.SaveChangesAsync();
        if (comisionGenerada is not null)
            NotificarComisionGenerada(comisionGenerada, contrato);
        return contrato;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var contrato = await _context.Contratos
            .Include(c => c.Pagos)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contrato is null) return false;

        contrato.Activo = false;
        contrato.FechaActualizacion = DateTime.UtcNow;

        foreach (var pago in contrato.Pagos)
        {
            pago.Activo = false;
            pago.FechaActualizacion = DateTime.UtcNow;
        }

        // Devolver la propiedad a Disponible si el contrato estaba vigente
        if (contrato.Estado == EstadoContrato.Vigente)
        {
            var propiedad = await _context.Propiedades.FindAsync(contrato.PropiedadId);
            if (propiedad is not null)
                propiedad.Estado = EstadoPropiedad.Disponible;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Ok, string? Error, Contrato? Contrato)> TransicionEstadoAsync(
        int id, EstadoContrato nuevoEstado, string? motivo, DateTime? fecha)
    {
        var contrato = await _context.Contratos.FindAsync(id);
        if (contrato is null) return (false, "Contrato no encontrado.", null);

        // Estados terminales: no admiten más transiciones
        if (contrato.Estado is EstadoContrato.Finalizado or EstadoContrato.Rescindido or EstadoContrato.Anulado)
            return (false, $"El contrato en estado '{contrato.Estado}' no puede cambiar de estado.", null);

        // Transiciones válidas
        var valida = (contrato.Estado, nuevoEstado) switch
        {
            (EstadoContrato.Borrador, EstadoContrato.Vigente)     => true,
            (EstadoContrato.Borrador, EstadoContrato.Anulado)     => true,
            (EstadoContrato.Vigente,  EstadoContrato.Finalizado)  => true,
            (EstadoContrato.Vigente,  EstadoContrato.Rescindido)  => true,
            (EstadoContrato.Vigente,  EstadoContrato.Anulado)     => true,
            _ => false
        };

        if (!valida)
            return (false, $"Transición de '{contrato.Estado}' a '{nuevoEstado}' no permitida.", null);

        if (nuevoEstado == EstadoContrato.Vigente)
        {
            var yaTieneVigente = await _context.Contratos.AnyAsync(c =>
                c.PropiedadId == contrato.PropiedadId && c.Estado == EstadoContrato.Vigente && c.Id != contrato.Id);
            if (yaTieneVigente)
                return (false, "Esta propiedad ya tiene un contrato Vigente. Finalizá o rescindí el anterior antes de activar uno nuevo.", null);
        }

        contrato.Estado = nuevoEstado;
        contrato.FechaActualizacion = DateTime.UtcNow;

        ComisionAgente? comisionGenerada = null;
        switch (nuevoEstado)
        {
            case EstadoContrato.Rescindido:
                contrato.MotivoRescision = motivo;
                contrato.FechaRescision = fecha ?? DateTime.UtcNow;
                break;
            case EstadoContrato.Anulado:
                contrato.MotivoAnulacion = motivo;
                contrato.FechaAnulacion = fecha ?? DateTime.UtcNow;
                break;
            case EstadoContrato.Vigente:
                if (contrato.AdministracionCobros)
                {
                    var tienePagos = await _context.Pagos.AnyAsync(p => p.ContratoId == contrato.Id && p.Activo);
                    if (!tienePagos)
                        await GenerarPagosAsync(contrato);
                }
                if (contrato.AgenteId.HasValue)
                    comisionGenerada = await GenerarComisionAgenteSiCorrespondeAsync(contrato);
                break;
        }

        await SincronizarEstadoPropiedadAsync(contrato);
        await _context.SaveChangesAsync();
        if (comisionGenerada is not null)
            NotificarComisionGenerada(comisionGenerada, contrato);
        return (true, null, contrato);
    }

    private async Task SincronizarEstadoPropiedadAsync(Contrato contrato)
    {
        var propiedad = await _context.Propiedades.FindAsync(contrato.PropiedadId);
        if (propiedad is null) return;

        propiedad.Estado = (contrato.Tipo, contrato.Estado) switch
        {
            (TipoContrato.Locacion, EstadoContrato.Vigente)              => EstadoPropiedad.Alquilada,
            (TipoContrato.Locacion, EstadoContrato.Finalizado)           => EstadoPropiedad.Disponible,
            (TipoContrato.Locacion, EstadoContrato.Rescindido)           => EstadoPropiedad.Disponible,
            (TipoContrato.Locacion, EstadoContrato.Anulado)              => EstadoPropiedad.Disponible,
            (TipoContrato.BoletoCompraventa, EstadoContrato.Vigente)     => EstadoPropiedad.BoletoFirmado,
            (TipoContrato.BoletoCompraventa, EstadoContrato.Finalizado)  => EstadoPropiedad.Vendida,
            (TipoContrato.BoletoCompraventa, EstadoContrato.Rescindido)  => EstadoPropiedad.Disponible,
            (TipoContrato.BoletoCompraventa, EstadoContrato.Anulado)     => EstadoPropiedad.Disponible,
            _ => propiedad.Estado
        };
    }

    private async Task GenerarPagosAsync(Contrato contrato)
    {
        if (contrato.FechaFin is null) return;

        var inicio = new DateTime(contrato.FechaInicio.Year, contrato.FechaInicio.Month, 1);
        var fin = new DateTime(contrato.FechaFin.Value.Year, contrato.FechaFin.Value.Month, 1);
        var numero = 1;
        var fecha = inicio;

        while (fecha <= fin)
        {
            _context.Pagos.Add(new Pago
            {
                ContratoId = contrato.Id,
                NumeroCuota = numero++,
                Periodo = fecha,
                MontoEsperado = contrato.MontoBase,
                Estado = EstadoPago.Pendiente,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow,
                TenantId = contrato.TenantId,
            });
            fecha = fecha.AddMonths(1);
        }
    }

    // Comisión de cierre del agente (una sola vez, al activar el contrato) — ver
    // docs/logica-negocio.md, sección COMISIONES DE AGENTES, para por qué se calcula así. Devuelve la
    // comisión recién creada (o null si no correspondía) para que el llamador pueda avisarle al
    // agente DESPUÉS de guardar — acá no se notifica directo porque esto corre antes del
    // SaveChangesAsync final del método que llama, y notificar antes de guardar sería prematuro.
    private async Task<ComisionAgente?> GenerarComisionAgenteSiCorrespondeAsync(Contrato contrato)
    {
        var yaExiste = await _context.ComisionesAgentes.AnyAsync(c => c.ContratoId == contrato.Id);
        if (yaExiste) return null;

        var agente = await _context.Agentes.FirstOrDefaultAsync(a => a.Id == contrato.AgenteId!.Value);
        if (agente is null || agente.ComisionPorcentaje <= 0) return null;

        decimal ComisionDe(decimal? monto, decimal? porcentaje) =>
            monto ?? (porcentaje.HasValue ? Math.Round(contrato.MontoBase * (porcentaje.Value / 100), 2) : 0);

        var montoComisionInmobiliaria =
            ComisionDe(contrato.ComisionLocadorMonto, contrato.ComisionLocadorPorcentaje) +
            ComisionDe(contrato.ComisionLocatarioMonto, contrato.ComisionLocatarioPorcentaje);
        if (montoComisionInmobiliaria <= 0) return null;

        var montoAgente = Math.Round(montoComisionInmobiliaria * (agente.ComisionPorcentaje / 100), 2);

        var comision = new ComisionAgente
        {
            ContratoId = contrato.Id,
            AgenteId = agente.Id,
            MontoComisionInmobiliaria = montoComisionInmobiliaria,
            PorcentajeAgente = agente.ComisionPorcentaje,
            MontoAgente = montoAgente,
            Estado = EstadoComisionAgente.Pendiente,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            FechaActualizacion = DateTime.UtcNow,
            TenantId = contrato.TenantId,
        };
        _context.ComisionesAgentes.Add(comision);
        return comision;
    }

    // Fire-and-forget con su propio scope — mismo patrón que usan los controllers para no bloquear la
    // respuesta HTTP en el envío de email/WhatsApp (ver PagosController, ContratosController, etc.).
    private void NotificarComisionGenerada(ComisionAgente comision, Contrato contrato)
    {
        var tenantId = contrato.TenantId;
        var contratoCodigo = contrato.Codigo;
        var agenteId = comision.AgenteId;
        var comisionId = comision.Id;
        var montoAgente = comision.MontoAgente;

        var plantillaSid = _config["WhatsApp:PlantillaAvisoVencimiento"];
        WhatsAppPlantilla? whatsApp = string.IsNullOrWhiteSpace(plantillaSid) ? null : new WhatsAppPlantilla
        {
            Sid = plantillaSid,
            Variables = new[] { contratoCodigo, $"comisión $ {montoAgente:N0}" },
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

                var asunto = $"Nueva comisión generada — {contratoCodigo}";
                var cuerpo = $"""
                    <p>Se generó tu comisión por el cierre del contrato <strong>{contratoCodigo}</strong>.</p>
                    <p style="font-size:18px;font-weight:bold;">$ {montoAgente:N2}</p>
                    """;
                var contexto = new NotificacionContexto
                {
                    TenantId = tenantId,
                    EntidadRelacionada = "EmailComisionAgente",
                    EntidadRelacionadaId = comisionId.ToString(),
                    DatosAdicionales = new { contrato = contratoCodigo, montoAgente },
                };

                await notificacion.NotificarAsync(agente, "ComisionGenerada", asunto, cuerpo, contexto, whatsApp: whatsApp);
            }
            catch { /* best-effort — ver catch equivalente en NotificacionService */ }
        });
    }
}

public class PagoRepository : IPagoRepository
{
    private readonly ApplicationDbContext _context;

    public PagoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Pago>> GetByContratoAsync(int contratoId)
    {
        return await _context.Pagos
            .Include(p => p.Detalles)
            .Include(p => p.Contrato)
            .Where(p => p.ContratoId == contratoId)
            .OrderBy(p => p.NumeroCuota)
            .ToListAsync();
    }

    public async Task<Pago?> GetByIdAsync(int id)
    {
        return await _context.Pagos
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Pago?> GetByIdConContratoAsync(int id)
    {
        return await _context.Pagos
            .Include(p => p.Detalles)
            .Include(p => p.Contrato)
            .ThenInclude(c => c.Propiedad)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PagedResult<Pago>> GetPagedAsync(
        PaginationParams paginacion,
        int? contratoId = null,
        EstadoPago? estado = null,
        int? mes = null,
        int? anio = null,
        string? buscar = null,
        EstadoContrato? estadoContrato = null)
    {
        var query = _context.Pagos
            .Include(p => p.Detalles)
            .Include(p => p.Contrato)
            .ThenInclude(c => c.Propiedad)
            .AsQueryable();

        if (contratoId.HasValue)
            query = query.Where(p => p.ContratoId == contratoId.Value);

        if (estado.HasValue)
            query = query.Where(p => p.Estado == estado.Value);

        if (estadoContrato.HasValue)
            query = query.Where(p => p.Contrato.Estado == estadoContrato.Value);

        if (mes.HasValue)
            query = query.Where(p => p.Periodo.Month == mes.Value);

        if (anio.HasValue)
            query = query.Where(p => p.Periodo.Year == anio.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(p =>
                p.Contrato.Codigo.Contains(buscar) ||
                p.Contrato.LocatarioNombre.Contains(buscar) ||
                p.Contrato.LocatarioApellido.Contains(buscar));

        query = query.OrderBy(p => p.Periodo).ThenBy(p => p.ContratoId).ThenBy(p => p.NumeroCuota);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<Pago> UpdateAsync(Pago pago)
    {
        pago.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return pago;
    }

    public async Task<Pago> UpdateWithDetallesAsync(Pago pago, IEnumerable<PagoDetalle> nuevosDetalles)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            pago.FechaActualizacion = DateTime.UtcNow;

            var anteriores = await _context.PagoDetalles
                .Where(d => d.PagoId == pago.Id)
                .ToListAsync();
            foreach (var d in anteriores)
            {
                d.Activo = false;
                d.FechaActualizacion = DateTime.UtcNow;
            }

            var lista = nuevosDetalles.ToList();
            foreach (var detalle in lista)
            {
                detalle.PagoId = pago.Id;
                detalle.FechaCreacion = DateTime.UtcNow;
                detalle.FechaActualizacion = DateTime.UtcNow;
                detalle.TenantId = pago.TenantId;
                _context.PagoDetalles.Add(detalle);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return pago;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
