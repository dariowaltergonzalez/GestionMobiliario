import { X } from 'lucide-react'
import { ESTADOS_CONTRATO, estadoContratoNumero, type ContratoDto } from '../../../api/contratos'

function formatMoneda(monto: number, moneda: string) {
  const fmt = monto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  return moneda === 'ARS' ? `$ ${fmt}` : `U$S ${fmt}`
}

function formatFecha(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function Dato({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs text-gray-400 mb-0.5">{label}</p>
      <p className="text-sm text-gray-800 font-medium">{value ?? <span className="text-gray-300">—</span>}</p>
    </div>
  )
}

function Seccion({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-3">{titulo}</p>
      <div className="grid grid-cols-2 gap-4">{children}</div>
    </div>
  )
}

export default function DetalleContratoModal({ contrato, onCerrar }: {
  contrato: ContratoDto
  onCerrar: () => void
}) {
  const estadoInfo = ESTADOS_CONTRATO[estadoContratoNumero(contrato.estado) as keyof typeof ESTADOS_CONTRATO]

  const comisionLocadorTexto = contrato.comisionLocadorPorcentaje != null
    ? `${contrato.comisionLocadorPorcentaje}%`
    : contrato.comisionLocadorMonto != null
      ? formatMoneda(contrato.comisionLocadorMonto, contrato.moneda)
      : 'Sin configurar'

  const comisionLocatarioTexto = contrato.comisionLocatarioPorcentaje != null
    ? `${contrato.comisionLocatarioPorcentaje}%`
    : contrato.comisionLocatarioMonto != null
      ? formatMoneda(contrato.comisionLocatarioMonto, contrato.moneda)
      : 'Sin configurar'

  // Comisión Locador: recurrente, se cobra sobre cada cuota (administración de cobros mensual).
  const tieneComisionLocador = contrato.comisionLocadorPorcentaje != null || contrato.comisionLocadorMonto != null
  const totalTeoricoLocador = tieneComisionLocador && contrato.pagos.length > 0
    ? contrato.pagos.reduce((suma, p) => {
        const comision = contrato.comisionLocadorMonto
          ?? (contrato.comisionLocadorPorcentaje != null ? p.montoEsperado * contrato.comisionLocadorPorcentaje / 100 : 0)
        return suma + comision
      }, 0)
    : null

  // Comisión Locatario: pago único al firmar (honorarios de gestión), no se repite por cuota.
  const tieneComisionLocatario = contrato.comisionLocatarioPorcentaje != null || contrato.comisionLocatarioMonto != null
  const montoUnicoLocatario = contrato.comisionLocatarioMonto
    ?? (contrato.comisionLocatarioPorcentaje != null ? contrato.montoBase * contrato.comisionLocatarioPorcentaje / 100 : null)

  return (
    <div className="fixed inset-0 bg-black/50 flex items-start justify-center z-50 p-4 overflow-y-auto">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-3xl my-6">

        <div className="flex items-center justify-between px-6 pt-5 pb-4 border-b border-gray-100">
          <div>
            <h2 className="font-semibold text-gray-800 text-lg">Detalle del contrato</h2>
            <p className="text-xs text-gray-400 mt-0.5">
              <span className="font-mono text-blue-600">{contrato.codigo}</span> · {contrato.propiedadDireccion}
            </p>
          </div>
          <div className="flex items-center gap-3">
            <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${estadoInfo?.color ?? 'bg-gray-100 text-gray-500'}`}>
              {estadoInfo?.label ?? contrato.estado}
            </span>
            <button onClick={onCerrar} className="text-gray-400 hover:text-gray-600">
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        <div className="px-6 py-5 space-y-6 max-h-[75vh] overflow-y-auto">

          <Seccion titulo="Contrato">
            <Dato label="Tipo" value={contrato.tipo === 'Locacion' ? 'Locación' : 'Boleto de Compraventa'} />
            <Dato label="Propiedad" value={`${contrato.propiedadDireccion}${contrato.propiedadCodigo ? ` (${contrato.propiedadCodigo})` : ''}`} />
            <Dato label="Agente" value={contrato.agenteNombre} />
            <Dato label="Administración de cobros" value={contrato.administracionCobros ? 'Sí' : 'No'} />
          </Seccion>

          <Seccion titulo="Locador / Propietario">
            <Dato label="Nombre" value={`${contrato.locadorNombre} ${contrato.locadorApellido}`} />
            <Dato label="DNI" value={contrato.locadorDni} />
            <Dato label="Email" value={contrato.locadorEmail} />
            <Dato label="Teléfono" value={contrato.locadorTelefono} />
            <Dato label="Domicilio" value={contrato.locadorDomicilio} />
            <Dato label="CUIT" value={contrato.locadorCuit} />
            <Dato label="Banco" value={contrato.locadorBanco} />
            <Dato label="CBU" value={contrato.locadorCbu} />
          </Seccion>

          <Seccion titulo="Locatario / Inquilino">
            <Dato label="Nombre" value={`${contrato.locatarioNombre} ${contrato.locatarioApellido}`} />
            <Dato label="DNI" value={contrato.locatarioDni} />
            <Dato label="Email" value={contrato.locatarioEmail} />
            <Dato label="Teléfono" value={contrato.locatarioTelefono} />
          </Seccion>

          {(contrato.garanteNombre || contrato.garanteApellido) && (
            <Seccion titulo="Garante">
              <Dato label="Nombre" value={`${contrato.garanteNombre ?? ''} ${contrato.garanteApellido ?? ''}`.trim()} />
              <Dato label="DNI" value={contrato.garanteDni} />
              <Dato label="Teléfono" value={contrato.garanteTelefono} />
            </Seccion>
          )}

          <Seccion titulo="Condiciones económicas">
            <Dato label="Monto base" value={formatMoneda(contrato.montoBase, contrato.moneda)} />
            <Dato label="Monto actual" value={formatMoneda(contrato.montoActual, contrato.moneda)} />
            <Dato label="Tipo de ajuste" value={contrato.tipoAjuste} />
            <Dato label="% de ajuste sugerido" value={contrato.porcentajeAjuste != null ? `${contrato.porcentajeAjuste}%` : null} />
            <Dato label="Periodicidad de ajuste" value={contrato.periodicidadAjusteMeses ? `Cada ${contrato.periodicidadAjusteMeses} meses` : null} />
            <Dato label="Ajuste automático" value={contrato.ajusteAutomatico ? 'Activado' : 'Desactivado'} />
            <Dato label="Día de vencimiento" value={contrato.diaVencimientoPago ? `Día ${contrato.diaVencimientoPago}` : null} />
            <Dato label="Último ajuste aplicado" value={formatFecha(contrato.fechaUltimoAjuste)} />
          </Seccion>

          <div>
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-3">Comisiones de la inmobiliaria</p>
            <div className="grid grid-cols-2 gap-4">

              <div className="border border-gray-100 rounded-xl p-4">
                <p className="text-xs text-gray-400 mb-2">
                  Comisión Locador (dueño) — <span className="font-medium text-gray-500">mensual, por administración de cobros</span>
                </p>
                <p className="text-sm text-gray-800 font-medium mb-2">{comisionLocadorTexto}</p>
                {tieneComisionLocador && (
                  <div className="pt-2 border-t border-gray-100">
                    <p className="text-xs text-gray-400 mb-0.5">Monto total teórico (todas las cuotas del contrato)</p>
                    <p className="text-sm font-semibold text-gray-800">
                      {totalTeoricoLocador != null
                        ? formatMoneda(totalTeoricoLocador, contrato.moneda)
                        : 'No aplica (el contrato no genera cuotas)'}
                    </p>
                  </div>
                )}
              </div>

              <div className="border border-gray-100 rounded-xl p-4">
                <p className="text-xs text-gray-400 mb-2">
                  Comisión Locatario (inquilino) — <span className="font-medium text-gray-500">pago único, honorarios de gestión del contrato</span>
                </p>
                <p className="text-sm text-gray-800 font-medium mb-2">{comisionLocatarioTexto}</p>
                {tieneComisionLocatario && (
                  <div className="pt-2 border-t border-gray-100">
                    <p className="text-xs text-gray-400 mb-0.5">Monto (una sola vez, al firmar)</p>
                    <p className="text-sm font-semibold text-gray-800">
                      {montoUnicoLocatario != null ? formatMoneda(montoUnicoLocatario, contrato.moneda) : '—'}
                    </p>
                  </div>
                )}
              </div>

            </div>
          </div>

          <Seccion titulo="Vigencia">
            <Dato label="Fecha de inicio" value={formatFecha(contrato.fechaInicio)} />
            <Dato label="Fecha de fin" value={formatFecha(contrato.fechaFin)} />
            <Dato label="Fecha de escrituración" value={formatFecha(contrato.fechaEscrituracion)} />
          </Seccion>

          {(contrato.motivoRescision || contrato.motivoAnulacion) && (
            <Seccion titulo="Baja del contrato">
              {contrato.motivoRescision && <Dato label="Motivo de rescisión" value={contrato.motivoRescision} />}
              {contrato.fechaRescision && <Dato label="Fecha de rescisión" value={formatFecha(contrato.fechaRescision)} />}
              {contrato.motivoAnulacion && <Dato label="Motivo de anulación" value={contrato.motivoAnulacion} />}
              {contrato.fechaAnulacion && <Dato label="Fecha de anulación" value={formatFecha(contrato.fechaAnulacion)} />}
            </Seccion>
          )}

          {contrato.observaciones && (
            <Seccion titulo="Observaciones">
              <div className="col-span-2">
                <p className="text-sm text-gray-700 whitespace-pre-wrap">{contrato.observaciones}</p>
              </div>
            </Seccion>
          )}

        </div>

        <div className="px-6 py-4 border-t border-gray-100">
          <button onClick={onCerrar}
            className="w-full border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
            Cerrar
          </button>
        </div>
      </div>
    </div>
  )
}
