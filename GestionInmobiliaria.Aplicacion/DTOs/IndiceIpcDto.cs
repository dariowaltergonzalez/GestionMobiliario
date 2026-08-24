namespace GestionInmobiliaria.Aplicacion.DTOs;

public class IndiceIpcDto
{
    public DateTime Fecha { get; set; }
    public decimal Valor { get; set; }
    public string Origen { get; set; } = string.Empty;
    public DateTime FechaConsulta { get; set; }
}

public class ActualizarIndiceIpcResultDto
{
    public int ValoresNuevos { get; set; }
    public IndiceIpcDto? Ultima { get; set; }
}
