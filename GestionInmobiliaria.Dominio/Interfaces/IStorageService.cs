namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IStorageService
{
    Task<string> GuardarArchivoAsync(Stream contenido, string nombreArchivo, string carpeta);
    Task EliminarArchivoAsync(string url);
}
