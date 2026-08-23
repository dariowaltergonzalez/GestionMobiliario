namespace GestionInmobiliaria.Aplicacion.Services;

public interface ITasaMoratoriaService
{
    /// <summary>
    /// Trae del BCRA los valores faltantes de la serie TIM y los guarda. La primera vez que se corre
    /// (tabla vacía) hace una carga histórica completa; las siguientes veces solo trae los días nuevos
    /// desde el último valor guardado. Devuelve la cantidad de filas nuevas insertadas.
    /// </summary>
    Task<int> ActualizarAsync();
}
