namespace GestionInmobiliaria.Aplicacion.Services;

public interface IIndiceIpcService
{
    /// <summary>
    /// Trae del INDEC los valores mensuales faltantes del IPC Nacional Nivel General y los guarda. La
    /// primera vez que se corre (tabla vacía) hace una carga histórica completa; las siguientes veces
    /// solo trae los meses nuevos desde el último valor guardado. Devuelve la cantidad de filas nuevas
    /// insertadas.
    /// </summary>
    Task<int> ActualizarAsync();
}
