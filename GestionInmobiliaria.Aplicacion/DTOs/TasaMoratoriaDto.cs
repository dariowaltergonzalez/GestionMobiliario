namespace GestionInmobiliaria.Aplicacion.DTOs;

public class TasaMoratoriaDto
{
    public DateTime Fecha { get; set; }
    public decimal Valor { get; set; }
    public string Origen { get; set; } = string.Empty;
    public DateTime FechaConsulta { get; set; }
}

public class ActualizarTasaMoratoriaResultDto
{
    public int ValoresNuevos { get; set; }
    public TasaMoratoriaDto? Ultima { get; set; }
}
