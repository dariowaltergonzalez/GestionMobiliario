using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/reservas")]
[Authorize]
public class ReservasController : ControllerBase
{
    private readonly IReservaRepository _repo;
    private readonly IPropiedadRepository _propiedades;

    public ReservasController(IReservaRepository repo, IPropiedadRepository propiedades)
    {
        _repo = repo;
        _propiedades = propiedades;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationParams paginacion,
        [FromQuery] string? buscar,
        [FromQuery] EstadoReserva? estado,
        [FromQuery] int? propiedadId)
    {
        var resultado = await _repo.GetPagedAsync(paginacion, buscar, estado, propiedadId);
        var paginado = new PagedResult<ReservaDto>
        {
            Items = resultado.Items.Select(MapToDto).ToList(),
            Pagina = resultado.Pagina,
            Tamano = resultado.Tamano,
            TotalRegistros = resultado.TotalRegistros,
            TotalPaginas = resultado.TotalPaginas
        };
        return Ok(ApiResponse<PagedResult<ReservaDto>>.Ok(paginado));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var reserva = await _repo.GetByIdAsync(id);
        if (reserva is null)
            return NotFound(ApiResponse<ReservaDto>.Fail("Reserva no encontrada."));
        return Ok(ApiResponse<ReservaDto>.Ok(MapToDto(reserva)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Create([FromBody] CreateReservaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompradorNombre) || string.IsNullOrWhiteSpace(request.CompradorApellido))
            return BadRequest(ApiResponse<ReservaDto>.Fail("Nombre y apellido del comprador son requeridos."));

        if (string.IsNullOrWhiteSpace(request.VendedorNombre) || string.IsNullOrWhiteSpace(request.VendedorApellido))
            return BadRequest(ApiResponse<ReservaDto>.Fail("Nombre y apellido del vendedor son requeridos."));

        if (request.MontoSenia <= 0)
            return BadRequest(ApiResponse<ReservaDto>.Fail("El monto de la seña debe ser mayor a cero."));

        if (request.FechaVencimiento <= request.FechaReserva)
            return BadRequest(ApiResponse<ReservaDto>.Fail("La fecha de vencimiento debe ser posterior a la fecha de reserva."));

        var reserva = MapFromCreate(request);
        var creada = await _repo.CreateAsync(reserva);
        return Ok(ApiResponse<ReservaDto>.Ok(MapToDto(creada), "Reserva creada correctamente."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Operador")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReservaRequest request)
    {
        var reserva = await _repo.GetByIdAsync(id);
        if (reserva is null)
            return NotFound(ApiResponse<ReservaDto>.Fail("Reserva no encontrada."));

        reserva.LeadId = request.LeadId;
        reserva.AgenteId = request.AgenteId;
        reserva.CompradorNombre = request.CompradorNombre;
        reserva.CompradorApellido = request.CompradorApellido;
        reserva.CompradorDni = request.CompradorDni;
        reserva.CompradorTelefono = request.CompradorTelefono;
        reserva.CompradorEmail = request.CompradorEmail;
        reserva.VendedorNombre = request.VendedorNombre;
        reserva.VendedorApellido = request.VendedorApellido;
        reserva.VendedorDni = request.VendedorDni;
        reserva.VendedorTelefono = request.VendedorTelefono;
        reserva.VendedorEmail = request.VendedorEmail;
        reserva.MontoSenia = request.MontoSenia;
        reserva.PrecioTotal = request.PrecioTotal;
        reserva.Moneda = (Moneda)request.Moneda;
        reserva.MedioDeposito = (MedioDeposito)request.MedioDeposito;
        reserva.FechaReserva = request.FechaReserva;
        reserva.FechaVencimiento = request.FechaVencimiento;
        reserva.Estado = (EstadoReserva)request.Estado;
        reserva.Observaciones = request.Observaciones;

        var actualizada = await _repo.UpdateAsync(reserva);
        return Ok(ApiResponse<ReservaDto>.Ok(MapToDto(actualizada), "Reserva actualizada correctamente."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminada = await _repo.DeleteAsync(id);
        if (!eliminada)
            return NotFound(ApiResponse<object>.Fail("Reserva no encontrada."));
        return Ok(ApiResponse<object>.Ok(null, "Reserva eliminada correctamente."));
    }

    private static Reserva MapFromCreate(CreateReservaRequest r) => new()
    {
        PropiedadId = r.PropiedadId,
        LeadId = r.LeadId,
        AgenteId = r.AgenteId,
        CompradorNombre = r.CompradorNombre,
        CompradorApellido = r.CompradorApellido,
        CompradorDni = r.CompradorDni,
        CompradorTelefono = r.CompradorTelefono,
        CompradorEmail = r.CompradorEmail,
        VendedorNombre = r.VendedorNombre,
        VendedorApellido = r.VendedorApellido,
        VendedorDni = r.VendedorDni,
        VendedorTelefono = r.VendedorTelefono,
        VendedorEmail = r.VendedorEmail,
        MontoSenia = r.MontoSenia,
        PrecioTotal = r.PrecioTotal,
        Moneda = (Moneda)r.Moneda,
        MedioDeposito = (MedioDeposito)r.MedioDeposito,
        FechaReserva = r.FechaReserva,
        FechaVencimiento = r.FechaVencimiento,
        Observaciones = r.Observaciones,
    };

    private static ReservaDto MapToDto(Reserva r) => new()
    {
        Id = r.Id,
        PropiedadId = r.PropiedadId,
        PropiedadDireccion = r.Propiedad.Direccion,
        LeadId = r.LeadId,
        AgenteId = r.AgenteId,
        AgenteNombre = r.Agente is not null ? $"{r.Agente.User?.Nombre} {r.Agente.User?.Apellido}" : null,
        CompradorNombre = r.CompradorNombre,
        CompradorApellido = r.CompradorApellido,
        CompradorDni = r.CompradorDni,
        CompradorTelefono = r.CompradorTelefono,
        CompradorEmail = r.CompradorEmail,
        VendedorNombre = r.VendedorNombre,
        VendedorApellido = r.VendedorApellido,
        VendedorDni = r.VendedorDni,
        VendedorTelefono = r.VendedorTelefono,
        VendedorEmail = r.VendedorEmail,
        MontoSenia = r.MontoSenia,
        PrecioTotal = r.PrecioTotal,
        Moneda = r.Moneda.ToString(),
        MedioDeposito = r.MedioDeposito.ToString(),
        FechaReserva = r.FechaReserva,
        FechaVencimiento = r.FechaVencimiento,
        Estado = r.Estado.ToString(),
        Observaciones = r.Observaciones,
        FechaCreacion = r.FechaCreacion,
        FechaActualizacion = r.FechaActualizacion,
    };
}
