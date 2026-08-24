namespace GestionInmobiliaria.Aplicacion.DTOs;

public class IndiceUvaDto
{
    public DateTime Fecha { get; set; }
    public decimal Valor { get; set; }
    public string Origen { get; set; } = string.Empty;
    public DateTime FechaConsulta { get; set; }
}

public class ActualizarIndiceUvaResultDto
{
    public int ValoresNuevos { get; set; }
    public IndiceUvaDto? Ultima { get; set; }
}
