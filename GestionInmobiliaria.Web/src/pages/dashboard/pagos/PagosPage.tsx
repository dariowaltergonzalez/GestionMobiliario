import { useState, useEffect, useCallback } from 'react'
import { ChevronLeft, ChevronRight, FileDown, Plus, Trash2, AlertTriangle } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getPagosConsolidados, getPagoMetricas, updatePagoConsolidado, descargarReciboPago,
  type PagoListDto, type PagoMetricasDto, type FiltrosPagos,
} from '../../../api/pagos'
import {
  ESTADOS_PAGO, MEDIOS_PAGO, REFERENCIA_PLACEHOLDER, estadoPagoNumero,
  type EstadoPago, type MedioPago, type PagoDetalleRequest,
} from '../../../api/contratos'

// ── Helpers ─────────────────────────────────────────────────────────────────

function mesAnio(iso: string) {
  return new Date(iso).toLocaleDateString('es-AR', { month: 'short', year: 'numeric' })
}

const toDateInput = (iso: string) => iso.split('T')[0]
const hoy = new Date().toISOString().split('T')[0]

const MESES = [
  [1, 'Enero'], [2, 'Febrero'], [3, 'Marzo'], [4, 'Abril'],
  [5, 'Mayo'], [6, 'Junio'], [7, 'Julio'], [8, 'Agosto'],
  [9, 'Septiembre'], [10, 'Octubre'], [11, 'Noviembre'], [12, 'Diciembre'],
] as const

function labelMedio(medio: string) {
  const map: Record<string, string> = {
    Efectivo: 'Efectivo',
    Debito: 'Transf. / Débito',
    Credito: 'Tarjeta de crédito',
    Cheque: 'Cheque',
  }
  return map[medio] ?? medio
}

// ── EstadoBadge ──────────────────────────────────────────────────────────────

function EstadoBadge({ estado }: { estado: string }) {
  const num = estadoPagoNumero(estado) as EstadoPago
  const info = ESTADOS_PAGO[num] ?? { label: estado, color: 'bg-gray-100 text-gray-500' }
  return <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${info.color}`}>{info.label}</span>
}

// ── MetricCard ───────────────────────────────────────────────────────────────

function MetricCard({ label, value, color }: { label: string; value: string | number; color: string }) {
  return (
    <div className="bg-white rounded-2xl border border-gray-100 p-5 shadow-sm">
      <div className={`text-xs font-semibold uppercase tracking-wider mb-2 ${color}`}>{label}</div>
      <div className="text-2xl font-bold text-gray-800 truncate">{value}</div>
    </div>
  )
}

// ── Ítem de pago (una fila del modal) ────────────────────────────────────────

interface DetalleItem extends PagoDetalleRequest {
  _key: number
}

function DetalleRow({ item, onChange, onRemove, canRemove }: {
  item: DetalleItem
  onChange: (updated: DetalleItem) => void
  onRemove: () => void
  canRemove: boolean
}) {
  const medioNum = item.medio as MedioPago
  const esCheque = medioNum === 4
  const refPlaceholder = !esCheque ? REFERENCIA_PLACEHOLDER[medioNum] : ''

  return (
    <div className="border border-gray-200 rounded-xl p-3 space-y-2">
      <div className="flex gap-2 items-start">
        <div className="flex-1">
          <label className="block text-xs text-gray-500 mb-1">Forma de pago</label>
          <select
            value={item.medio}
            onChange={e => onChange({ ...item, medio: Number(e.target.value), referencia: undefined, chequeBanco: undefined, chequeNumero: undefined, chequeFechaVencimiento: undefined })}
            className="w-full border border-gray-200 rounded-lg px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
          >
            {Object.entries(MEDIOS_PAGO).map(([k, v]) => (
              <option key={k} value={k}>{v}</option>
            ))}
          </select>
        </div>
        <div className="w-36">
          <label className="block text-xs text-gray-500 mb-1">Monto</label>
          <input
            type="number"
            value={item.monto || ''}
            onChange={e => onChange({ ...item, monto: Number(e.target.value) })}
            placeholder="0"
            className="w-full border border-gray-200 rounded-lg px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        {canRemove && (
          <button onClick={onRemove} className="mt-5 p-1.5 text-gray-300 hover:text-red-500 transition-colors">
            <Trash2 className="w-4 h-4" />
          </button>
        )}
      </div>

      {/* Referencia para débito / crédito */}
      {!esCheque && refPlaceholder && (
        <div>
          <label className="block text-xs text-gray-500 mb-1">Referencia</label>
          <input
            type="text"
            value={item.referencia ?? ''}
            onChange={e => onChange({ ...item, referencia: e.target.value })}
            placeholder={refPlaceholder}
            className="w-full border border-gray-200 rounded-lg px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      )}

      {/* Campos específicos de cheque */}
      {esCheque && (
        <div className="grid grid-cols-3 gap-2">
          <div>
            <label className="block text-xs text-gray-500 mb-1">Banco</label>
            <input
              type="text"
              value={item.chequeBanco ?? ''}
              onChange={e => onChange({ ...item, chequeBanco: e.target.value })}
              placeholder="Ej: Nación"
              className="w-full border border-gray-200 rounded-lg px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">N° cheque</label>
            <input
              type="text"
              value={item.chequeNumero ?? ''}
              onChange={e => onChange({ ...item, chequeNumero: e.target.value })}
              placeholder="12345678"
              className="w-full border border-gray-200 rounded-lg px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">Fecha vto.</label>
            <input
              type="date"
              value={item.chequeFechaVencimiento ?? ''}
              onChange={e => onChange({ ...item, chequeFechaVencimiento: e.target.value })}
              className="w-full border border-gray-200 rounded-lg px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>
      )}
    </div>
  )
}

// ── Modal: Registrar cobro ───────────────────────────────────────────────────

interface ModalProps {
  pago: PagoListDto
  onGuardado: () => void
  onCerrar: () => void
}

let nextKey = 1

function RegistrarCobroModal({ pago, onGuardado, onCerrar }: ModalProps) {
  const [fecha, setFecha] = useState(pago.fechaPago ? toDateInput(pago.fechaPago) : hoy)
  const [observaciones, setObservaciones] = useState(pago.observaciones ?? '')
  const [detalles, setDetalles] = useState<DetalleItem[]>([
    { _key: nextKey++, medio: 1, monto: pago.montoEsperado },
  ])
  const [confirmando, setConfirmando] = useState(false)
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')

  const total = detalles.reduce((s, d) => s + (d.monto || 0), 0)
  const diferencia = total - pago.montoEsperado

  const agregarDetalle = () => {
    setDetalles(prev => [...prev, { _key: nextKey++, medio: 1, monto: 0 }])
  }

  const actualizarDetalle = (key: number, updated: DetalleItem) => {
    setDetalles(prev => prev.map(d => d._key === key ? updated : d))
  }

  const quitarDetalle = (key: number) => {
    setDetalles(prev => prev.filter(d => d._key !== key))
  }

  const validar = () => {
    if (detalles.length === 0) return 'Agregá al menos una forma de pago.'
    if (detalles.some(d => !d.monto || d.monto <= 0)) return 'Todos los montos deben ser mayores a cero.'
    return ''
  }

  const handleConfirmar = async () => {
    setGuardando(true)
    setError('')
    try {
      await updatePagoConsolidado(pago.contratoId, pago.id, {
        estado: 2,
        fechaPago: new Date(fecha).toISOString(),
        observaciones: observaciones.trim() || undefined,
        detalles: detalles.map(({ _key: _k, ...d }) => ({
          ...d,
          chequeFechaVencimiento: d.chequeFechaVencimiento || undefined,
        })),
      })
      onGuardado()
    } catch {
      setError('No se pudo registrar el cobro. Intente nuevamente.')
      setConfirmando(false)
    } finally {
      setGuardando(false)
    }
  }

  const handleSolicitarConfirmacion = () => {
    const err = validar()
    if (err) { setError(err); return }
    setError('')
    setConfirmando(true)
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] flex flex-col">

        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-100 shrink-0">
          <h2 className="text-lg font-bold text-gray-800">Registrar cobro</h2>
          <p className="text-sm text-gray-500 mt-0.5">
            {pago.contratoCodigo} — {pago.propiedadDireccion} — {mesAnio(pago.periodo)} (cuota #{pago.numeroCuota})
          </p>
        </div>

        {/* Pantalla de confirmación */}
        {confirmando ? (
          <div className="flex-1 flex flex-col items-center justify-center px-6 py-8 text-center">
            <div className="w-16 h-16 rounded-full bg-yellow-100 flex items-center justify-center mb-4">
              <AlertTriangle className="w-8 h-8 text-yellow-600" />
            </div>
            <h3 className="text-xl font-bold text-gray-800 mb-2">¿Confirmar el cobro?</h3>
            <p className="text-sm text-gray-500 mb-6">Esta acción registrará el pago y enviará el recibo al propietario.</p>

            <div className="w-full bg-gray-50 rounded-xl p-4 text-left space-y-2 mb-6">
              {detalles.map((d) => {
                const medioLabel = MEDIOS_PAGO[d.medio as MedioPago] ?? `Medio ${d.medio}`
                let descripcion = medioLabel
                if (d.medio === 4) {
                  if (d.chequeBanco) descripcion += ` · ${d.chequeBanco}`
                  if (d.chequeNumero) descripcion += ` Nro ${d.chequeNumero}`
                  if (d.chequeFechaVencimiento) descripcion += ` vto ${new Date(d.chequeFechaVencimiento + 'T00:00').toLocaleDateString('es-AR')}`
                } else if (d.referencia) {
                  descripcion += ` · ${d.referencia}`
                }
                return (
                  <div key={d._key} className="flex justify-between text-sm">
                    <span className="text-gray-600">{descripcion}</span>
                    <span className="font-semibold text-gray-800">$ {d.monto.toLocaleString('es-AR')}</span>
                  </div>
                )
              })}
              <div className="border-t border-gray-200 pt-2 flex justify-between text-sm font-bold">
                <span className="text-gray-700">Total cobrado</span>
                <span className={diferencia !== 0 ? 'text-orange-600' : 'text-green-700'}>
                  $ {total.toLocaleString('es-AR')}
                  {diferencia !== 0 && ` (${diferencia > 0 ? '+' : ''}${diferencia.toLocaleString('es-AR')} vs esperado)`}
                </span>
              </div>
            </div>

            {error && <p className="text-sm text-red-600 mb-4">{error}</p>}

            <div className="flex gap-3 w-full">
              <button
                onClick={() => setConfirmando(false)}
                disabled={guardando}
                className="flex-1 px-4 py-3 text-sm font-medium text-gray-700 border border-gray-200 rounded-xl hover:bg-gray-50 transition-colors disabled:opacity-60"
              >
                Volver y corregir
              </button>
              <button
                onClick={handleConfirmar}
                disabled={guardando}
                className="flex-1 px-4 py-3 text-sm font-bold bg-green-600 text-white rounded-xl hover:bg-green-700 transition-colors disabled:opacity-60"
              >
                {guardando ? 'Guardando...' : 'Sí, confirmar cobro'}
              </button>
            </div>
          </div>
        ) : (
          <>
            {/* Formulario */}
            <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">

              {/* Fecha + observaciones */}
              <div className="flex gap-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Fecha de pago</label>
                  <input
                    type="date"
                    value={fecha}
                    onChange={e => setFecha(e.target.value)}
                    className="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div className="flex-1">
                  <label className="block text-sm font-medium text-gray-700 mb-1">Observaciones</label>
                  <input
                    type="text"
                    value={observaciones}
                    onChange={e => setObservaciones(e.target.value)}
                    placeholder="Opcional"
                    className="w-full border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              {/* Formas de pago */}
              <div>
                <div className="flex items-center justify-between mb-2">
                  <label className="text-sm font-medium text-gray-700">Formas de pago</label>
                  <button
                    onClick={agregarDetalle}
                    className="flex items-center gap-1 text-xs text-blue-600 hover:text-blue-700 font-medium"
                  >
                    <Plus className="w-3.5 h-3.5" />
                    Agregar otra forma
                  </button>
                </div>
                <div className="space-y-2">
                  {detalles.map(d => (
                    <DetalleRow
                      key={d._key}
                      item={d}
                      onChange={updated => actualizarDetalle(d._key, updated)}
                      onRemove={() => quitarDetalle(d._key)}
                      canRemove={detalles.length > 1}
                    />
                  ))}
                </div>
              </div>

              {/* Total */}
              <div className={`flex justify-between items-center rounded-xl px-4 py-3 text-sm font-semibold ${
                diferencia === 0
                  ? 'bg-green-50 text-green-700'
                  : diferencia > 0
                    ? 'bg-blue-50 text-blue-700'
                    : 'bg-orange-50 text-orange-700'
              }`}>
                <span>Total a registrar</span>
                <span>$ {total.toLocaleString('es-AR')}</span>
              </div>
              <div className="text-xs text-gray-400 -mt-2 text-right">
                Monto esperado: $ {pago.montoEsperado.toLocaleString('es-AR')}
                {diferencia !== 0 && ` · Diferencia: ${diferencia > 0 ? '+' : ''}$ ${diferencia.toLocaleString('es-AR')}`}
              </div>

              {error && <p className="text-sm text-red-600">{error}</p>}
            </div>

            {/* Footer */}
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3 shrink-0">
              <button
                onClick={onCerrar}
                className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 rounded-xl border border-gray-200 hover:bg-gray-50 transition-colors"
              >
                Cancelar
              </button>
              <button
                onClick={handleSolicitarConfirmacion}
                className="px-5 py-2 text-sm font-semibold bg-blue-600 text-white rounded-xl hover:bg-blue-700 transition-colors"
              >
                Registrar cobro →
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}

// ── PagosPage ────────────────────────────────────────────────────────────────

export default function PagosPage() {
  const ahora = new Date()
  const años = [ahora.getFullYear() - 1, ahora.getFullYear(), ahora.getFullYear() + 1]

  const [metricas, setMetricas] = useState<PagoMetricasDto | null>(null)
  const [items, setItems] = useState<PagoListDto[]>([])
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [filtros, setFiltros] = useState<FiltrosPagos>({
    pagina: 1,
    tamano: 15,
    mes: ahora.getMonth() + 1,
    anio: ahora.getFullYear(),
  })
  const [cargando, setCargando] = useState(false)
  const [pagoModal, setPagoModal] = useState<PagoListDto | null>(null)
  const [descargando, setDescargando] = useState<number | null>(null)

  const cargarMetricas = useCallback(async () => {
    try {
      const res = await getPagoMetricas()
      if (res.success && res.data) setMetricas(res.data)
    } catch {}
  }, [])

  const cargarPagos = useCallback(async () => {
    setCargando(true)
    try {
      const res = await getPagosConsolidados(filtros)
      if (res.success && res.data) {
        setItems(res.data.items)
        setTotalPaginas(res.data.totalPaginas)
        setTotalRegistros(res.data.totalRegistros)
      }
    } finally {
      setCargando(false)
    }
  }, [filtros])

  useEffect(() => { cargarMetricas() }, [cargarMetricas])
  useEffect(() => { cargarPagos() }, [cargarPagos])

  const handleDescargarRecibo = async (p: PagoListDto) => {
    setDescargando(p.id)
    try {
      const d = new Date(p.periodo)
      const periodo = d.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' }).replace(' de ', '_')
      await descargarReciboPago(p.contratoId, p.id, p.contratoCodigo, periodo)
    } finally {
      setDescargando(null)
    }
  }

  const onCobroRegistrado = () => {
    setPagoModal(null)
    cargarPagos()
    cargarMetricas()
  }

  const fmt = (v: number) => `$ ${v.toLocaleString('es-AR')}`

  return (
    <DashboardLayout titulo="Pagos">

      {/* Métricas */}
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-6">
        <MetricCard label="Pendientes"       value={metricas?.pendientesCount ?? '—'}                   color="text-yellow-600" />
        <MetricCard label="Atrasados"         value={metricas?.atrasadosCount ?? '—'}                    color="text-red-600" />
        <MetricCard label="Cobrados este mes" value={metricas?.pagadosMesCount ?? '—'}                   color="text-green-600" />
        <MetricCard label="Cobrado este mes"  value={metricas ? fmt(metricas.montoCobradoMes) : '—'}     color="text-blue-600" />
        <MetricCard label="Total por cobrar"  value={metricas ? fmt(metricas.montoTotalPendiente) : '—'} color="text-orange-600" />
      </div>

      {/* Filtros */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm mb-4 px-5 py-4">
        <div className="flex flex-wrap gap-3 items-center">
          <select
            value={filtros.mes ?? ''}
            onChange={e => setFiltros(f => ({ ...f, pagina: 1, mes: e.target.value ? Number(e.target.value) : undefined }))}
            className="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">Todos los meses</option>
            {MESES.map(([num, label]) => (
              <option key={num} value={num}>{label}</option>
            ))}
          </select>

          <select
            value={filtros.anio ?? ''}
            onChange={e => setFiltros(f => ({ ...f, pagina: 1, anio: e.target.value ? Number(e.target.value) : undefined }))}
            className="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">Todos los años</option>
            {años.map(a => <option key={a} value={a}>{a}</option>)}
          </select>

          <select
            value={filtros.estado ?? ''}
            onChange={e => setFiltros(f => ({ ...f, pagina: 1, estado: e.target.value || undefined }))}
            className="border border-gray-200 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">Todos los estados</option>
            <option value="1">Pendiente</option>
            <option value="2">Pagado</option>
            <option value="3">Atrasado</option>
            <option value="4">Anulado</option>
          </select>

          <span className="ml-auto text-sm text-gray-400">{totalRegistros} registros</span>
        </div>
      </div>

      {/* Tabla */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Contrato</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Propiedad</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Locatario</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Período</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-600">Esperado</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-600">Cobrado</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Formas de pago</th>
                <th className="px-4 py-3 text-center font-semibold text-gray-600">Estado</th>
                <th className="px-4 py-3 text-center font-semibold text-gray-600">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {cargando ? (
                <tr><td colSpan={9} className="py-16 text-center text-gray-400">Cargando pagos...</td></tr>
              ) : items.length === 0 ? (
                <tr><td colSpan={9} className="py-16 text-center text-gray-400">No hay pagos que coincidan con los filtros.</td></tr>
              ) : items.map(p => {
                const estadoNum = estadoPagoNumero(p.estado)
                const esPendiente = estadoNum === 1 || estadoNum === 3
                const esPagado = estadoNum === 2
                return (
                  <tr key={p.id} className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                    <td className="px-4 py-3 font-mono text-xs text-blue-700 font-semibold whitespace-nowrap">
                      {p.contratoCodigo}
                    </td>
                    <td className="px-4 py-3 text-gray-700 max-w-[160px] truncate" title={p.propiedadDireccion}>
                      {p.propiedadDireccion}
                    </td>
                    <td className="px-4 py-3 text-gray-700 whitespace-nowrap">
                      {p.locatarioNombre} {p.locatarioApellido}
                    </td>
                    <td className="px-4 py-3 text-gray-600 whitespace-nowrap">
                      <div>{mesAnio(p.periodo)}</div>
                      <div className="text-xs text-gray-400">Cuota #{p.numeroCuota}</div>
                    </td>
                    <td className="px-4 py-3 text-right text-gray-700 whitespace-nowrap">
                      $ {p.montoEsperado.toLocaleString('es-AR')}
                    </td>
                    <td className="px-4 py-3 text-right whitespace-nowrap">
                      {p.montoPagado != null
                        ? <span className="text-green-700 font-semibold">$ {p.montoPagado.toLocaleString('es-AR')}</span>
                        : <span className="text-gray-300">—</span>
                      }
                    </td>
                    <td className="px-4 py-3 max-w-[160px]">
                      {p.detalles?.length > 0 ? (
                        <div className="space-y-0.5">
                          {p.detalles.map((d, i) => (
                            <div key={i} className="text-xs text-gray-500">
                              {labelMedio(d.medio)}
                              {d.chequeBanco && ` · ${d.chequeBanco}`}
                              {d.chequeNumero && ` #${d.chequeNumero}`}
                            </div>
                          ))}
                        </div>
                      ) : (
                        <span className="text-gray-300 text-xs">—</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-center">
                      <EstadoBadge estado={p.estado} />
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-center gap-2">
                        {esPendiente && (
                          <button
                            onClick={() => setPagoModal(p)}
                            className="text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 px-3 py-1.5 rounded-lg transition-colors whitespace-nowrap"
                          >
                            Registrar cobro
                          </button>
                        )}
                        {esPagado && (
                          <button
                            onClick={() => handleDescargarRecibo(p)}
                            disabled={descargando === p.id}
                            className="flex items-center gap-1 text-xs text-gray-500 hover:text-blue-600 transition-colors disabled:opacity-40 whitespace-nowrap"
                          >
                            <FileDown className="w-3.5 h-3.5" />
                            {descargando === p.id ? '...' : 'Recibo'}
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>

        {totalPaginas > 1 && (
          <div className="flex items-center justify-between px-5 py-4 border-t border-gray-100">
            <span className="text-sm text-gray-500">Página {filtros.pagina} de {totalPaginas}</span>
            <div className="flex gap-2">
              <button
                onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina - 1 }))}
                disabled={filtros.pagina === 1}
                className="p-2 rounded-xl hover:bg-gray-100 disabled:opacity-30 transition-colors"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button
                onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina + 1 }))}
                disabled={filtros.pagina === totalPaginas}
                className="p-2 rounded-xl hover:bg-gray-100 disabled:opacity-30 transition-colors"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {pagoModal && (
        <RegistrarCobroModal
          pago={pagoModal}
          onGuardado={onCobroRegistrado}
          onCerrar={() => setPagoModal(null)}
        />
      )}
    </DashboardLayout>
  )
}
