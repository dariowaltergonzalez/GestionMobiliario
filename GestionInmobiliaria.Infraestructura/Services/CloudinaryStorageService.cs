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

        // Siempre "raw" (no "auto"): no usamos transformaciones de imagen de Cloudinary, así que no
        // hace falta que detecte el tipo — y usar siempre el mismo resource_type hace que borrar el
        // archivo después sea determinístico (con "auto" el borrado necesitaría saber qué tipo le
        // asignó Cloudinary al subir, dato que no guardamos en ningún lado).
        var resultado = await _cloudinary.UploadAsync(uploadParams, "raw");
        if (resultado.Error is not null)
            throw new InvalidOperationException($"Error subiendo archivo a Cloudinary: {resultado.Error.Message}");

        return resultado.SecureUrl.ToString();
    }

    public async Task EliminarArchivoAsync(string url)
    {
        var publicId = ExtraerPublicId(url);
        if (publicId is null)
        {
            _logger.LogWarning("CloudinaryStorageService: no se pudo extraer el public_id de {Url}, no se elimina nada.", url);
            return;
        }

        var resultado = await _cloudinary.DestroyAsync(new DeletionParams(publicId) { ResourceType = ResourceType.Raw });
        if (resultado.Error is not null)
            _logger.LogWarning("CloudinaryStorageService: error eliminando {PublicId}: {Error}", publicId, resultado.Error.Message);
    }

    // Una URL "raw" de Cloudinary tiene la forma:
    // https://res.cloudinary.com/{cloud}/raw/upload/v{version}/{public_id}
    // A diferencia de image/video, en "raw" el public_id INCLUYE la extensión — no hay que sacarla.
    // El public_id incluye la carpeta (ej. "gestioninmobiliaria/2/fotos/abc123.jpg").
    private static string? ExtraerPublicId(string url)
    {
        var match = Regex.Match(url, @"/upload/(?:v\d+/)?(.+)$");
        return match.Success ? match.Groups[1].Value : null;
    }
}
