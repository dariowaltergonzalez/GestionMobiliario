using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Dominio.Interfaces;

namespace GestionInmobiliaria.WebApi.Middleware;

public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IAppLogRepository logRepo)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var origen = $"{context.Request.Method} {context.Request.Path}";
            await logRepo.RegistrarAsync(
                NivelLog.Error,
                origen,
                ex.Message,
                ex.ToString()
            );

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"errors\":[\"Error interno del servidor.\"]}");
        }
    }
}
