namespace GestionInmobiliaria.Aplicacion.DTOs;

public class EmailMessage
{
    public string Destinatario { get; set; } = string.Empty;
    public string? NombreDestinatario { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public List<string> Cc { get; set; } = [];
    public List<EmailAdjunto> Adjuntos { get; set; } = [];
}

public class EmailAdjunto
{
    public string NombreArchivo { get; set; } = string.Empty;
    public byte[] Contenido { get; set; } = [];
    public string TipoMime { get; set; } = "application/pdf";
}
