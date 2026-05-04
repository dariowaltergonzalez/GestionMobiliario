using GestionInmobiliaria.Dominio.Common;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.Infraestructura.Extensions;

public static class PaginationExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pagina,
        int tamano)
    {
        var total = await query.CountAsync();
        var items = await query
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            Pagina = pagina,
            Tamano = tamano,
            TotalRegistros = total,
            TotalPaginas = (int)Math.Ceiling(total / (double)tamano)
        };
    }
}
