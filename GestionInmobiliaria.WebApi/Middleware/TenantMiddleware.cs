using System.Security.Claims;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.WebApi.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        // Si el JWT ya tiene tenantId (usuario autenticado) lo usamos directamente
        var tenantClaim = context.User?.FindFirstValue("tenantId");
        if (tenantClaim != null && int.TryParse(tenantClaim, out var tenantIdFromJwt))
        {
            context.Items["TenantId"] = tenantIdFromJwt;
            await _next(context);
            return;
        }

        // Para endpoints públicos resolvemos desde el header X-Tenant
        var slug = context.Request.Headers["X-Tenant"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(slug))
        {
            var tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == slug && t.Activo);

            if (tenant is not null)
                context.Items["TenantId"] = tenant.Id;
        }

        await _next(context);
    }
}
