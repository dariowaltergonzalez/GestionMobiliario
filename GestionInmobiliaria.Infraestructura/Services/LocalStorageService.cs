using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace GestionInmobiliaria.Infraestructura.Services;

public class LocalStorageService : IStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<string> GuardarArchivoAsync(Stream contenido, string nombreArchivo, string carpeta)
    {
        var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
        var nuevoNombre = $"{Guid.NewGuid()}{extension}";
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var carpetaCompleta = Path.Combine(webRoot, "uploads", carpeta);
        Directory.CreateDirectory(carpetaCompleta);
        var rutaCompleta = Path.Combine(carpetaCompleta, nuevoNombre);

        using var fileStream = new FileStream(rutaCompleta, FileMode.Create);
        await contenido.CopyToAsync(fileStream);

        return $"/uploads/{carpeta}/{nuevoNombre}";
    }

    public Task EliminarArchivoAsync(string url)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var relativePath = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var rutaCompleta = Path.Combine(webRoot, relativePath);
        if (File.Exists(rutaCompleta))
            File.Delete(rutaCompleta);
        return Task.CompletedTask;
    }
}
