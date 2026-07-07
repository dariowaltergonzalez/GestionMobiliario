using GestionInmobiliaria.Dominio.Common;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;
using GestionInmobiliaria.Infraestructura.Extensions;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Repositorios;

public class ReservaRepository : IReservaRepository
{
    private readonly ApplicationDbContext _context;

    public ReservaRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<Reserva>> GetPagedAsync(
        PaginationParams paginacion,
        string? buscar = null,
        EstadoReserva? estado = null,
        int? propiedadId = null)
    {
        var query = _context.Reservas
            .Include(r => r.Propiedad)
            .Include(r => r.Agente).ThenInclude(a => a!.User)
            .AsQueryable();

        if (estado.HasValue)
            query = query.Where(r => r.Estado == estado.Value);

        if (propiedadId.HasValue)
            query = query.Where(r => r.PropiedadId == propiedadId.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(r =>
                r.CompradorNombre.Contains(buscar) ||
                r.CompradorApellido.Contains(buscar) ||
                r.Propiedad.Direccion.Contains(buscar) ||
                (r.CompradorEmail != null && r.CompradorEmail.Contains(buscar)) ||
                (r.CompradorDni != null && r.CompradorDni.Contains(buscar)));

        query = query.OrderByDescending(r => r.FechaCreacion);

        return await query.ToPagedResultAsync(paginacion.Pagina, paginacion.Tamano);
    }

    public async Task<Reserva?> GetByIdAsync(int id) =>
        await _context.Reservas
            .Include(r => r.Propiedad).ThenInclude(p => p.Propietario)
            .Include(r => r.Agente).ThenInclude(a => a!.User)
            .Include(r => r.Lead)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Reserva> CreateAsync(Reserva reserva)
    {
        reserva.FechaCreacion = DateTime.UtcNow;
        reserva.FechaActualizacion = DateTime.UtcNow;
        _context.Reservas.Add(reserva);

        // Propiedad pasa a Reservada
        var propiedad = await _context.Propiedades.FindAsync(reserva.PropiedadId);
        if (propiedad is not null)
            propiedad.Estado = EstadoPropiedad.Reservada;

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(reserva.Id))!;
    }

    public async Task<Reserva> UpdateAsync(Reserva reserva)
    {
        reserva.FechaActualizacion = DateTime.UtcNow;

        // Sincronizar estado de la propiedad
        var propiedad = await _context.Propiedades.FindAsync(reserva.PropiedadId);
        if (propiedad is not null)
        {
            propiedad.Estado = reserva.Estado switch
            {
                EstadoReserva.Vigente    => EstadoPropiedad.Reservada,
                EstadoReserva.Convertida => EstadoPropiedad.BoletoFirmado,
                _                        => EstadoPropiedad.Disponible
            };
        }

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(reserva.Id))!;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var reserva = await _context.Reservas.FindAsync(id);
        if (reserva is null) return false;

        reserva.Activo = false;
        reserva.FechaActualizacion = DateTime.UtcNow;

        // Liberar la propiedad si esta reserva era la activa
        var propiedad = await _context.Propiedades.FindAsync(reserva.PropiedadId);
        if (propiedad is not null && propiedad.Estado == EstadoPropiedad.Reservada)
            propiedad.Estado = EstadoPropiedad.Disponible;

        await _context.SaveChangesAsync();
        return true;
    }
}
