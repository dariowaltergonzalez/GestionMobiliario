namespace GestionInmobiliaria.Aplicacion.DTOs;

public class ClausulaContratoDto
{
    public int Id { get; set; }
    public int Orden { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class CreateClausulaContratoRequest
{
    public string Numero { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
}

public class UpdateClausulaContratoRequest
{
    public string Numero { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
