using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/contratos")]
[Authorize]
public class ContratosController : ControllerBase
{
    private readonly IContratoRepository _repo;
    private readonly IPagoRepository _pagos;

    public ContratosController(IContratoRepository repo, IPagoRepository pagos)
    {
        _repo = repo;
        _pagos = pagos;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? buscar,
        [FromQuery] TipoContrato? tipo,
        [FromQuery] EstadoContrato? estado,
        [FromQuery] int? propiedadId,
        [FromQuery] int? agenteId)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, buscar, tipo, estado, propiedadId, agenteId);
        var paginado = new PagedResult<ContratoDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<ContratoDto>>.Ok(paginado));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contrato = await _repo.GetByIdAsync(id);
        if (contrato is null)
            return NotFound(ApiResponse<ContratoDto>.Fail("Contrato no encontrado."));
        return Ok(ApiResponse<ContratoDto>.Ok(MapToDto(contrato)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Create([FromBody] CreateContratoRequest request)
    {
        var validacion = Validar(request);
        if (validacion is not null) return BadRequest(validacion);

        var contrato = MapFromRequest(request);
        var creado = await _repo.CreateAsync(contrato);
        var resultado = await _repo.GetByIdAsync(creado.Id);
        return Ok(ApiResponse<ContratoDto>.Ok(MapToDto(resultado!), "Contrato creado correctamente."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContratoRequest request)
    {
        var contrato = await _repo.GetByIdAsync(id);
        if (contrato is null)
            return NotFound(ApiResponse<ContratoDto>.Fail("Contrato no encontrado."));

        if (contrato.Estado != EstadoContrato.Borrador)
            return BadRequest(ApiResponse<ContratoDto>.Fail(
                $"Solo se pueden editar contratos en estado Borrador. Este contrato está en estado '{contrato.Estado}'."));

        var validacion = Validar(request);
        if (validacion is not null) return BadRequest(validacion);

        contrato.Tipo = (TipoContrato)request.Tipo;
        contrato.PropiedadId = request.PropiedadId;
        contrato.ReservaId = request.ReservaId;
        contrato.AgenteId = request.AgenteId;
        contrato.PropietarioRefId = request.PropietarioRefId;
        contrato.InquilinoRefId = request.InquilinoRefId;
        contrato.LocadorNombre = request.LocadorNombre;
        contrato.LocadorApellido = request.LocadorApellido;
        contrato.LocadorDni = request.LocadorDni;
        contrato.LocadorEmail = request.LocadorEmail;
        contrato.LocadorTelefono = request.LocadorTelefono;
        contrato.LocadorDomicilio = request.LocadorDomicilio;
        contrato.LocadorBanco = request.LocadorBanco;
        contrato.LocadorCbu = request.LocadorCbu;
        contrato.LocadorCuit = request.LocadorCuit;
        contrato.LocatarioNombre = request.LocatarioNombre;
        contrato.LocatarioApellido = request.LocatarioApellido;
        contrato.LocatarioDni = request.LocatarioDni;
        contrato.LocatarioEmail = request.LocatarioEmail;
        contrato.LocatarioTelefono = request.LocatarioTelefono;
        contrato.GaranteNombre = request.GaranteNombre;
        contrato.GaranteApellido = request.GaranteApellido;
        contrato.GaranteDni = request.GaranteDni;
        contrato.GaranteTelefono = request.GaranteTelefono;
        contrato.MontoBase = request.MontoBase;
        contrato.MontoActual = request.MontoBase;
        contrato.Moneda = (Moneda)request.Moneda;
        contrato.TipoAjuste = (TipoAjuste)request.TipoAjuste;
        contrato.PeriodicidadAjusteMeses = request.PeriodicidadAjusteMeses;
        contrato.PorcentajeAjuste = request.PorcentajeAjuste;
        contrato.DiaVencimientoPago = request.DiaVencimientoPago;
        contrato.ComisionLocadorPorcentaje = request.ComisionLocadorPorcentaje;
        contrato.ComisionLocadorMonto = request.ComisionLocadorMonto;
        contrato.ComisionLocatarioPorcentaje = request.ComisionLocatarioPorcentaje;
        contrato.ComisionLocatarioMonto = request.ComisionLocatarioMonto;
        contrato.AdministracionCobros = request.AdministracionCobros;
        contrato.FechaInicio = request.FechaInicio;
        contrato.FechaFin = request.FechaFin;
        contrato.FechaEscrituracion = request.FechaEscrituracion;
        contrato.Observaciones = request.Observaciones;

        var actualizado = await _repo.UpdateAsync(contrato);
        var resultado = await _repo.GetByIdAsync(actualizado.Id);
        return Ok(ApiResponse<ContratoDto>.Ok(MapToDto(resultado!), "Contrato actualizado correctamente."));
    }

    [HttpPut("{id}/estado")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> TransicionEstado(int id, [FromBody] TransicionEstadoRequest request)
    {
        var nuevoEstado = (EstadoContrato)request.Estado;

        if (nuevoEstado is EstadoContrato.Rescindido or EstadoContrato.Anulado
            && string.IsNullOrWhiteSpace(request.Motivo))
            return BadRequest(ApiResponse<ContratoDto>.Fail("El motivo es requerido para este cambio de estado."));

        var (ok, error, contrato) = await _repo.TransicionEstadoAsync(id, nuevoEstado, request.Motivo, request.Fecha);
        if (!ok) return BadRequest(ApiResponse<ContratoDto>.Fail(error!));

        var resultado = await _repo.GetByIdAsync(contrato!.Id);
        return Ok(ApiResponse<ContratoDto>.Ok(MapToDto(resultado!), $"Contrato pasado a estado '{nuevoEstado}' correctamente."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var contrato = await _repo.GetByIdAsync(id);
        if (contrato is null)
            return NotFound(ApiResponse<object>.Fail("Contrato no encontrado."));

        if (contrato.Estado != EstadoContrato.Borrador)
            return BadRequest(ApiResponse<object>.Fail(
                $"Solo se pueden eliminar contratos en estado Borrador. Para otros estados, usá la transición de estado correspondiente."));

        var eliminado = await _repo.DeleteAsync(id);
        if (!eliminado)
            return NotFound(ApiResponse<object>.Fail("Contrato no encontrado."));
        return Ok(ApiResponse<object>.Ok(null, "Contrato eliminado correctamente."));
    }

    [HttpGet("{id}/pagos")]
    public async Task<IActionResult> GetPagos(int id)
    {
        var pagos = await _pagos.GetByContratoAsync(id);
        return Ok(ApiResponse<IEnumerable<PagoDto>>.Ok(pagos.Select(MapPagoToDto)));
    }

    [HttpPut("{contratoId}/pagos/{pagoId}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> UpdatePago(int contratoId, int pagoId, [FromBody] UpdatePagoRequest request)
    {
        var pago = await _pagos.GetByIdAsync(pagoId);
        if (pago is null || pago.ContratoId != contratoId)
            return NotFound(ApiResponse<PagoDto>.Fail("Pago no encontrado."));

        pago.Estado = (EstadoPago)request.Estado;
        pago.FechaPago = request.FechaPago;
        pago.Observaciones = request.Observaciones;
        pago.MontoPagado = request.Detalles.Any() ? request.Detalles.Sum(d => d.Monto) : null;

        var detalles = request.Detalles.Select(d => new PagoDetalle
        {
            Medio = (MedioPago)d.Medio,
            Monto = d.Monto,
            Referencia = d.Referencia,
            ChequeBanco = d.ChequeBanco,
            ChequeNumero = d.ChequeNumero,
            ChequeFechaVencimiento = string.IsNullOrWhiteSpace(d.ChequeFechaVencimiento)
                ? null : DateTime.Parse(d.ChequeFechaVencimiento),
            Activo = true,
        });

        var actualizado = await _pagos.UpdateWithDetallesAsync(pago, detalles);
        return Ok(ApiResponse<PagoDto>.Ok(MapPagoToDto(actualizado), "Pago actualizado correctamente."));
    }

    private static ApiResponse<ContratoDto>? Validar(CreateContratoRequest r)
    {
        if (r.PropiedadId <= 0)
            return ApiResponse<ContratoDto>.Fail("La propiedad es requerida.");
        if (string.IsNullOrWhiteSpace(r.LocadorNombre) || string.IsNullOrWhiteSpace(r.LocadorApellido))
            return ApiResponse<ContratoDto>.Fail("Nombre y apellido del locador son requeridos.");
        if (string.IsNullOrWhiteSpace(r.LocatarioNombre) || string.IsNullOrWhiteSpace(r.LocatarioApellido))
            return ApiResponse<ContratoDto>.Fail("Nombre y apellido del locatario son requeridos.");
        if (r.MontoBase <= 0)
            return ApiResponse<ContratoDto>.Fail("El monto base debe ser mayor a cero.");
        if (r.FechaFin.HasValue && r.FechaFin <= r.FechaInicio)
            return ApiResponse<ContratoDto>.Fail("La fecha de fin debe ser posterior a la de inicio.");
        return null;
    }

    private static Contrato MapFromRequest(CreateContratoRequest r) => new()
    {
        Tipo = (TipoContrato)r.Tipo,
        Estado = (EstadoContrato)r.Estado,
        PropiedadId = r.PropiedadId,
        ReservaId = r.ReservaId,
        AgenteId = r.AgenteId,
        PropietarioRefId = r.PropietarioRefId,
        InquilinoRefId = r.InquilinoRefId,
        LocadorNombre = r.LocadorNombre,
        LocadorApellido = r.LocadorApellido,
        LocadorDni = r.LocadorDni,
        LocadorEmail = r.LocadorEmail,
        LocadorTelefono = r.LocadorTelefono,
        LocadorDomicilio = r.LocadorDomicilio,
        LocadorBanco = r.LocadorBanco,
        LocadorCbu = r.LocadorCbu,
        LocadorCuit = r.LocadorCuit,
        LocatarioNombre = r.LocatarioNombre,
        LocatarioApellido = r.LocatarioApellido,
        LocatarioDni = r.LocatarioDni,
        LocatarioEmail = r.LocatarioEmail,
        LocatarioTelefono = r.LocatarioTelefono,
        GaranteNombre = r.GaranteNombre,
        GaranteApellido = r.GaranteApellido,
        GaranteDni = r.GaranteDni,
        GaranteTelefono = r.GaranteTelefono,
        MontoBase = r.MontoBase,
        Moneda = (Moneda)r.Moneda,
        TipoAjuste = (TipoAjuste)r.TipoAjuste,
        PeriodicidadAjusteMeses = r.PeriodicidadAjusteMeses,
        DiaVencimientoPago = r.DiaVencimientoPago,
        ComisionLocadorPorcentaje = r.ComisionLocadorPorcentaje,
        ComisionLocadorMonto = r.ComisionLocadorMonto,
        ComisionLocatarioPorcentaje = r.ComisionLocatarioPorcentaje,
        ComisionLocatarioMonto = r.ComisionLocatarioMonto,
        AdministracionCobros = r.AdministracionCobros,
        PorcentajeAjuste = r.PorcentajeAjuste,
        FechaInicio = r.FechaInicio,
        FechaFin = r.FechaFin,
        FechaEscrituracion = r.FechaEscrituracion,
        Observaciones = r.Observaciones,
    };

    private static ContratoDto MapToDto(Contrato c) => new()
    {
        Id = c.Id,
        Codigo = c.Codigo,
        Tipo = c.Tipo.ToString(),
        Estado = c.Estado.ToString(),
        PropiedadId = c.PropiedadId,
        PropiedadDireccion = c.Propiedad.Direccion,
        PropiedadCodigo = c.Propiedad.Codigo,
        ReservaId = c.ReservaId,
        AgenteId = c.AgenteId,
        AgenteNombre = c.Agente is not null ? $"{c.Agente.User?.Nombre} {c.Agente.User?.Apellido}" : null,
        PropietarioRefId = c.PropietarioRefId,
        InquilinoRefId = c.InquilinoRefId,
        LocadorNombre = c.LocadorNombre,
        LocadorApellido = c.LocadorApellido,
        LocadorDni = c.LocadorDni,
        LocadorEmail = c.LocadorEmail,
        LocadorTelefono = c.LocadorTelefono,
        LocadorDomicilio = c.LocadorDomicilio,
        LocadorBanco = c.LocadorBanco,
        LocadorCbu = c.LocadorCbu,
        LocadorCuit = c.LocadorCuit,
        LocatarioNombre = c.LocatarioNombre,
        LocatarioApellido = c.LocatarioApellido,
        LocatarioDni = c.LocatarioDni,
        LocatarioEmail = c.LocatarioEmail,
        LocatarioTelefono = c.LocatarioTelefono,
        GaranteNombre = c.GaranteNombre,
        GaranteApellido = c.GaranteApellido,
        GaranteDni = c.GaranteDni,
        GaranteTelefono = c.GaranteTelefono,
        MontoBase = c.MontoBase,
        Moneda = c.Moneda.ToString(),
        TipoAjuste = c.TipoAjuste.ToString(),
        PeriodicidadAjusteMeses = c.PeriodicidadAjusteMeses,
        DiaVencimientoPago = c.DiaVencimientoPago,
        ComisionLocadorPorcentaje = c.ComisionLocadorPorcentaje,
        ComisionLocadorMonto = c.ComisionLocadorMonto,
        ComisionLocatarioPorcentaje = c.ComisionLocatarioPorcentaje,
        ComisionLocatarioMonto = c.ComisionLocatarioMonto,
        AdministracionCobros = c.AdministracionCobros,
        PorcentajeAjuste = c.PorcentajeAjuste,
        MontoActual = c.MontoActual,
        FechaUltimoAjuste = c.FechaUltimoAjuste,
        FechaInicio = c.FechaInicio,
        FechaFin = c.FechaFin,
        FechaEscrituracion = c.FechaEscrituracion,
        MotivoRescision = c.MotivoRescision,
        FechaRescision = c.FechaRescision,
        MotivoAnulacion = c.MotivoAnulacion,
        FechaAnulacion = c.FechaAnulacion,
        Observaciones = c.Observaciones,
        ArchivoUrl = c.ArchivoUrl,
        Pagos = c.Pagos.Select(MapPagoToDto).ToList(),
        Ajustes = c.Ajustes.Select(a => new AjusteContratoDto
        {
            Id = a.Id,
            ContratoId = a.ContratoId,
            FechaAplicacion = a.FechaAplicacion,
            MontoPrevio = a.MontoPrevio,
            MontoNuevo = a.MontoNuevo,
            Porcentaje = a.Porcentaje,
            TipoAjuste = a.TipoAjuste,
            Observaciones = a.Observaciones,
        }).ToList(),
        FechaCreacion = c.FechaCreacion,
        FechaActualizacion = c.FechaActualizacion,
    };

    private static PagoDto MapPagoToDto(Pago p) => new()
    {
        Id                 = p.Id,
        ContratoId         = p.ContratoId,
        NumeroCuota        = p.NumeroCuota,
        Periodo            = p.Periodo,
        MontoEsperado      = p.MontoEsperado,
        MontoPagado        = p.MontoPagado,
        FechaPago          = p.FechaPago,
        Estado             = p.Estado.ToString(),
        Observaciones      = p.Observaciones,
        Detalles           = p.Detalles.Where(d => d.Activo).Select(d => new PagoDetalleDto
        {
            Id                     = d.Id,
            Medio                  = d.Medio.ToString(),
            Monto                  = d.Monto,
            Referencia             = d.Referencia,
            ChequeBanco            = d.ChequeBanco,
            ChequeNumero           = d.ChequeNumero,
            ChequeFechaVencimiento = d.ChequeFechaVencimiento,
        }).ToList(),
        FechaCreacion      = p.FechaCreacion,
        FechaActualizacion = p.FechaActualizacion,
    };
}
