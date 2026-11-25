namespace GestionInmobiliaria.Dominio.Entidades;

public enum EstrategiaAsignacionLeads
{
    Manual = 1,
    RotacionSimple = 2,
    PorCargaDeTrabajo = 3,
}

public class ConfiguracionEmpresa
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string? Cuit { get; set; }
    public string? CondicionFiscal { get; set; }
    public string? LogoUrl { get; set; }
    public string? Slogan { get; set; }
    public string? Telefono { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? SitioWeb { get; set; }
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? Provincia { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Pais { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public string? Twitter { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }

    // Configuración SMTP
    public bool EmailHabilitado { get; set; }
    public string? EmailSmtpHost { get; set; }
    public int EmailSmtpPuerto { get; set; } = 587;
    public string? EmailSmtpUsuario { get; set; }
    public string? EmailSmtpPassword { get; set; }
    public string? EmailNombreRemitente { get; set; }

    // Reparto automático de Leads — ver docs/logica-negocio.md, sección LEADS. Solo se aplica cuando
    // se crea un Lead sin agente ya determinado (ej. "Solicitar visita" sobre una propiedad sin
    // agente fijo, o el Lead automático de una Tasación) — el alta manual desde el dashboard y la
    // propiedad con agente fijo siguen mandando siempre. UltimoAgenteAsignadoLeadId es estado interno
    // (el cursor de la rotación), no se expone para editar desde la UI.
    public EstrategiaAsignacionLeads EstrategiaAsignacionLeads { get; set; } = EstrategiaAsignacionLeads.Manual;
    public int? UltimoAgenteAsignadoLeadId { get; set; }
}
