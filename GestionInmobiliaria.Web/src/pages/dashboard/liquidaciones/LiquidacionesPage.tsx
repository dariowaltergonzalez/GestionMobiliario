import { Fragment, useState, useEffect, useCallback, useRef } from 'react'
import { ChevronLeft, ChevronRight, ChevronDown, Search, Wallet, Pencil, Trash2, Plus, X, AlertTriangle, Paperclip, Sparkles, FileImage } from 'lucide-react'
import DashboardLayout from '../../../components/layout/DashboardLayout'
import {
  getLiquidaciones, getLiquidacionMetricas, eliminarLiquidacion,
  agregarAbono, editarAbono, eliminarAbono, extraerComprobante,
  type LiquidacionDto, type LiquidacionAbonoDto, type LiquidacionMetricasDto,
  type FiltrosLiquidaciones, type AbonoFormData,
} from '../../../api/liquidaciones'
import { getPropietariosActivos, type PropietarioComboDto } from '../../../api/propietarios'
import { MEDIOS_PAGO, medioPagoNumero } from '../../../api/contratos'

const API_URL = 'http://localhost:5005'

function mesAnio(iso: string) {
  return new Date(iso).toLocaleDateString('es-AR', { month: 'short', year: 'numeric' })
}

function formatFecha(iso: string) {
  return new Date(iso).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatMoneda(monto: number, moneda: string) {
  const fmt = monto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  return moneda === 'ARS' ? `$ ${fmt}` : `U$S ${fmt}`
}

const ESTADO_BADGE: Record<string, string> = {
  Pendiente: 'bg-yellow-100 text-yellow-700',
  Parcial: 'bg-orange-100 text-orange-700',
  Liquidado: 'bg-green-100 text-green-700',
}

function MontoInput({ value, onChange, className }: {
  value: number
  onChange: (v: number) => void
  className: string
}) {
  const [focused, setFocused] = useState(false)
  const [texto, setTexto] = useState('')

  const formatear = (v: number) =>
    v ? v.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : ''

  return (
    <input
      type="text"
      inputMode="decimal"
      value={focused ? texto : formatear(value)}
      onFocus={() => { setFocused(true); setTexto(value ? String(value) : '') }}
      onBlur={() => setFocused(false)}
      onChange={e => {
        const raw = e.target.value.replace(/[^\d.,]/g, '')
        setTexto(raw)
        const numerico = Number(raw.replace(',', '.'))
        onChange(Number.isFinite(numerico) ? numerico : 0)
      }}
      className={className}
    />
  )
}

function MetricCard({ label, value, color }: { label: string; value: string | number; color: string }) {
  return (
    <div className="bg-white rounded-2xl border border-gray-100 shadow-sm px-5 py-4">
      <p className="text-xs text-gray-400 mb-1">{label}</p>
      <p className={`text-xl font-bold ${color}`}>{value}</p>
    </div>
  )
}

const abonoVacio = (montoSugerido: number): AbonoFormData => ({
  monto: Math.round(montoSugerido * 100) / 100,
  fecha: new Date().toISOString().slice(0, 10),
  medio: 2,
  cbuCvuDestino: '',
  entidadDestino: '',
  numeroOperacion: '',
  observaciones: '',
  comprobanteUrl: '',
})

// ─── Modal de detalle / abonos ──────────────────────────────────────────────

function DetalleLiquidacionModal({ liquidacion, moneda, onCambio, onEliminada, onCerrar }: {
  liquidacion: LiquidacionDto
  moneda: string
  onCambio: (actualizada: LiquidacionDto) => void
  onEliminada: () => void
  onCerrar: () => void
}) {
  const [form, setForm] = useState<AbonoFormData>(abonoVacio(liquidacion.montoRestante))
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [guardando, setGuardando] = useState(false)
  const [error, setError] = useState('')
  const [confirmEliminarAbono, setConfirmEliminarAbono] = useState<LiquidacionAbonoDto | null>(null)
  const [confirmEliminarLiquidacion, setConfirmEliminarLiquidacion] = useState(false)
  const [subiendoComprobante, setSubiendoComprobante] = useState(false)
  const [errorComprobante, setErrorComprobante] = useState('')
  const fileInputRef = useRef<HTMLInputElement>(null)

  const set = <K extends keyof AbonoFormData>(campo: K, valor: AbonoFormData[K]) =>
    setForm(f => ({ ...f, [campo]: valor }))

  const empezarEdicion = (abono: LiquidacionAbonoDto) => {
    setEditandoId(abono.id)
    setForm({
      monto: abono.monto,
      fecha: abono.fecha.slice(0, 10),
      medio: medioPagoNumero(abono.medio),
      cbuCvuDestino: abono.cbuCvuDestino ?? '',
      entidadDestino: abono.entidadDestino ?? '',
      numeroOperacion: abono.numeroOperacion ?? '',
      observaciones: abono.observaciones ?? '',
      comprobanteUrl: abono.comprobanteUrl ?? '',
    })
    setError('')
    setErrorComprobante('')
  }

  const handleSubirComprobante = async (archivo: File) => {
    setSubiendoComprobante(true)
    setErrorComprobante('')
    try {
      const res = await extraerComprobante(archivo)
      if (res.success) {
        const d = res.data
        setForm(f => ({
          ...f,
          comprobanteUrl: d.comprobanteUrl,
          monto: d.monto ?? f.monto,
          fecha: d.fecha ? d.fecha.slice(0, 10) : f.fecha,
          cbuCvuDestino: d.cbuCvuDestino ?? f.cbuCvuDestino,
          entidadDestino: d.entidadDestino ?? f.entidadDestino,
          numeroOperacion: d.numeroOperacion ?? f.numeroOperacion,
        }))
      } else {
        setErrorComprobante(res.errors?.[0] ?? res.message ?? 'No se pudo subir el comprobante.')
      }
    } catch (err: unknown) {
      const axErr = err as { response?: { data?: { errors?: string[]; message?: string } } }
      setErrorComprobante(axErr.response?.data?.errors?.[0] ?? axErr.response?.data?.message ?? 'No se pudo subir el comprobante.')
    } finally {
      setSubiendoComprobante(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const cancelarEdicion = () => {
    setEditandoId(null)
    setForm(abonoVacio(liquidacion.montoRestante))
    setError('')
    setErrorComprobante('')
  }

  const handleGuardar = async () => {
    if (!form.monto || form.monto <= 0) { setError('El monto debe ser mayor a cero.'); return }
    setGuardando(true)
    setError('')
    try {
      const res = editandoId
        ? await editarAbono(liquidacion.id, editandoId, form)
        : await agregarAbono(liquidacion.id, form)
      if (res.success) {
        onCambio(res.data)
        setEditandoId(null)
        setForm(abonoVacio(res.data.montoRestante))
      } else {
        setError(res.errors?.[0] ?? res.message ?? 'No se pudo guardar el abono.')
      }
    } catch (err: unknown) {
      const axErr = err as { response?: { data?: { errors?: string[]; message?: string } } }
      setError(axErr.response?.data?.errors?.[0] ?? axErr.response?.data?.message ?? 'No se pudo guardar el abono.')
    } finally {
      setGuardando(false)
    }
  }

  const handleEliminarAbono = async () => {
    if (!confirmEliminarAbono) return
    setGuardando(true)
    try {
      const res = await eliminarAbono(liquidacion.id, confirmEliminarAbono.id)
      if (res.success) {
        onCambio(res.data)
        if (editandoId === confirmEliminarAbono.id) cancelarEdicion()
      }
      setConfirmEliminarAbono(null)
    } catch {
      setError('No se pudo eliminar el abono.')
      setConfirmEliminarAbono(null)
    } finally {
      setGuardando(false)
    }
  }

  const handleEliminarLiquidacion = async () => {
    setGuardando(true)
    try {
      const res = await eliminarLiquidacion(liquidacion.id)
      if (res.success) onEliminada()
      else setError(res.errors?.[0] ?? res.message ?? 'No se pudo eliminar la liquidación.')
    } catch {
      setError('No se pudo eliminar la liquidación.')
    } finally {
      setGuardando(false)
      setConfirmEliminarLiquidacion(false)
    }
  }

  const inp = 'w-full border border-gray-200 rounded-xl px-3 py-2 text-sm text-gray-700 outline-none focus:ring-2 focus:ring-blue-900/20 focus:border-blue-900 transition'

  return (
    <div className="fixed inset-0 bg-black/50 flex items-start justify-center z-50 p-4 overflow-y-auto">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg my-6">

        <div className="flex items-center justify-between px-6 pt-5 pb-4 border-b border-gray-100">
          <div>
            <h2 className="font-semibold text-gray-800">Liquidación</h2>
            <p className="text-xs text-gray-400 mt-0.5 font-mono">
              {liquidacion.contratoCodigo} · {mesAnio(liquidacion.periodo)} · {liquidacion.propietarioNombre} {liquidacion.propietarioApellido}
            </p>
          </div>
          <button onClick={onCerrar} className="text-gray-400 hover:text-gray-600">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="px-6 py-4 space-y-5 max-h-[75vh] overflow-y-auto">

          {/* Resumen */}
          <div className="bg-gray-50 rounded-xl px-4 py-3 grid grid-cols-2 gap-3 text-sm">
            <div><p className="text-xs text-gray-400">A liquidar</p><p className="font-semibold text-gray-800">{formatMoneda(liquidacion.montoALiquidar, moneda)}</p></div>
            <div><p className="text-xs text-gray-400">Abonado</p><p className="font-semibold text-green-700">{formatMoneda(liquidacion.montoAbonado, moneda)}</p></div>
            <div><p className="text-xs text-gray-400">Restante</p><p className="font-semibold text-orange-600">{formatMoneda(liquidacion.montoRestante, moneda)}</p></div>
            <div><p className="text-xs text-gray-400">Estado</p>
              <span className={`inline-block text-xs px-2 py-0.5 rounded-full font-medium ${ESTADO_BADGE[liquidacion.estado] ?? ''}`}>{liquidacion.estado}</span>
            </div>
          </div>

          {/* Abonos ya cargados */}
          {liquidacion.abonos.length > 0 && (
            <div>
              <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-2">Abonos registrados</p>
              <div className="border border-gray-100 rounded-xl divide-y divide-gray-100">
                {liquidacion.abonos.map(a => (
                  <div key={a.id} className="px-4 py-2.5 flex items-center justify-between gap-2">
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-gray-800">{formatMoneda(a.monto, moneda)} <span className="text-xs text-gray-400 font-normal">· {a.medio}</span></p>
                      <p className="text-xs text-gray-400 truncate">
                        {formatFecha(a.fecha)}
                        {a.entidadDestino && ` · ${a.entidadDestino}`}
                        {a.numeroOperacion && ` · Op. ${a.numeroOperacion}`}
                      </p>
                    </div>
                    <div className="flex items-center gap-1 shrink-0">
                      {a.comprobanteUrl && (
                        <a href={`${API_URL}${a.comprobanteUrl}`} target="_blank" rel="noreferrer"
                          className="p-1.5 text-gray-500 hover:bg-gray-100 rounded-lg transition-colors" title="Ver comprobante">
                          <FileImage className="w-3.5 h-3.5" />
                        </a>
                      )}
                      <button onClick={() => empezarEdicion(a)} className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors" title="Editar">
                        <Pencil className="w-3.5 h-3.5" />
                      </button>
                      <button onClick={() => setConfirmEliminarAbono(a)} className="p-1.5 text-red-500 hover:bg-red-50 rounded-lg transition-colors" title="Eliminar">
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Form agregar/editar abono */}
          {liquidacion.montoRestante > 0 || editandoId ? (
            <div>
              <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-2">
                {editandoId ? 'Editar abono' : 'Agregar abono'}
              </p>
              <div className="space-y-3">
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">
                    Comprobante (foto o captura, opcional)
                  </label>
                  <input ref={fileInputRef} type="file" accept="image/jpeg,image/png,image/webp,image/heic"
                    disabled={subiendoComprobante}
                    onChange={e => { const f = e.target.files?.[0]; if (f) void handleSubirComprobante(f) }}
                    className="block w-full text-sm text-gray-500 file:mr-3 file:py-2 file:px-3 file:rounded-xl file:border-0 file:text-sm file:font-medium file:bg-blue-50 file:text-blue-900 hover:file:bg-blue-100 disabled:opacity-60" />
                  {subiendoComprobante && (
                    <p className="text-xs text-blue-700 mt-1.5 flex items-center gap-1.5">
                      <Sparkles className="w-3.5 h-3.5" /> Leyendo el comprobante con IA...
                    </p>
                  )}
                  {!subiendoComprobante && form.comprobanteUrl && (
                    <p className="text-xs text-green-700 mt-1.5 flex items-center gap-1.5">
                      <Paperclip className="w-3.5 h-3.5" /> Comprobante adjuntado.
                      Revisá los datos de abajo antes de guardar — no se cargan solos.
                    </p>
                  )}
                  {errorComprobante && <p className="text-xs text-red-600 mt-1.5">{errorComprobante}</p>}
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">Monto</label>
                    <MontoInput value={form.monto} onChange={v => set('monto', v)} className={inp} />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">Fecha</label>
                    <input type="date" value={form.fecha} onChange={e => set('fecha', e.target.value)} className={inp} />
                  </div>
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Medio</label>
                  <select value={form.medio} onChange={e => set('medio', Number(e.target.value))} className={`${inp} bg-white`}>
                    {Object.entries(MEDIOS_PAGO).map(([valor, label]) => (
                      <option key={valor} value={valor}>{label}</option>
                    ))}
                  </select>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">CBU / CVU destino</label>
                    <input type="text" value={form.cbuCvuDestino} onChange={e => set('cbuCvuDestino', e.target.value)}
                      placeholder="0000003100..." className={inp} />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-600 mb-1">Entidad destino</label>
                    <input type="text" value={form.entidadDestino} onChange={e => set('entidadDestino', e.target.value)}
                      placeholder="Banco / billetera" className={inp} />
                  </div>
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Número de operación</label>
                  <input type="text" value={form.numeroOperacion} onChange={e => set('numeroOperacion', e.target.value)}
                    placeholder="Ej: 163163059380" className={inp} />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Observaciones</label>
                  <textarea value={form.observaciones} onChange={e => set('observaciones', e.target.value)}
                    rows={2} placeholder="Aclaraciones adicionales..." className={`${inp} resize-none`} />
                </div>

                {error && <p className="text-sm text-red-600">{error}</p>}

                <div className="flex gap-3">
                  {editandoId && (
                    <button onClick={cancelarEdicion}
                      className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
                      Cancelar edición
                    </button>
                  )}
                  <button onClick={handleGuardar} disabled={guardando}
                    className="flex-1 flex items-center justify-center gap-2 bg-blue-900 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-blue-800 disabled:opacity-60 transition-colors">
                    {!editandoId && <Plus className="w-4 h-4" />}
                    {guardando ? 'Guardando...' : editandoId ? 'Guardar cambios' : 'Agregar abono'}
                  </button>
                </div>
              </div>
            </div>
          ) : (
            <p className="text-sm text-green-700 bg-green-50 rounded-xl px-4 py-3">Esta liquidación ya está completamente abonada.</p>
          )}
        </div>

        <div className="px-6 py-4 border-t border-gray-100 flex items-center justify-between">
          {liquidacion.estado === 'Pendiente' && liquidacion.abonos.length === 0 ? (
            <button onClick={() => setConfirmEliminarLiquidacion(true)}
              className="text-sm text-red-600 hover:text-red-700 font-medium transition-colors">
              Eliminar liquidación
            </button>
          ) : <span />}
          <button onClick={onCerrar}
            className="border border-gray-200 text-gray-600 px-5 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
            Cerrar
          </button>
        </div>
      </div>

      {/* Confirmar eliminar abono */}
      {confirmEliminarAbono && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[60] p-4">
          <div className="bg-white rounded-2xl p-6 max-w-sm w-full shadow-xl">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 bg-red-100 rounded-xl flex items-center justify-center">
                <AlertTriangle className="w-5 h-5 text-red-600" />
              </div>
              <h3 className="font-semibold text-gray-800">Eliminar abono</h3>
            </div>
            <p className="text-sm text-gray-600 mb-6">
              ¿Confirmás eliminar el abono de <strong>{formatMoneda(confirmEliminarAbono.monto, moneda)}</strong>?
            </p>
            <div className="flex gap-3">
              <button onClick={() => setConfirmEliminarAbono(null)}
                className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
                Cancelar
              </button>
              <button onClick={handleEliminarAbono} disabled={guardando}
                className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors">
                {guardando ? 'Eliminando...' : 'Eliminar'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Confirmar eliminar liquidación */}
      {confirmEliminarLiquidacion && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[60] p-4">
          <div className="bg-white rounded-2xl p-6 max-w-sm w-full shadow-xl">
            <div className="flex items-center gap-3 mb-4">
              <div className="w-10 h-10 bg-red-100 rounded-xl flex items-center justify-center">
                <AlertTriangle className="w-5 h-5 text-red-600" />
              </div>
              <h3 className="font-semibold text-gray-800">Eliminar liquidación</h3>
            </div>
            <p className="text-sm text-gray-600 mb-6">
              ¿Confirmás eliminar esta liquidación? No tiene abonos cargados, así que se puede sacar sin dejar
              nada pendiente suelto.
            </p>
            <div className="flex gap-3">
              <button onClick={() => setConfirmEliminarLiquidacion(false)}
                className="flex-1 border border-gray-200 text-gray-600 py-2.5 rounded-xl text-sm font-medium hover:bg-gray-50 transition-colors">
                Cancelar
              </button>
              <button onClick={handleEliminarLiquidacion} disabled={guardando}
                className="flex-1 bg-red-600 text-white py-2.5 rounded-xl text-sm font-medium hover:bg-red-700 disabled:opacity-60 transition-colors">
                {guardando ? 'Eliminando...' : 'Eliminar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// ─── Página principal ────────────────────────────────────────────────────────

export default function LiquidacionesPage() {
  const [metricas, setMetricas] = useState<LiquidacionMetricasDto | null>(null)
  const [items, setItems] = useState<LiquidacionDto[]>([])
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [totalRegistros, setTotalRegistros] = useState(0)
  const [propietarios, setPropietarios] = useState<PropietarioComboDto[]>([])
  const [filtros, setFiltros] = useState<FiltrosLiquidaciones>({ pagina: 1, tamano: 15 })
  const [buscarInput, setBuscarInput] = useState('')
  const [cargando, setCargando] = useState(false)
  const [error, setError] = useState('')
  const [detalle, setDetalle] = useState<LiquidacionDto | null>(null)
  const [expandidoId, setExpandidoId] = useState<number | null>(null)

  const cargarMetricas = useCallback(async () => {
    try {
      const res = await getLiquidacionMetricas()
      if (res.success) setMetricas(res.data)
    } catch { /* no bloquea la pantalla */ }
  }, [])

  const cargar = useCallback(async () => {
    setCargando(true)
    setError('')
    try {
      const res = await getLiquidaciones(filtros)
      if (res.success) {
        setItems(res.data.items)
        setTotalPaginas(res.data.totalPaginas)
        setTotalRegistros(res.data.totalRegistros)
      }
    } catch {
      setError('No se pudieron cargar las liquidaciones.')
    } finally {
      setCargando(false)
    }
  }, [filtros])

  useEffect(() => { cargarMetricas() }, [cargarMetricas])
  useEffect(() => { cargar() }, [cargar])
  useEffect(() => {
    getPropietariosActivos().then(res => { if (res.success) setPropietarios(res.data) }).catch(() => {})
  }, [])

  const handleBuscar = () => setFiltros(f => ({ ...f, buscar: buscarInput || undefined, pagina: 1 }))

  const handleCambioDetalle = (actualizada: LiquidacionDto) => {
    setDetalle(actualizada)
    setItems(prev => prev.map(l => l.id === actualizada.id ? actualizada : l))
    cargarMetricas()
  }

  const handleEliminada = () => {
    setDetalle(null)
    cargar()
    cargarMetricas()
  }

  const selectClass = 'border border-gray-200 rounded-xl px-3 py-2.5 text-sm text-gray-600 bg-white outline-none'

  return (
    <DashboardLayout titulo="Liquidaciones">

      {/* Métricas */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <MetricCard label="Pendientes"          value={metricas?.pendientesCount ?? '—'} color="text-yellow-600" />
        <MetricCard label="Total pendiente"      value={metricas ? formatMoneda(metricas.montoPendienteTotal, 'ARS') : '—'} color="text-orange-600" />
        <MetricCard label="Liquidadas este mes"  value={metricas?.liquidadasMesCount ?? '—'} color="text-green-600" />
        <MetricCard label="Liquidado este mes"   value={metricas ? formatMoneda(metricas.montoLiquidadoMes, 'ARS') : '—'} color="text-blue-600" />
      </div>

      {/* Filtros */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm mb-4 px-5 py-4">
        <div className="flex flex-wrap gap-3 items-center">
          <div className="flex items-center gap-2 border border-gray-200 rounded-xl px-3 bg-white">
            <Search className="w-4 h-4 text-gray-400 shrink-0" />
            <input
              type="text"
              placeholder="Código de contrato o propietario..."
              value={buscarInput}
              onChange={e => setBuscarInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleBuscar()}
              className="py-2 text-sm outline-none w-56 text-gray-700 placeholder-gray-400"
            />
          </div>
          <button onClick={handleBuscar} className="bg-blue-900 text-white px-4 py-2 rounded-xl text-sm font-medium hover:bg-blue-800 transition-colors">
            Buscar
          </button>

          <select
            value={filtros.estado ?? ''}
            onChange={e => setFiltros(f => ({ ...f, pagina: 1, estado: e.target.value || undefined }))}
            className={selectClass}
          >
            <option value="">Todos los estados</option>
            <option value="1">Pendiente</option>
            <option value="3">Parcial</option>
            <option value="2">Liquidado</option>
          </select>

          <select
            value={filtros.propietarioId ?? ''}
            onChange={e => setFiltros(f => ({ ...f, pagina: 1, propietarioId: e.target.value ? Number(e.target.value) : undefined }))}
            className={selectClass}
          >
            <option value="">Todos los propietarios</option>
            {propietarios.map(p => <option key={p.id} value={p.id}>{p.nombreCompleto}</option>)}
          </select>

          <span className="ml-auto text-sm text-gray-400">{totalRegistros} registros</span>
        </div>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-xl mb-4 text-sm">{error}</div>}

      {/* Tabla */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50">
                <th className="w-8"></th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Contrato</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Propietario</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-600">Período</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-600">A liquidar</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-600">Abonado</th>
                <th className="px-4 py-3 text-center font-semibold text-gray-600">Estado</th>
                <th className="px-4 py-3 text-center font-semibold text-gray-600">Acción</th>
              </tr>
            </thead>
            <tbody>
              {cargando ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i} className="border-b border-gray-50">
                    {Array.from({ length: 8 }).map((_, j) => (
                      <td key={j} className="px-4 py-4"><div className="h-4 bg-gray-100 rounded animate-pulse" /></td>
                    ))}
                  </tr>
                ))
              ) : items.length === 0 ? (
                <tr><td colSpan={8} className="text-center py-16 text-gray-400">No se encontraron liquidaciones</td></tr>
              ) : items.map(l => {
                const tieneDetalle = l.montoComision > 0 || l.montoGastos > 0
                const expandido = expandidoId === l.id
                return (
                <Fragment key={l.id}>
                <tr className="border-b border-gray-50 hover:bg-gray-50 transition-colors">
                  <td className="px-2 py-4">
                    {tieneDetalle && (
                      <button
                        onClick={() => setExpandidoId(expandido ? null : l.id)}
                        className="p-1 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
                        title="Ver desglose"
                      >
                        <ChevronDown className={`w-4 h-4 transition-transform ${expandido ? 'rotate-180' : ''}`} />
                      </button>
                    )}
                  </td>
                  <td className="px-4 py-4">
                    <span className="font-mono text-blue-600">{l.contratoCodigo}</span>
                    <div className="text-xs text-gray-400">{l.propiedadDireccion}</div>
                  </td>
                  <td className="px-4 py-4 text-gray-700">{l.propietarioApellido}, {l.propietarioNombre}</td>
                  <td className="px-4 py-4 text-gray-600">
                    {mesAnio(l.periodo)}
                    <div className="text-xs text-gray-400">Cuota #{l.numeroCuota}</div>
                  </td>
                  <td className="px-4 py-4 text-right font-semibold text-gray-800">{formatMoneda(l.montoALiquidar, l.moneda)}</td>
                  <td className="px-4 py-4 text-right">
                    <span className="text-green-700">{formatMoneda(l.montoAbonado, l.moneda)}</span>
                    {l.montoRestante > 0 && (
                      <div className="text-xs text-gray-400">faltan {formatMoneda(l.montoRestante, l.moneda)}</div>
                    )}
                  </td>
                  <td className="px-4 py-4 text-center">
                    <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${ESTADO_BADGE[l.estado] ?? ''}`}>
                      {l.estado === 'Liquidado' && l.fechaLiquidacion ? `Liquidado ${formatFecha(l.fechaLiquidacion)}` : l.estado}
                    </span>
                  </td>
                  <td className="px-4 py-4 text-center">
                    <button
                      onClick={() => setDetalle(l)}
                      className="inline-flex items-center gap-1 text-xs px-3 py-1.5 rounded-lg bg-blue-900 text-white hover:bg-blue-800 transition-colors"
                    >
                      <Wallet className="w-3.5 h-3.5" /> {l.estado === 'Pendiente' ? 'Liquidar' : 'Ver / gestionar'}
                    </button>
                  </td>
                </tr>
                {expandido && (
                  <tr className="border-b border-gray-50 bg-gray-50/60">
                    <td></td>
                    <td colSpan={7} className="px-4 py-3">
                      <p className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-2">
                        Cómo se llegó a {formatMoneda(l.montoALiquidar, l.moneda)}
                      </p>
                      <div className="text-sm space-y-1 max-w-md">
                        <div className="flex justify-between">
                          <span className="text-gray-500">Cobrado</span>
                          <span className="text-gray-700">{formatMoneda(l.montoCobrado, l.moneda)}</span>
                        </div>
                        {l.montoComision > 0 && (
                          <div className="flex justify-between">
                            <span className="text-gray-500">
                              Comisión inmobiliaria {l.comisionPorcentaje ? `(${l.comisionPorcentaje}%)` : ''}
                            </span>
                            <span className="text-red-500">− {formatMoneda(l.montoComision, l.moneda)}</span>
                          </div>
                        )}
                        {l.gastos.map(g => (
                          <div key={g.id} className="flex justify-between">
                            <span className="text-gray-500">
                              Gasto: {g.categoria}{g.descripcion ? ` — ${g.descripcion}` : ''}
                            </span>
                            <span className="text-red-500">− {formatMoneda(g.monto, l.moneda)}</span>
                          </div>
                        ))}
                        <div className="flex justify-between pt-1 border-t border-gray-200 font-semibold">
                          <span className="text-gray-700">A liquidar</span>
                          <span className="text-gray-800">{formatMoneda(l.montoALiquidar, l.moneda)}</span>
                        </div>
                      </div>
                    </td>
                  </tr>
                )}
                </Fragment>
                )
              })}
            </tbody>
          </table>
        </div>

        {!cargando && totalRegistros > 0 && (
          <div className="px-5 py-4 border-t border-gray-100 flex items-center justify-between">
            <span className="text-sm text-gray-400">
              {totalRegistros} liquidaci{totalRegistros !== 1 ? 'ones' : 'ón'} · Página {filtros.pagina} de {totalPaginas}
            </span>
            <div className="flex gap-1">
              <button onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina - 1 }))} disabled={filtros.pagina <= 1}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button onClick={() => setFiltros(f => ({ ...f, pagina: f.pagina + 1 }))} disabled={filtros.pagina >= totalPaginas}
                className="p-2 rounded-lg text-gray-400 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors">
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {detalle && (
        <DetalleLiquidacionModal
          liquidacion={detalle}
          moneda={detalle.moneda}
          onCambio={handleCambioDetalle}
          onEliminada={handleEliminada}
          onCerrar={() => { setDetalle(null); cargar() }}
        />
      )}

    </DashboardLayout>
  )
}
