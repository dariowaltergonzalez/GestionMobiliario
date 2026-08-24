using System.Security.Cryptography;

namespace GestionInmobiliaria.Dominio.Common;

/// <summary>
/// Genera y parsea los tokens del Portal de autoservicio (Inquilino/Propietario, sin login) — ver
/// docs/logica-negocio.md, sección PORTAL DE AUTOSERVICIO. Formato: "{TenantId}.{secreto}". El
/// TenantId no necesita ser secreto (es solo routing, para no tener que escanear todos los tenants
/// buscando el token) — la seguridad real está en que "secreto" es aleatorio e imposible de adivinar.
/// </summary>
public static class TokenPortal
{
    public static string Generar(int tenantId)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var secreto = Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
        return $"{tenantId}.{secreto}";
    }

    public static int? ParsearTenantId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var puntoIdx = token.IndexOf('.');
        if (puntoIdx <= 0) return null;
        return int.TryParse(token[..puntoIdx], out var tenantId) ? tenantId : null;
    }
}
