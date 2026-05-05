using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GestionInmobiliaria.Infraestructura.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public int? TenantId
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx?.Items["TenantId"] is int id)
                return id;
            return null;
        }
    }
}
