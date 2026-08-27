using System.Text.RegularExpressions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using GestionInmobiliaria.Dominio.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestionInmobiliaria.Infraestructura.Services;

/// <summary>
/// Implementación de <see cref="IStorageService"/> con Cloudinary — storage permanente para que
/// las fotos/documentos/comprobantes no se pierdan en cada redeploy (a diferencia del disco de
/// Render, que es efímero). Se usa solo en producción; en desarrollo local se sigue usando
/// <see cref="LocalStorageService"/> — ver registro condicional en Program.cs. Ver
/// docs/logica-negocio.md, sección PENDIENTES GENERALES → "Desplegar el sistema".
/// </summary>
public class CloudinaryStorageService : IStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryStorageService> _logger;

    public CloudinaryStorageService(IConfiguration config, ILogger<CloudinaryStorageService> logger)
    {
        var cloudName = config["Cloudinary:CloudName"];
        var apiKey = config["Cloudinary:ApiKey"];
        var apiSecret = config["Cloudinary:ApiSecret"];

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        _logger = logger;
    }

    private static readonly string[] ExtensionesVideo = { ".mp4", ".mov", ".webm", ".avi", ".mkv" };

    public async Task<string> GuardarArchivoAsync(Stream contenido, string nombreArchivo, string carpeta)
    {
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(nombreArchivo, contenido),
            Folder = $"gestioninmobiliaria/{carpeta}",
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false,
        };

        // "raw" para todo salvo videos: el plan free de Cloudinary limita "raw" a 10MB, pero permite
        // hasta 100MB para resource_type "video" — sin esta distinción, cualquier video real (>10MB)
        // fallaba al subir. Fotos/documentos se quedan en "raw" (ya están por debajo de 10MB por
        // validación propia del backend, y así el borrado sigue siendo determinístico).
        var esVideo = ExtensionesVideo.Contains(Path.GetExtension(nombreArchivo).ToLowerInvariant());
        var resourceType = esVideo ? "video" : "raw";

        var resultado = await _cloudinary.UploadAsync(uploadParams, resourceType);
        if (resultado.Error is not null)
            throw new InvalidOperationException($"Error subiendo archivo a Cloudinary: {resultado.Error.Message}");

        return resultado.SecureUrl.ToString();
    }

    public async Task EliminarArchivoAsync(string url)
    {
        // El resource_type usado al subir queda codificado en la URL (".../raw/upload/..." o
        // ".../video/upload/...") — lo leemos de ahí en vez de guardarlo aparte.
        var esVideo = url.Contains("/video/upload/");
        var tipo = esVideo ? ResourceType.Video : ResourceType.Raw;

        var publicId = ExtraerPublicId(url, esVideo);
        if (publicId is null)
        {
            _logger.LogWarning("CloudinaryStorageService: no se pudo extraer el public_id de {Url}, no se elimina nada.", url);
            return;
        }

        var resultado = await _cloudinary.DestroyAsync(new DeletionParams(publicId) { ResourceType = tipo });
        if (resultado.Error is not null)
            _logger.LogWarning("CloudinaryStorageService: error eliminando {PublicId}: {Error}", publicId, resultado.Error.Message);
    }

    // Una URL de Cloudinary tiene la forma:
    // https://res.cloudinary.com/{cloud}/{resource_type}/upload/v{version}/{public_id}
    // En "raw" el public_id INCLUYE la extensión; en "video" (igual que "image") Cloudinary la
    // recorta del public_id, así que hay que quitarla nosotros para poder borrar el recurso.
    // El public_id incluye la carpeta (ej. "gestioninmobiliaria/2/fotos/abc123.jpg").
    private static string? ExtraerPublicId(string url, bool esVideo)
    {
        var patron = esVideo ? @"/upload/(?:v\d+/)?(.+?)(?:\.[a-zA-Z0-9]+)?$" : @"/upload/(?:v\d+/)?(.+)$";
        var match = Regex.Match(url, patron);
        return match.Success ? match.Groups[1].Value : null;
    }
}
