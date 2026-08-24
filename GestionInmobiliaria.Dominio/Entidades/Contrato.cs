namespace GestionInmobiliaria.Dominio.Entidades;

public enum TipoContrato
{
    Locacion = 1,
    BoletoCompraventa = 2
}

public enum EstadoContrato
{
    Borrador = 1,
    Vigente = 2,
    Finalizado = 3,
    Rescindido = 4,
    Anulado = 5
}

public enum TipoAjuste
{
    Fijo = 1,
    IndiceICL = 2,
    Porcentaje = 3,
    Otro = 4,
    IndiceIPC = 5,
    IndiceUVA = 6
}

public enum EstadoPago
{
    Pendiente = 1,
    Pagado = 2,
    Atrasado = 3,
    Anulado = 4
}

public enum MedioPago
{
    Efectivo = 1,
    Debito = 2,
    Credito = 3,
    Cheque = 4,
}

public class Contrato : IAuditable
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;

    public TipoContrato Tipo { get; set; }
    public EstadoContrato Estado { get; set; } = EstadoContrato.Borrador;

    public int PropiedadId { get; set; }
    public Propiedad Propiedad { get; set; } = null!;

    public int? ReservaId { get; set; }
    public Reserva? Reserva { get; set; }

    public int? AgenteId { get; set; }
    public Agente? Agente { get; set; }

    // Referencias opcionales sin FK real (para joins manuales)
    public int? PropietarioRefId { get; set; }
    public int? InquilinoRefId { get; set; }

    // Snapshot locador/vendedor
    public string LocadorNombre { get; set; } = string.Empty;
    public string LocadorApellido { get; set; } = string.Empty;
    public string? LocadorDni { get; set; }
    public string? LocadorEmail { get; set; }
    public string? LocadorTelefono { get; set; }
    public string? LocadorDomicilio { get; set; }
    public string? LocadorBanco { get; set; }
    public string? LocadorCbu { get; set; }
    public string? LocadorCuit { get; set; }

    // Snapshot locatario/comprador
    public string LocatarioNombre { get; set; } = string.Empty;
    public string LocatarioApellido { get; set; } = string.Empty;
    public string? LocatarioDni { get; set; }
    public string? LocatarioEmail { get; set; }
    public string? LocatarioTelefono { get; set; }

    // Snapshot garante (opcional)
    public string? GaranteNombre { get; set; }
    public string? GaranteApellido { get; set; }
    public string? GaranteDni { get; set; }
    public string? GaranteTelefono { get; set; }

    // Condiciones económicas
    public decimal MontoBase { get; set; }
    public Moneda Moneda { get; set; } = Moneda.ARS;
    public TipoAjuste TipoAjuste { get; set; } = TipoAjuste.Fijo;
    public int? PeriodicidadAjusteMeses { get; set; }
    public int? DiaVencimientoPago { get; set; }

    // Comisiones (doble: puede cobrar a ambas partes)
    public decimal? ComisionLocadorPorcentaje { get; set; }
    public decimal? ComisionLocadorMonto { get; set; }
    public decimal? ComisionLocatarioPorcentaje { get; set; }
    public decimal? ComisionLocatarioMonto { get; set; }

    public bool AdministracionCobros { get; set; } = false;

    // Punitorio por mora: % fijo diario simple. Si es null/0, el cálculo cae a la tasa TIM del BCRA
    // (ver TasaMoratoria y docs/logica-negocio.md, sección PUNITORIOS). AplicaPunitorios es el
    // interruptor general — si está en false, no se calcula ni se muestra nada para este contrato,
    // sin importar PunitorioPorcentaje ni DiaVencimientoPago (ese campo lo sigue usando el aviso de
    // vencimiento próximo aunque los punitorios estén apagados, son cosas independientes).
    public bool AplicaPunitorios { get; set; } = true;
    public decimal? PunitorioPorcentaje { get; set; }

    // Ajuste de cuotas
    public decimal? PorcentajeAjuste { get; set; }
    public decimal MontoActual { get; set; }
    public DateTime? FechaUltimoAjuste { get; set; }

    // Interruptor del ajuste automático (ICL/IPC/Porcentaje según TipoAjuste) — default false a
    // propósito (a diferencia de AplicaPunitorios que es true): cada contrato negoció una cláusula
    // de ajuste específica, activarlo para todos de golpe aplicaría aumentos no acordados.
    public bool AjusteAutomatico { get; set; } = false;

    // Vigencia
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime? FechaEscrituracion { get; set; }

    // Transiciones de estado
    public string? MotivoRescision { get; set; }
    public DateTime? FechaRescision { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public string? Observaciones { get; set; }
    public string? ArchivoUrl { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }

    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public ICollection<AjusteContrato> Ajustes { get; set; } = new List<AjusteContrato>();
    public ICollection<DocumentoContrato> Documentos { get; set; } = new List<DocumentoContrato>();
}

public class DocumentoContrato : IAuditable
{
    public int Id { get; set; }

    public int ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;

    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaRelativa { get; set; } = string.Empty;
    public string TipoMime { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }
}

public class Pago : IAuditable
{
    public int Id { get; set; }

    public int ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;

    public int NumeroCuota { get; set; }
    public DateTime Periodo { get; set; }

    public decimal MontoEsperado { get; set; }
    public decimal? MontoPagado { get; set; }
    public DateTime? FechaPago { get; set; }

    public EstadoPago Estado { get; set; } = EstadoPago.Pendiente;
    public string? Observaciones { get; set; }

    // Control de recordatorios de vencimiento (RecordatorioVencimientoService) — evita reenviar el
    // mismo aviso si el chequeo diario corre más de una vez, y evita perderlo si el server estuvo
    // caído justo el día exacto.
    public bool AvisoVencimiento7DiasEnviado { get; set; }
    public bool AvisoVencimiento1DiaEnviado { get; set; }

    // Punitorio por mora efectivamente cobrado junto con esta cuota, congelado al momento de cobrar
    // (el cálculo "en vivo" que se ve en pantalla antes de cobrar cambia día a día, esto no — ver
    // IPunitorioService / docs/logica-negocio.md sección PUNITORIOS).
    public decimal? MontoPunitorioCobrado { get; set; }
    public int? DiasAtrasoPunitorioCobrado { get; set; }
    public DateTime? FechaVencimientoPunitorioCobrado { get; set; }
    public string? DetallePunitorioCobrado { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }

    public List<PagoDetalle> Detalles { get; set; } = [];
}

public class AjusteContrato : IAuditable
{
    public int Id { get; set; }

    public int ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;

    public DateTime FechaAplicacion { get; set; }
    public decimal MontoPrevio { get; set; }
    public decimal MontoNuevo { get; set; }
    public decimal? Porcentaje { get; set; }
    public string TipoAjuste { get; set; } = string.Empty;
    public string? Observaciones { get; set; }

    // Solo tiene valor cuando el ajuste se calculó con un índice (ICL/IPC) — texto libre con los
    // valores exactos usados en cada fecha, mismo criterio que Pago.DetallePunitorioCobrado (así
    // queda trazable sin necesitar columnas separadas por índice).
    public string? DetalleIndiceUsado { get; set; }

    // true si lo disparó el proceso automático, false si lo aplicó un Admin/Operador a mano.
    public bool Automatico { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }
}

public class PagoDetalle : IAuditable
{
    public int Id { get; set; }
    public int PagoId { get; set; }
    public Pago Pago { get; set; } = null!;
    public MedioPago Medio { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public string? ChequeBanco { get; set; }
    public string? ChequeNumero { get; set; }
    public DateTime? ChequeFechaVencimiento { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }
}

public enum EstadoLiquidacion
{
    Pendiente = 1,
    Liquidado = 2,
    Parcial = 3,
}

/// <summary>
/// Lo que le corresponde transferir al propietario por un Pago cobrado, descontando la
/// comisión de administración del contrato (Contrato.ComisionLocadorPorcentaje/Monto).
/// Se genera automáticamente al marcar un Pago como Pagado — ver PagosController. El Estado y
/// FechaLiquidacion se recalculan a partir de la suma de los Abonos (ver LiquidacionAbono) —
/// no se setean a mano.
/// </summary>
public class Liquidacion : IAuditable
{
    public int Id { get; set; }

    public int PagoId { get; set; }
    public Pago Pago { get; set; } = null!;

    public decimal MontoCobrado { get; set; }
    public decimal? ComisionPorcentaje { get; set; }
    public decimal? ComisionMonto { get; set; }
    public decimal MontoComision { get; set; }
    public decimal MontoALiquidar { get; set; }

    // Total de Gastos a cargo del Propietario descontados automáticamente al generar esta
    // Liquidacion (ver Gasto.LiquidacionId para el detalle de cada gasto descontado).
    public decimal MontoGastos { get; set; }

    public EstadoLiquidacion Estado { get; set; } = EstadoLiquidacion.Pendiente;
    public DateTime? FechaLiquidacion { get; set; }
    public string? Observaciones { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }

    public List<LiquidacionAbono> Abonos { get; set; } = [];
    public List<Gasto> Gastos { get; set; } = [];
}

/// <summary>
/// Una transferencia parcial (o total) contra una Liquidacion. Puede haber varias por Liquidacion
/// para cubrir el caso de pagarle al propietario en partes.
/// </summary>
public class LiquidacionAbono : IAuditable
{
    public int Id { get; set; }

    public int LiquidacionId { get; set; }
    public Liquidacion Liquidacion { get; set; } = null!;

    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public MedioPago Medio { get; set; }
    public string? CbuCvuDestino { get; set; }
    public string? EntidadDestino { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? Observaciones { get; set; }

    // Imagen del comprobante subida por el admin (guardada siempre que se sube, aunque la
    // extracción por IA falle o el usuario corrija los datos a mano). Ver IReciboIaService.
    public string? ComprobanteUrl { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public int TenantId { get; set; }
}
