namespace GestionInmobiliaria.Dominio.Entidades;

public class ClausulaContrato : IAuditable
{
    public int Id { get; set; }
    public int Orden { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }
}

/// <summary>
/// Foto de las cláusulas tal como estaban activas en <see cref="ClausulaContrato"/> (la plantilla
/// compartida) en el momento en que un Contrato pasó a Vigente por primera vez — ver
/// docs/logica-negocio.md, sección CONTRATO. Se genera una sola vez (ver
/// ContratoRepository.GenerarClausulasSnapshotSiCorrespondeAsync) y de ahí en más, generar el PDF de
/// ESE contrato siempre usa esta foto (ContratoRepository.GetClausulasParaPdfAsync), nunca la
/// plantilla en vivo — así una reimpresión siempre coincide con lo que se firmó, aunque la plantilla
/// compartida cambie después (bug real encontrado el 2026-11-19: sin esto, reimprimir un contrato
/// viejo mostraba la plantilla ACTUAL, no la que estaba activa cuando se firmó).
/// </summary>
public class ContratoClausula : IAuditable
{
    public int Id { get; set; }

    public int ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;

    public int Orden { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }
}
