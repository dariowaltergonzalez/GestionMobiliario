namespace GestionInmobiliaria.Aplicacion.DTOs;

public class IndiceIclDto
{
    public DateTime Fecha { get; set; }
    public decimal Valor { get; set; }
    public string Origen { get; set; } = string.Empty;
    public DateTime FechaConsulta { get; set; }
}

public class ActualizarIndiceIclResultDto
{
    public int ValoresNuevos { get; set; }
    public IndiceIclDto? Ultima { get; set; }
}
